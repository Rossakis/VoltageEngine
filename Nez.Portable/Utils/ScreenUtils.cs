using System;
using Nez;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez.UI;
using Nez.Utils;

public class ScreenUtils
{
    private static bool _isFullscreen = false;
    private static bool _isBorderless = false;
    private static int _width = 0;
    private static int _height = 0;

    public static void SetFullScreenMode()
    {
        Core.Instance.Window.Position = Point.Zero;
        ToggleFullscreen();
    }

    public static void SetWindowedMode()
    {
        Core.Instance.Window.Position = new Point(Screen.MonitorWidth / 4, Screen.MonitorHeight / 4);
        ToggleBorderless();
    }

#if DEBUG
        public static void SetImguiWindowedMode()
        {
            Core.Instance.Window.Position = new Point(Screen.MonitorWidth / 6, Screen.MonitorHeight / 6);
            Screen.SetSize(640 * 4, 360 * 4);
            Core.Instance.Window.IsBorderless = false;
        }
#endif

    public static void ToggleFullscreen()
    {
        var oldIsFullscreen = _isFullscreen;

        if (_isBorderless)
            _isBorderless = false;
        else
            _isFullscreen = !_isFullscreen;

        ApplyFullscreenChange(oldIsFullscreen);
    }

    public static void ToggleBorderless()
    {
        var oldIsFullscreen = _isFullscreen;

        _isBorderless = !_isBorderless;
        _isFullscreen = _isBorderless;

        ApplyFullscreenChange(oldIsFullscreen);
    }

    private static void ApplyFullscreenChange(bool oldIsFullscreen)
    {
        if (_isFullscreen)
        {
            if (oldIsFullscreen)
                ApplyHardwareMode();
            else
                SetFullscreen();
        }
        else
        {
            UnsetFullscreen();
        }
    }

    private static void ApplyHardwareMode()
    {
        Screen.HardwareModeSwitch = !_isBorderless;
        Screen.ApplyChanges();
    }

    private static void SetFullscreen()
    {
        _width = Screen.MonitorWidth / 2;
        _height = Screen.MonitorHeight / 2;

        Screen.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
        Screen.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        Screen.HardwareModeSwitch = !_isBorderless;

        Screen.IsFullscreen = true;
        Screen.ApplyChanges();
    }

    private static void UnsetFullscreen()
    {
        Screen.PreferredBackBufferWidth = _width;
        Screen.PreferredBackBufferHeight = _height;
        Screen.IsFullscreen = false;
        Screen.ApplyChanges();
    }
}