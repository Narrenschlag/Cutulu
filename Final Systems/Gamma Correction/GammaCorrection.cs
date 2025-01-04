using Godot;

namespace Cutulu.Core
{
    public partial class GammaCorrection : ColorRect
    {
        private static GammaCorrection _instance;
        private static float _value;

        public static float Value
        {
            get => _value;

            set
            {
                _value = value;

                if (_instance.NotNull())
                    _instance.Update();
            }
        }

        public override void _EnterTree()
        {
            _instance = this;

            Update();
        }

        private void Update()
        {
            if (Material is ShaderMaterial shader)
                shader.SetShaderParameter("gamma", _value);
        }
    }
}