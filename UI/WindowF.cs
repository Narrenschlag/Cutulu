namespace Cutulu.UI
{
    using Godot;

    using Core;

    static class WindowF
    {
        /// <summary>
        /// Clamps user interface window to given viewport/host window
        /// <br/>to prevent it from leaving the screen.
        /// </summary>
        public static bool ClampToViewport(this Window window, Node hostWindow, int titleMargin = 35, int PixelMargin = 0)
        {
            // Try to use main window if no host window defined
            if (hostWindow.IsNull()) hostWindow = Nodef.Main;

            // No host window to clamp to
            if (hostWindow.IsNull()) return false;
            Vector2I LeftTop = window.Position;

            // X clamp
            if (LeftTop.X < PixelMargin.max(0)) LeftTop.X = 0;
            else
            {
                int max = (int)hostWindow.GetViewport().GetVisibleRect().Size.X;
                int width = (int)window.GetVisibleRect().Size.X;

                int curMax = LeftTop.X + width;
                if (curMax > max - PixelMargin.max(0)) LeftTop.X = max - width;
            }

            // Y clamp
            if (LeftTop.Y < titleMargin + PixelMargin.max(0)) LeftTop.Y = titleMargin;
            {
                int max = (int)hostWindow.GetViewport().GetVisibleRect().Size.Y;
                int height = (int)window.GetVisibleRect().Size.Y;

                int curMax = LeftTop.Y + height;
                if (curMax > max - PixelMargin.max(0)) LeftTop.Y = max - height;
            }

            // Apply position
            window.Position = LeftTop;
            return true;
        }
    }
}