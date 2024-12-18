namespace Cutulu
{
    using Godot;

    public partial class HexagonGrid<VALUE> : Node3D where VALUE : Node3D
    {
        public VALUE[] Values { get; private set; }
        public int Range { get; private set; }

        protected Vector3 rotation { get; set; }
        protected Orientation orientation { get; set; }

        public virtual Orientation Orientation
        {
            get
            {
                if (rotation != GlobalRotation || orientation == default)
                {
                    orientation = new(this);
                }

                return orientation;
            }
        }

        public virtual VALUE GetNewValue(int index, Color vertexColor = default) => default;

        public virtual void Generate(int ringCount)
        {
            this.Clear();

            ringCount = Mathf.Max(ringCount, 1);
            Range = ringCount;

            Values = new VALUE[Hexagon.GetCellCountInRange(ringCount)];
            var o = Orientation;

            for (int index = 0; index < Values.Length; index++)
                GenerateIndex(index, o, Colors.DimGray);
        }

        public virtual VALUE GenerateIndex(int index, Orientation orientation, Color vertexColor = default)
        {
            var value = GetNewValue(index, vertexColor);

            value.GlobalPosition = Hexagon.ToPosition(index, orientation);
            value.Rotation = -GlobalRotation;

            if (index >= 0 && index < Values.Length) Values[index] = value;

            return value;
        }
    }
}