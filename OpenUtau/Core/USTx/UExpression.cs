﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using JSON = System.Web.Script.Serialization.JavaScriptSerializer;

namespace OpenUtau.Core.USTx
{
    public abstract class UExpression
    {
        public UExpression(UNote parent, string name, string abbr) { _parent = parent; _name = name; _abbr = abbr; }

        protected UNote _parent;
        protected string _name;
        protected string _abbr;

        public UNote Parent => _parent;
        public virtual string Name => _name;
        public virtual string Abbr => _abbr;

        public abstract string Type { get; }
        public abstract object Data { set; get; }

        public abstract UExpression Clone(UNote newParent);
        public abstract UExpression Split(UNote newParent, int offset);

        public abstract class UExpDiff {
            public abstract string Type { get; }
            public abstract object Data { get; set; }
        }
    }

    public class IntExpression : UExpression
    {
        public IntExpression(UNote parent, string name, string abbr) : base(parent, name, abbr) { }
        protected int _data;
        protected int _min = 0;
        protected int _max = 100;
        protected int _default = 0;
        public virtual int Default { set => _default = value;
            get => _default;
        }
        public virtual int Min { set => _min = value;
            get => _min;
        }
        public virtual int Max { set => _max = value;
            get => _max;
        }
        public override string Type => "int";
        public override object Data { set => _data = Math.Min(Max, Math.Max(Min, (int)value));
            get => _data;
        }
        public override UExpression Clone(UNote newParent) { return new IntExpression(newParent, Name, Abbr) { Min = Min, Max = Max, Data = Data, Default = Default }; }
        public override UExpression Split(UNote newParent, int postick) { var exp = Clone(newParent); return exp; }
        public new class UExpDiff : UExpression.UExpDiff
        {
            public override string Type => "int";

            protected int _data;

            public override object Data { get => _data; set => _data = (int)value; }
        }
    }

    public class FlagIntExpression : IntExpression
    {
        public virtual string Flag { set; get; }
        public FlagIntExpression(UNote parent, string name, string abbr) : base(parent, name, abbr)
        {
        }

        public FlagIntExpression(UNote parent, string name, string abbr, string flag) : base(parent, name, abbr)
        {
            Flag = flag;
        }

        public override string Type => "flag_int";

        public override UExpression Clone(UNote newParent)
        {
            return new FlagIntExpression(newParent, Name, Abbr, Flag) { Min = Min, Max = Max, Data = Data, Default = Default};
        }

        public override UExpression Split(UNote newParent, int postick)
        {
            return Clone(newParent);
        }
    }

    public class FloatExpression : UExpression
    {
        public FloatExpression(UNote parent, string name, string abbr) : base(parent, name, abbr) { }
        protected float _data;
        protected float _min = 0;
        protected float _max = 100;
        protected float _default = 0;
        public virtual float Default { set => _default = value;
            get => _default;
        }
        public virtual float Min { set => _min = value;
            get => _min;
        }
        public virtual float Max { set => _max = value;
            get => _max;
        }
        public override string Type => "float";
        public override object Data { set => _data = Math.Min(Max, Math.Max(Min, (float)value));
            get => _data;
        }
        public override UExpression Clone(UNote newParent) { return new FloatExpression(newParent, Name, Abbr) { Min = Min, Max = Max, Data = Data, Default =Default }; }
        public override UExpression Split(UNote newParent, int postick) { var exp = Clone(newParent); return exp; }

        public new class UExpDiff : UExpression.UExpDiff
        {
            public override string Type => "float";

            protected float _data;

            public override object Data { get => _data; set => _data = (float)value; }
        }
    }

    public class FlagFloatExpression : FloatExpression
    {
        public virtual string Flag { set; get; }
        public FlagFloatExpression(UNote parent, string name, string abbr) : base(parent, name, abbr)
        {
        }

        public FlagFloatExpression(UNote parent, string name, string abbr, string flag) : base(parent, name, abbr)
        {
            Flag = flag;
        }

        public override string Type => "flag_float";

