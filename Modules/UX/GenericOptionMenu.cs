using System;
using Godot;

namespace Cutulu.UX
{
    [GlobalClass]
    public partial class GenericOptionMenu : PolyControl
    {
        [Export] public string BbPrefix { get; set; } = "[b][wave rate=40.0 level=2 connected = 1][pulse rate=40.0 level=2]";
        [Export] public float FillDuration { get; set; } = 1f;

        [Export] public Color BackgroundColor { get; set; } = Colors.SeaGreen;
        [Export] public Color HoverColor { get; set; } = Colors.Goldenrod;
        [Export] public Color PressColor { get; set; } = Colors.White;

        public OptionButton[] Buttons;
        public OptionPointer Pointer;
        public Vector2[][] Points;
        public string[] Options;

        public virtual void Setup(float diameter, params string[] options)
        {
            if (options.IsEmpty()) throw new System.Exception("No options setup for the Option Menu.");

            if (Pointer.IsNull())
            {
                (Pointer = new OptionPointer() { HoverColor = HoverColor, FullColor = PressColor }).
                Setup(this);
            }

            CustomMinimumSize = Vector2.One * diameter;
            Options = options;
            lastIndex = -1;
            Redraw();
        }

        private int lastIndex;
        private float lastValue;
        public void SetValue(int index, float value01)
        {
            Pointer.SetValue(index < 0 ? 0 : index %= Buttons.Length, value01 = Mathf.Min(value01, 1f));

            if (lastIndex != index || (lastValue < 1 != value01 < 1))
            {
                if (lastIndex >= 0) Buttons[lastIndex % Buttons.Length].UpdateText(BackgroundColor);
                if (index >= 0) Buttons[index].UpdateText(value01 < 1 ? HoverColor : PressColor, BbPrefix);
            }

            lastValue = value01;
            lastIndex = index;
        }

        public void Redraw()
        {
            redraw = true;
            QueueRedraw();
        }

        private int count = 0;
        private bool redraw;

        public override void _Notification(int what)
        {
            if (what == NotificationResized) redraw = true;

            base._Notification(what);
        }

        public override void _Draw()
        {
            if (Options.IsEmpty()) return;

            if (redraw && Buttons.NotEmpty())
            {
                for (int i = 0; i < Buttons.Length; i++)
                {
                    Buttons[i].Destroy();
                }
            }

            switch (Options.Length)
            {
                case 1:
                    Draw1();
                    break;

                case 2:
                    DrawN(Vector2.Right, 0.75f);
                    break;

                default:
                    DrawN(Vector2.Up, 0.75f - (0.1f * (Options.Length - 2)));
                    break;
            }

            redraw = false;
        }

        protected void Draw1()
        {
            var points = new Vector2[3]{
                new(0, RHeight),
                new(RWidth / 2 , 0),
                new(RWidth, RHeight)
            };

            var colors = new Color[3]{
                BackgroundColor,
                BackgroundColor,
                BackgroundColor
            };

            DrawPolygon(points, colors);

            if (redraw)
            {
                Points = new Vector2[1][] {
                    new Vector2[4] {
                        points[1],
                        points[2],
                        new (RWidth / 2, RHeight),
                        points[0]
                    }
                };

                Buttons = new OptionButton[1] { new() { Position = RSize * 0.5f } };
                Buttons[0].Setup(this, Options[0], BackgroundColor);
            }
        }

        protected void DrawN(Vector2 up, float indent01)
        {
            var steps = Options.Length * 2;
            var step = 360f / steps;

            var points = new Vector2[steps];
            var colors = new Color[steps];
            for (int i = 0; i < steps; i++)
            {
                colors[i] = BackgroundColor;

                points[i] = GetTip(ref up, step * i, i % 2 != 0 ? Math.Max(indent01, 0f) : 0f);
            }

            DrawPolygon(points, colors);

            if (redraw)
            {
                Buttons = new OptionButton[Options.Length];
                Points = new Vector2[Options.Length][];
                for (int i = 0; i < Points.Length; i++)
                {
                    int k(int offset) => offset < 0 ? k(points.Length + offset) : (i * 2 + offset) % points.Length;
                    Points[i] = new Vector2[4]{
                        points[k(0)],
                        points[k(1)],
                        Vector2.One * 0.5f * RSize,
                        points[k(-1)]
                    };

                    Buttons[i] = new() { Position = GetTip(ref up, step * i * 2, -0.2f) };
                    Buttons[i].Setup(this, Options[i], BackgroundColor);

                    Buttons[i].Position += Buttons[i].GetRect().Size * 0.5f * up.RotatedD(step * i * 2);
                }
            }
        }

        protected Vector2 GetTip(ref Vector2 up, float angle, float indent01)
        {
            return (Vector2.One + up.RotatedD(angle) * (1f - indent01)) * RSize * 0.5f;
        }

        public virtual int GetIndex(float angleUp)
        {
            if (Options.IsEmpty()) return default;

            switch (Options.Length)
            {
                case 2:
                    angleUp = (angleUp + 270) % 360;
                    break;

                default:
                    angleUp %= 360;
                    break;
            }

            return Mathf.RoundToInt(Options.Length * angleUp / 360);
        }
    }
}