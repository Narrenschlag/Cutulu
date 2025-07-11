#if GODOT4_0_OR_GREATER
namespace Cutulu.UI
{
    using Godot;

    using Core;

    [GlobalClass]
    public partial class OptionPointer : PolyControl
    {
        [Export] public Color HoverColor { get; set; } = Colors.Goldenrod;
        [Export] public Color FullColor { get; set; } = Colors.White;

        public GenericOptionMenu Menu;
        private float Value;
        private int Index;

        public virtual void Setup(GenericOptionMenu parent)
        {
            if (parent.IsNull()) throw new System.Exception("No parent menu assigned.");

            Name = "-> Option Pointer";
            Menu = parent;
            Value = 0;

            parent.SetChild(this, 5);
            parent.SetAnchorsPreset(LayoutPreset.FullRect, false);

            QueueRedraw();
        }

        public override void _Draw()
        {
            if (Menu.IsNull() || Menu.Points.IsEmpty()) return;

            var cache = Menu.Points[Index %= Menu.Points.Length];

            var points = new Vector2[cache.Length];
            var colors = new Color[cache.Length];
            for (int i = 0; i < cache.Length; i++)
            {
                points[i] = i < 1 ? cache[2].Lerp(cache[0], Value) : cache[i];
                colors[i] = Value >= 1f ? FullColor : HoverColor;
            }

            DrawPolygon(points, colors);
        }

        public void SetValue(int index, float value01)
        {
            value01 = Mathf.Clamp(value01, 0f, 1f);

            if (Index == index && value01 == Value) return;

            Value = value01;
            Index = index;

            QueueRedraw();
        }
    }
}
#endif