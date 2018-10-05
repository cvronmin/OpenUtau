﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.Core.USTx
{
    public class UProject
    {
        public double BPM = 120;
        public int BeatPerBar = 4;
        public int BeatUnit = 4;
        public int Resolution = 480;

        public string Name = "New Project";
        public string Comment = "";
        public string OutputDir = "Vocal";
        public string CacheDir = "UCache";
        public string FilePath;
        public bool Saved = false;

        public List<UTrack> Tracks = new List<UTrack>();
        public List<UPart> Parts = new List<UPart>();
        public List<USinger> Singers = new List<USinger>();

        private Dictionary<int, double> _subBPM = new Dictionary<int, double>();

        public Dictionary<int, double> SubBPM { get => (_subBPM = _subBPM.OrderBy(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value)); }

        public Dictionary<string, UExpression> ExpressionTable = new Dictionary<string, UExpression>();

        public void RegisterExpression(UExpression exp) { if (!ExpressionTable.ContainsKey(exp.Name)) ExpressionTable.Add(exp.Name, exp); }
        public void UnregisterExpression(UExpression exp) { if (ExpressionTable.ContainsKey(exp.Name)) ExpressionTable.Remove(exp.Name); }
        public UNote CreateNote()
        {
            UNote note = UNote.Create();
            foreach (var pair in ExpressionTable)
            {
                note.Expressions.Add(pair.Key, pair.Value.Clone(note));
                switch (pair.Value)
                {
                    case IntExpression exp:
                        note.VirtualExpressions.Add(pair.Key,  new IntExpression.UExpDiff() { Data = 0 });
                        break;
                    case FloatExpression exp:
                        note.VirtualExpressions.Add(pair.Key, new FloatExpression.UExpDiff() { Data = 0 });
                        break;
                    case BoolExpression exp:
                        note.VirtualExpressions.Add(pair.Key, new BoolExpression.UExpDiff() { Data = false });
                        break;
                    default:
                        break;
                }
            }
            note.PitchBend.Points[0].X = -25;
            note.PitchBend.Points[1].X = 25;
            return note;
        }

        public UNote CreateNote(int noteNum, int posTick, int durTick)
        {
            var note = CreateNote();
            note.NoteNum = noteNum;
            note.PosTick = posTick;
            note.DurTick = durTick;
            note.PitchBend.Points[1].X = Math.Min(25, DocManager.Inst.Project.TickToMillisecond(note.DurTick, posTick) / 2);
            return note;
        }

        public UVoicePart CreateVoicePart(int TrackNo, int PosTick) {
            UVoicePart part = new UVoicePart() { TrackNo = TrackNo, PosTick = PosTick, PartNo = Parts.Count };
            foreach (var pair in ExpressionTable) { part.Expressions.Add(pair.Key, pair.Value); }
            return part;
        }

        public UProject() { }

        public int MillisecondToTick(double ms, double offsetMs = 0) {
            var tick1 = MillisecondToTickImpl(ms + offsetMs);
            var tick2 = MillisecondToTickImpl(offsetMs);
            return tick1 - tick2;
        }

        private int MillisecondToTickImpl(double ms)
        {
            int tick = MusicMath.MillisecondToTick(ms, BPM, BeatUnit, Resolution);
            int processed = tick;
            var passedBpm = SubBPM.Where(pair => pair.Key < tick).OrderBy(pair => pair.Key);
            double passedMs = ms;
            if (passedBpm.Any())
            {
                processed = passedBpm.First().Key;
                passedMs -= MusicMath.TickToMillisecond(passedBpm.First().Key, BPM, BeatUnit, Resolution);
                for (int i = 0; i < passedBpm.Count(); i++)
                {
                    if (i < passedBpm.Count() - 1)
                    {
                        double expectMs = MusicMath.TickToMillisecond(passedBpm.ElementAt(i + 1).Key - passedBpm.ElementAt(i).Key, passedBpm.ElementAt(i).Value, BeatUnit, Resolution);
                        passedMs -= expectMs;
                        processed += MusicMath.MillisecondToTick(passedMs < 0 ? expectMs + passedMs : expectMs, passedBpm.ElementAt(i).Value, BeatUnit, Resolution);
                        if (passedMs <= 0) break;
                    }
                    else
                    {
                        processed += MusicMath.MillisecondToTick(passedMs, passedBpm.ElementAt(i).Value, BeatUnit, Resolution);
                    }
                }
            }
            return processed;
        }

        public double TickToMillisecond(double tick, double offsetTick = 0) {
            var ms1 = TickToMillisecondImpl(tick + offsetTick);
            var ms2 = TickToMillisecondImpl(offsetTick);
            return ms1 - ms2;
        }

        private double TickToMillisecondImpl(double tick)
        {
            var passedBpm = SubBPM.Where(pair => pair.Key < tick).OrderBy(pair => pair.Key);
            double ms;
            if (passedBpm.Any())
            {
                ms = MusicMath.TickToMillisecond(passedBpm.First().Key, BPM, BeatUnit, Resolution);
                for (int i = 0; i < passedBpm.Count(); i++)
                {
                    ms += MusicMath.TickToMillisecond((i == passedBpm.Count() - 1 ? tick : passedBpm.ElementAt(i + 1).Key) - passedBpm.ElementAt(i).Key, passedBpm.ElementAt(i).Value, BeatUnit, Resolution);
                }
            }
            else
            {
                ms = MusicMath.TickToMillisecond(tick, BPM, BeatUnit, Resolution);
            }
            return ms;
        }

    }
}