        public override UExpression Clone(UNote newParent)
        {
            return new FlagFloatExpression(newParent, Name, Abbr, Flag) { Min = Min, Max = Max, Data = Data,Default = Default };
        }

        public override UExpression Split(UNote newParent, int postick)
        {
            return Clone(newParent);
        }
    }

    public class BoolExpression : UExpression
    {
        public BoolExpression(UNote parent, string name, string abbr) : base(parent, name, abbr) { }
        protected bool _data;
        protected bool _default = false;
        public virtual bool Default { set => _default = value;
            get => _default;
        }
        public override string Type => "bool";
        public override object Data { set => _data = (bool)value;
            get => _data;
        }
        public override UExpression Clone(UNote newParent) { return new BoolExpression(newParent, Name, Abbr) { Data = Data, Default = Default }; }
        public override UExpression Split(UNote newParent, int postick) { var exp = Clone(newParent); return exp; }

        public new class UExpDiff : UExpression.UExpDiff
        {
            public override string Type => "bool";

            protected bool _data;

            public override object Data { get => _data; set => _data = (bool)value; }
        }
    }

    public class FlagBoolExpression : BoolExpression
    {
        public virtual string Flag { set; get; }
        public FlagBoolExpression(UNote parent, string name, string abbr) : base(parent, name, abbr)
        {
        }

        public FlagBoolExpression(UNote parent, string name, string abbr, string flag) : base(parent, name, abbr)
        {
            Flag = flag;
        }

        public override string Type => "flag_bool";

        public override UExpression Clone(UNote newParent)
        {
            return new FlagBoolExpression(newParent, Name, Abbr, Flag) { Data = Data,Default = Default };
        }

        public override UExpression Split(UNote newParent, int postick)
        {
            return Clone(newParent);
        }
    }

    public class ExpPoint : IComparable<ExpPoint>
    {
        public double X;
        public double Y;
        public int CompareTo(ExpPoint other)
        {
            if (this.X > other.X) return 1;
            else if (this.X == other.X) return 0;
            else return -1;
        }
        public ExpPoint(double x, double y) { X = x; Y = y; }
        public ExpPoint Clone() { return new ExpPoint(X, Y); }
    }

    public enum PitchPointShape 
    {
        /// <summary>
        /// SineInOut
        /// </summary>
        InOut = 3,
        /// <summary>
        /// Linear
        /// </summary>
        Linear = 0,
        /// <summary>
        /// SineIn
        /// </summary>
        In = 1,
        /// <summary>
        /// SineOut
        /// </summary>
        Out = 2
    };

    public class PitchPoint : ExpPoint
    {
        public PitchPointShape Shape;
        public PitchPoint(double x, double y, PitchPointShape shape = PitchPointShape.InOut) : base(x, y) { Shape = shape; }
        public new PitchPoint Clone() { return new PitchPoint(X, Y, Shape); }
    }

    public class PitchBendExpression : UExpression
    {
        public PitchBendExpression(UNote parent) : base(parent, "pitch", "PIT") {
            _data.Add(new PitchPoint(0, 0));
            _data.Add(new PitchPoint(0, 0));
        }
        protected List<PitchPoint> _data = new List<PitchPoint>();
        protected bool _snapFirst = true;
        public override string Type => "pitch";
        public override object Data { set => _data = (List<PitchPoint>)value;
            get => _data;
        }
        public List<PitchPoint> Points => _data;

        public bool SnapFirst { set => _snapFirst = value;
            get => _snapFirst;
        }
        public void AddPoint(PitchPoint p) { _data.Add(p); _data.Sort(); }
        public void RemovePoint(PitchPoint p) { _data.Remove(p); }
        public override UExpression Clone(UNote newParent)
        {
            var data = new List<PitchPoint>();
            foreach (var p in this._data) data.Add(p.Clone());
            return new PitchBendExpression(newParent) { Data = data, SnapFirst = this.SnapFirst };
        }
        public override UExpression Split(UNote newParent, int offset)
        {
            var newdata = new List<PitchPoint>();
            while (_data.Count > 0 && _data.Last().X >= offset) { newdata.Add(_data.Last()); _data.Remove(_data.Last()); }
            newdata.Reverse();
            return new PitchBendExpression(newParent) { Data = newdata, SnapFirst = true };
        }

