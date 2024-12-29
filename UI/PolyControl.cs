namespace Cutulu.UI
{
    using System;
    using Godot;

    using Core;

    public partial class PolyControl : Control
    {
        // Points of the triangle relative to the control's size
        protected Vector2[] points;
        protected Color[] colors;

        public float RHeight => GetRect().Size.Y;
        public float RWidth => GetRect().Size.X;
        public Vector2 RSize => GetRect().Size;

        public override void _Ready()
        {
            // Initialize the points
            _DefineShape();
        }

        public override void _Draw()
        {
            if (points.Size() < 1) return;

            #region Handle Colors
            if (colors.Size() < points.Length)
            {
                var array = new Color[points.Length];

                if (colors != null)
                {
                    Array.Copy(colors, array, colors.Length);

                    for (int i = colors.Length; i < array.Length; i++)
                    {
                        array[i] = colors[^1];
                    }
                }

                else
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        array[i] = Colors.White;
                    }
                }

                colors = array;
            }

            else if (colors.Length > points.Length)
            {
                var array = new Color[points.Length];
                Array.Copy(colors, array, array.Length);
                colors = array;
            }
            #endregion

            // Draw the triangle
            DrawPolygon(points, colors);
        }

        /// <summary>
        /// Sets the protected values 'points' and 'colors'.
        /// </summary>
        protected virtual void _DefineShape() { }

        public override void _Notification(int what)
        {
            if (what == NotificationResized)
            {
                // Update the triangle points when the size of the control changes
                _DefineShape();

                // Redraw the control
                QueueRedraw();
            }
        }

        protected void SetTriangle()
        {
            points = new Vector2[]{
                new(0, RHeight),
                new(RWidth / 2, 0),
                new(RWidth, RHeight),
            };

            colors = new Color[]{
                Colors.Red,
                Colors.Green,
                Colors.Blue
            };
        }
    }
}