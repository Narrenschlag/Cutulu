namespace Cutulu.Core
{
    using Godot;

    public partial class GammaCorrection : ColorRect
    {
        private const string GammaPath = "app/gamma";
        private const float DefaultValue = 1f;

        private static GammaCorrection Instance;

        public static float Value
        {
            get => AppData.GetAppData(GammaPath, DefaultValue);

            set
            {
                AppData.SetAppData(GammaPath, value);

                if (Instance.NotNull())
                    Instance.Update(value);
            }
        }

        public override void _EnterTree()
        {
            Instance = this;

            Update(Value);
        }

        private void Update(float val)
        {
            if (Material is ShaderMaterial shader)
                shader.SetShaderParameter("gamma", val);
        }
    }
}