        public UExpression Merge(UNote oldParnet) {
            foreach (var item in oldParnet.PitchBend.Points)
            {
                var pre = item.Clone();
                pre.X += DocManager.Inst.Project.TickToMillisecond(oldParnet.PosTick - Parent.PosTick, DocManager.Inst.Project.Parts[oldParnet.PartNo].PosTick);
                pre.Y += (oldParnet.NoteNum - Parent.NoteNum) * 10;
                AddPoint(pre);
            }
            return this;
        }
    }

    public class EnvelopeExpression : UExpression
    {
        public EnvelopeExpression(UNote parent) : base(parent, "envelope", "env")
        {
            _data.Add(new ExpPoint(0, 0));
            _data.Add(new ExpPoint(0, 100));
            _data.Add(new ExpPoint(0, 100));
            _data.Add(new ExpPoint(0, 100));
            _data.Add(new ExpPoint(0, 0));
        }
        protected List<ExpPoint> _data = new List<ExpPoint>();
        public override string Type => "envelope";

        public override object Data { set => _data = (List<ExpPoint>)value;
            get => _data;
        }
        public List<ExpPoint> Points => _data;
        public UPhoneme ParentPhoneme;
        public override UExpression Clone(UNote newParent)
        {
            var data = new List<ExpPoint>();
            foreach (var p in this._data) data.Add(p.Clone());
            return new EnvelopeExpression(newParent) { Data = data };
        }
        public override UExpression Split(UNote newParent, int offset)
        {
            var newdata = new List<ExpPoint>();
            // TODO
            return new EnvelopeExpression(newParent) { Data = newdata };
        }
    }

    public class VibratoExpression : UExpression
    {
        public VibratoExpression(UNote parent) : base(parent, "vibrato", "VBR") { }
        double _length;
        double _period;
        double _depth;
        double _in;
        double _out;
        double _shift;
        double _drift;
        public double Length { set => _length = Math.Max(0, Math.Min(100, value));
            get => _length;
        }
        public double Period { set => _period = Math.Max(64, Math.Min(512, value));
            get => _period;
        }
        public double Depth { set => _depth = Math.Max(5, Math.Min(200, value));
            get => _depth;
        }
        public double In { set { _in = Math.Max(0, Math.Min(100, value)); _out = Math.Max(0, Math.Min(_out, 100 - value)); } get => _in;
        }
        public double Out { set { _out = Math.Max(0, Math.Min(100, value)); _in = Math.Max(0, Math.Min(_in, 100 - value)); } get => _out;
        }
        public double Shift { set => _shift = Math.Max(0, Math.Min(100, value));
            get => _shift;
        }
        public double Drift { set => _drift = Math.Max(-100, Math.Min(100, value));
            get => _drift;
        }
        public override string Type => "pitch";
        public override object Data { set; get; }
        public override UExpression Clone(UNote newParent)
        {
            return new VibratoExpression(newParent)
            {
                _length = _length,
                _period = _period,
                _depth = _depth,
                _in = _in,
                _out = _out,
                _shift = _shift,
                _drift = _drift,
                IsEnabled = IsEnabled
            };
        }
        public override UExpression Split(UNote newParent, int postick) {
            var exp = Clone(newParent) as VibratoExpression;
            if (postick >= Parent.PosTick + Parent.DurTick * (1 - Length / 100f))
            {
                this.Disable();
                exp.Length = Parent.DurTick * Length / 100f / newParent.DurTick;
            }
            else {
                exp.Length = 100;
                this.Length = (Parent.DurTick * Length / 100f - newParent.DurTick) / Parent.DurTick;
            }
            return exp;
        }
        public bool IsEnabled { get; private set; }
        public void Disable()
        {
            _length = 0;
            _depth = 0;
            _period = 0;
            IsEnabled = false;
        }
        public void Enable(bool force = false)
        {
            IsEnabled = true;
            if (force)
            {
                Length = 10;
                Depth = 30;
                Period = -1;
                In = 20;
                Out = 20;
            }
        }
    }
}
