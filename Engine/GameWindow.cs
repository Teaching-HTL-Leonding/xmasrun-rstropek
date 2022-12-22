using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

namespace XmasRun.Engine;

public record struct GameWindowHandlers(
    Action<P5> Draw,
    Action<P5>? Setup = null,
    Action<P5, Key>? KeyDown = null,
    Action<SKPoint>? MouseDown = null,
    Action<SKPoint>? MouseUp = null,
    Func<SKPoint, bool>? MouseMove = null,
    Action<float>? MouseWheel = null,
    WindowOptions? WindowOptions = null
);

public record WindowOptions(
    double? Width = null,
    double? Height = null
);

public class GameApplication : Application
{
    private GameApplication(P5 p5)
    {
        this.MainWindow = new GameWindow(p5);
    }

    public static void Run(GameWindowHandlers handlers)
    {
        ArgumentNullException.ThrowIfNull(handlers);

        Thread.CurrentThread.SetApartmentState(ApartmentState.Unknown);
        Thread.CurrentThread.SetApartmentState(ApartmentState.STA);

        var p5 = new P5(handlers);
        var app = new GameApplication(p5);

        app.Run();
    }
}

file class GameWindow : Window
{
    private readonly P5 p5;
    private DispatcherTimer timer = new();

    public GameWindow(P5 p5)
    {
        this.p5 = p5;

        // Create Skia Element on which we will draw
        var element = new SKElement();
        element.PaintSurface += OnPaintSurface;

        // Create main window. Skia element will be the only child.
        var window = new Window() { Content = element };

        var fixedSize = false;
        if (p5.handlers.WindowOptions?.Width != null)
        {
            window.Width = p5.handlers.WindowOptions.Width.Value;
            fixedSize = true;
        }
        if (p5.handlers.WindowOptions?.Height != null)
        {
            window.Height = p5.handlers.WindowOptions.Height.Value;
            fixedSize = true;
        }
        if (fixedSize)
        {
            window.ResizeMode = ResizeMode.NoResize;
        }

        // Shutdown app if main window is closed.
        window.Closed += (_, args) => Application.Current.Shutdown();

        window.KeyDown += (_, args) =>
        {
            p5.KeyDown(args);
            element.InvalidateVisual();
        };
        window.KeyUp += (_, args) =>
        {
            p5.KeyUp(args);
            element.InvalidateVisual();
        };

        timer.Interval = TimeSpan.FromMilliseconds(1000 / 60);
        timer.Tick += (_, _) => element.InvalidateVisual();
        timer.Start();

        window.Show();
        this.p5 = p5;

        p5.Setup();
    }

    private void OnPaintSurface(object? _, SKPaintSurfaceEventArgs e)
    {
        p5.Draw(e.Surface.Canvas, e.Info);
    }
}
