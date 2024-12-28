namespace Cutulu.Core
{
    using Godot;

    using P = CinemaPerspective;
    public partial class Cinema : Node3D
    {
        [ExportGroup("Nodes")]
        [Export] public Node3D GlobalPositionPivot;
        [Export] public Node3D LocalPositionPivot;
        [Export] public Node3D LocalRotationPivot;
        [Export] public Node3D RotationYPivot;
        [Export] public Node3D RotationXPivot;

        [ExportGroup("Integrated Perspectives")]
        [Export] public int DefaultPerspectiveIndex;
        [Export] public P[] Perspectives;

        private float xRotation;
        private Vector2 input;
        private Node3D target;

        private static Cinema Singleton;
        private static P perspective;

        #region Overrides
        public override void _EnterTree()
        {
            if (Singleton.NotNull())
            {
                Singleton.Destroy();
            }

            Singleton = this;
        }

        public override void _Process(double delta)
        {
            if (target.IsNull())
            {
                return;
            }

            if (perspective.IsNull())
            {
                Perspective(Perspectives.GetClampedElement(DefaultPerspectiveIndex));
            }

            if (perspective.UsePhysicsUpdate == false)
            {
                MoveCamera((float)delta);
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            if (target.IsNull() || perspective.IsNull())
            {
                return;
            }

            if (perspective.UsePhysicsUpdate)
            {
                MoveCamera((float)delta);
            }
        }
        #endregion

        #region Static
        public static Vector2 RelativeMouseMovement
        {
            set
            {
                if (Singleton.NotNull())
                {
                    Singleton.input = value;
                }
            }
        }

        public static void Target(Node3D target, P perspective, bool instant = true)
        {
            Perspective(perspective);
            Target(target, instant);
        }

        public new static void SetGlobalPosition(Vector3 globalPosition)
        {
            if (Singleton.NotNull() && perspective.NotNull())
            {
                Singleton.SetGlobalPosition(globalPosition, false);
            }
        }

        public static void SetGlobalPosition(Node3D target, Vector3 globalPosition)
        {
            if (Singleton.NotNull() && target == Singleton.target)
            {
                SetGlobalPosition(globalPosition);
            }
        }

        public static void Target(Node3D target, bool instant = true)
        {
            if (Singleton.IsNull())
            {
                "No cinema instance established.".LogError();
                return;
            }

            Singleton.target = target;

            if (instant)
            {
                SetGlobalPosition(target.GlobalPosition);
            }
        }

        public static void Perspective(byte i)
        {
            if (Singleton.IsNull())
            {
                return;
            }

            Perspective(Singleton.Perspectives.GetClampedElement(i));
        }

        public static void Perspective(P perspective)
        => Cinema.perspective = perspective;

        public static bool TryGetRotation(out Vector3 Rotation)
        {
            if (Singleton.NotNull())
            {
                Rotation = Singleton.RotationYPivot.Rotation;
                return true;
            }

            else
            {
                Rotation = default;
                return false;
            }
        }
        #endregion

        private void SetGlobalPosition(Vector3 globalPosition, bool ignoreOffset = true)
        {
            GlobalPositionPivot.GlobalPosition = ignoreOffset == false ?
                PosPlusOffset(globalPosition) :
                globalPosition;
        }

        private Vector3 PosPlusOffset(Vector3 globalPosition)
        {
            globalPosition += Vector3.Up * perspective.GlobalOffset.Y;

            if (perspective.GlobalOffset.X != 0)
            {
                globalPosition += RotationYPivot.Right() * perspective.GlobalOffset.X;
            }

            if (perspective.GlobalOffset.Z != 0)
            {
                globalPosition += RotationYPivot.Forward() * perspective.GlobalOffset.Z;
            }

            return globalPosition;
        }

        public static void EnsureCamera()
        {
            if (Singleton.NotNull())
            {
                Singleton.MoveCamera(0);
            }
        }

        private void MoveCamera(float delta)
        {
            // Read local input
            Vector2 input = this.input;
            this.input = Vector2.Zero;

            // Apply positioning
            this.SetGlobalPosition(perspective.LerpPosition ?
                GlobalPositionPivot.GlobalPosition.Lerp(PosPlusOffset(target.GlobalPosition), delta * perspective.LerpSpeed) :
                PosPlusOffset(target.GlobalPosition)
            );

            // Left/Right
            if (RotationYPivot.NotNull())
            {
                RotationYPivot.RotateY(perspective.RotationSpeedY.toRadians() * input.X * delta);
            }

            // Up/Down
            if (RotationXPivot.NotNull())
            {
                xRotation = Mathf.Clamp(xRotation + perspective.RotationSpeedX * input.Y * delta, perspective.MinAngleX, perspective.MaxAngleX);
                RotationXPivot.Rotation = RotationXPivot.Rotation.setX(xRotation.toRadians());
            }

            // Perspective Offset
            if (LocalPositionPivot.NotNull())
            {
                LocalPositionPivot.Position = LocalPositionPivot.Position.Lerp(perspective.LocalOffset, 8 * delta);
            }

            // Perspective Angle
            if (LocalRotationPivot.NotNull())
            {
                LocalRotationPivot.Rotation = LocalRotationPivot.Rotation.setX(Mathf.LerpAngle(LocalRotationPivot.Rotation.X, perspective.CameraAngle, delta * 8));
            }
        }
    }
}