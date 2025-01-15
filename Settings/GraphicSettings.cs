namespace Cutulu.Settings
{
    using Cutulu.Core;
    using Godot;

    public static class GraphicSettings
    {
        public static T GetValue<T>(string key, T defaultValue) => AppData.GetAppData($"graphic_settings/{key}", defaultValue);
        public static void SetValue<T>(string key, T value) => AppData.SetAppData($"graphic_settings/{key}", value);

        public static System.Action Updated { get; set; }

        public static void LoadValues()
        {
            SetFullscreen(GetValue(nameof(Fullscreen), true), false);
            SetVSync(GetValue(nameof(VSync), true), false);

            ScaleWindow(GetValue(nameof(ResolutionMode), Viewport.Scaling3DModeEnum.Bilinear), false);
            ScaleWindow(GetValue(nameof(ResolutionScale), 1f), false);

            SetWindowSize(GetValue(nameof(WindowSize), new Vector2I(1280, 720)), false);
            CenterWindow();

            Updated?.Invoke();
        }

        public static bool Fullscreen
        {
            get => DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen;
            set => SetFullscreen(value);
        }

        private static void SetFullscreen(bool enabled, bool update = true)
        {
            if (enabled && Fullscreen == false)
            {
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
            }

            else if (enabled == false && Fullscreen)
            {
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Maximized);
            }

            if (update)
            {
                SetValue(nameof(Fullscreen), enabled);
                Updated?.Invoke();
            }
        }

        public static bool VSync
        {
            get => DisplayServer.WindowGetVsyncMode() == DisplayServer.VSyncMode.Enabled;
            set => SetVSync(value);
        }

        private static void SetVSync(bool enabled, bool update = true)
        {
            DisplayServer.WindowSetVsyncMode(enabled ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);

            if (update)
            {
                SetValue(nameof(VSync), enabled);
                Updated?.Invoke();
            }
        }

        public static Vector2I WindowSize
        {
            get => DisplayServer.WindowGetSize();
            set => SetWindowSize(value);
        }

        private static void SetWindowSize(Vector2I size, bool update = true)
        {
            DisplayServer.WindowSetSize(size);

            if (update)
            {
                SetValue(nameof(WindowSize), size);
                Updated?.Invoke();
            }
        }

        public static float ResolutionScale
        {
            get => Nodef.Main.GetViewport().Scaling3DScale;
            set => ScaleWindow(value);
        }

        private static void ScaleWindow(float scale, bool update = true)
        {
            Nodef.Main.GetViewport().Scaling3DScale = scale;

            if (update)
            {
                SetValue(nameof(ResolutionScale), scale);
                Updated?.Invoke();
            }
        }

        public static Viewport.Scaling3DModeEnum ResolutionMode
        {
            get => Nodef.Main.GetViewport().Scaling3DMode;
            set => ScaleWindow(value);
        }

        private static void ScaleWindow(Viewport.Scaling3DModeEnum mode, bool update = true)
        {
            Nodef.Main.GetViewport().Scaling3DMode = mode;

            if (update)
            {
                SetValue(nameof(ResolutionMode), mode);
                Updated?.Invoke();
            }
        }

        public static Vector2I ScreenSize => DisplayServer.ScreenGetSize();

        public static void CenterWindow()
        {
            var window = Nodef.Main.GetWindow();

            var center = DisplayServer.ScreenGetPosition() + ScreenSize / 2;
            var size = window.GetSizeWithDecorations();

            window.Position = center - size / 2;
        }

        public static void ResizeWindow(Vector2I resolution)
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);

            SetWindowSize(resolution, false);
            CenterWindow();
        }
    }
}