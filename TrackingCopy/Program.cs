using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ScriptPortal;
using ScriptPortal.Vegas;
using System.Windows.Forms;
using System.IO;
using VegasWrapper = ScriptPortal.Vegas.Vegas;

namespace TrackingCopy
{
    public class EntryPoint
    {
        public void FromVegas(VegasWrapper vegas)
        {
            var fileName = "aud.csv";
            var trackName = "1CAM_AUD";
            string[] sourceOutsTiming = null;
            var EffectNames = new[] {
                    "EffectName_1",
                    "EffectName_2",
                    "....",
                    "EffectName_N",
                };
            try
            {
                sourceOutsTiming = File.ReadAllLines(Path.GetDirectoryName(vegas.Project.FilePath) + "\\" + fileName);
            }
            catch
            {
                MessageBox.Show("File " + fileName + " are used by another process.\nPlease close the file.", "File are unaccessible.");
                return;
            }
            var timings = ParseOutTiming(sourceOutsTiming);

            var track = GetTrackByName(vegas.Project, trackName);
            var volumeEnvelope = track.Envelopes.FindByType(EnvelopeType.Volume);
            if(volumeEnvelope == null)
            {
                volumeEnvelope = new Envelope(EnvelopeType.Volume);
                track.Envelopes.Add(volumeEnvelope);
            }
            foreach(var timing in timings)
            {
                volumeEnvelope.Points.Add(new EnvelopePoint(timing.Item1, 1));
                volumeEnvelope.Points.Add(new EnvelopePoint(timing.Item1 + Timecode.FromFrames(5), 0.072));
                volumeEnvelope.Points.Add(new EnvelopePoint(timing.Item2 - Timecode.FromFrames(5), Math.Sqrt(((40 - 10) / 46))));
                volumeEnvelope.Points.Add(new EnvelopePoint(timing.Item2, 1));
            }
            removeFXFromTrackByName(track, EffectNames);
        }
        private IEnumerable<Tuple<Timecode, Timecode>> ParseOutTiming(string[] sourceOutsTiming)
        {
            foreach(var outTiming in sourceOutsTiming)
            {
                if(!string.IsNullOrWhiteSpace(outTiming))
                {
                    var splitedRegionData = outTiming.Split(',');
                    long first, second;
                    long.TryParse(splitedRegionData[0], out first);
                    long.TryParse(splitedRegionData[1], out second);
                    yield return new Tuple<Timecode, Timecode>(Timecode.FromFrames(first), Timecode.FromFrames(second));
                }
            }
        }

        private AudioTrack GetTrackByName(Project vegasPr, string tName)
        {
            foreach(var track in vegasPr.Tracks)
            {
                if(track.Name == tName && track.IsAudio()) return (AudioTrack)track;
            }
            return null;
        }
        private void removeFXFromTrackByName(Track track, params string[] FXNameList)
        {
            var listToRemove = new List<Effect>();
            foreach(var effect in track.Effects)
            {
                foreach(var fxToRemoveName in FXNameList)
                {
                    if(effect.PlugIn.Name.ToLower().Contains(fxToRemoveName.ToLower()))
                    {
                        listToRemove.Add(effect);
                    }
                }
            }
            foreach(var effectToRemove in listToRemove)
            {
                try
                {
                    track.Effects.Remove(effectToRemove);
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Cant remove effect by name '" + effectToRemove.PlugIn.Name + "'" + Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace, ex.GetType().Name);
                }
            }
        }
    }
}
