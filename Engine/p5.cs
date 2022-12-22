using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using SkiaSharp;
using SkiaSharp.Views.WPF;

namespace XmasRun.Engine;

public partial class P5
{
    internal GameWindowHandlers handlers;

    private State CurrentState = State.Undefined;
    private SKCanvas? canvas;
    private SKImageInfo imageInfo;
    private SKColor? BackgroundColor;
    private SKPaint BitmapPaint = new()
    {
        FilterQuality = SKFilterQuality.High,
        IsAntialias = false,
        IsDither = false,
    };
    private Dictionary<string, P5Image> ImageCache = new();
    private Key lastKey;
    private List<Key> pressedKeys = new();
    private SKColor? CurrentStrokeColor;
    private float CurrentStrokeWeight = 0f;
    private float CurrentTextSize = 0f;

    private enum State
    {
        Undefined,
        InDraw,
    }

    public P5(GameWindowHandlers handlers)
    {
        this.handlers = handlers;
    }

    internal void KeyDown(KeyEventArgs key)
    {
        lastKey = key.Key;
        pressedKeys.Add(key.Key);

        if (handlers.KeyDown != null) { handlers.KeyDown(this, lastKey); }
    }

    internal void KeyUp(KeyEventArgs key)
    {
        lastKey = key.Key;
        pressedKeys.Remove(key.Key);

        if (handlers.KeyDown != null) { handlers.KeyDown(this, lastKey); }
    }

    internal void Setup()
    {
        if (handlers.Setup != null) { handlers.Setup(this); }
    }

    internal void Draw(SKCanvas canvas, SKImageInfo imageInfo)
    {
        if (CurrentState == State.InDraw) { return; }

        CurrentStrokeColor = null;
        CurrentStrokeWeight = 1f;
        CurrentTextSize = 12f;

        this.canvas = canvas;
        this.imageInfo = imageInfo;
        CurrentState = State.InDraw;

        try
        {
            if (BackgroundColor != null) { canvas.Clear(BackgroundColor.Value); }

            handlers.Draw(this);
        }
        finally
        {
            CurrentState = State.Undefined;
        }
    }

    private SKColor BuildColor(string color)
    {
        SKColor skColor;
        if (color.StartsWith("#"))
        {
            skColor = SKColor.Parse(color);
        }
        else
        {
            var colorFieldInfo = typeof(SKColors).GetField(color, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
            if (colorFieldInfo == null) { throw new ArgumentException($"Color {color} not found."); }
            skColor = (SKColor)colorFieldInfo.GetValue(null)!;
        }

        return skColor;
    }

    /// <summary>
    /// Sets the background color of the canvas.
    /// </summary>
    /// <param name="color">The color to set the background to. See remarks for details.</param>
    /// <remarks>
    /// The color can be specified in the following ways:
    /// <list type="bullet">
    /// <item>As a hex string starting with a # character, e.g. "#00ccff"</item>
    /// <item>As a named color, e.g. "Red" (for a list of colors see https://learn.microsoft.com/en-us/dotnet/api/skiasharp.skcolors)</item>
    /// </list>
    /// </remarks>
    public void Background(string color)
    {
        var skColor = BuildColor(color);
 
        if (CurrentState == State.InDraw && canvas != null)
        {
            // Method called during Draw
            canvas.Clear(skColor);
        }
        else
        {
            // Method called during Setup
            BackgroundColor = skColor;
        }
    }

    /// <summary>
    /// Loads an image from the application resources.
    /// </summary>
    /// <param name="resourceName">The name of the resource.</param>
    /// <remarks>
    /// The resource name must be a file name or the name of a WPF
    /// embedded resource. Note that images are cached. Multiple calls
    /// with the same resource name will return the same instance
    /// of <see cref="P5Image"/>.
    /// </remarks>
    public P5Image LoadImage(string resourceName)
    {
        if (ImageCache.TryGetValue(resourceName, out var image)) { return image; }

        image = new P5Image(resourceName);
        ImageCache[resourceName] = image;
        return image;
    }

    /// <summary>
    /// Draws an image to the canvas.
    /// </summary>
    /// <param name="image">The image to draw.</param>
    /// <param name="x">The x coordinate of the top left corner of the image.</param>
    /// <param name="y">The y coordinate of the top left corner of the image.</param>
    public void Image(P5Image image, float x, float y)
    {
        if (CurrentState != State.InDraw) { throw new InvalidOperationException("Image can only be called during Draw."); }
        if (canvas == null) { throw new InvalidOperationException("Canvas is null."); }

        canvas.DrawImage(image.image, x, y, BitmapPaint);
    }

    /// <summary>
    /// Draws an image to the canvas.
    /// </summary>
    /// <param name="resourceName">The name of the resource (will be loaded with <see cref="LoadImage"/>).</param>
    /// <param name="x">The x coordinate of the top left corner of the image.</param>
    /// <param name="y">The y coordinate of the top left corner of the image.</param>
    public void Image(string resourceName, float x, float y)
        => Image(LoadImage(resourceName), x, y);

    /// <summary>
    /// Draws an image to the canvas.
    /// </summary>
    /// <param name="image">The image to draw.</param>
    public void Image(P5Image image) => Image(image, 0, 0);

    /// <summary>
    /// Draws an image to the canvas.
    /// </summary>
    /// <param name="resourceName">The name of the resource (will be loaded with <see cref="LoadImage"/>).</param>
    public void Image(string resourceName) => Image(LoadImage(resourceName), 0, 0);

    /// <summary>
    /// Draws an image to the canvas.
    /// </summary>
    /// <param name="image">The image to draw.</param>
    /// <param name="x">The x coordinate of the top left corner of the image.</param>
    /// <param name="y">The y coordinate of the top left corner of the image.</param>
    /// <param name="width">The width of the image.</param>
    /// <param name="height">The height of the image.</param>
    public void Image(P5Image image, float x, float y, float width, float height)
    {
        if (CurrentState != State.InDraw) { throw new InvalidOperationException("Image can only be called during Draw."); }
        if (canvas == null) { throw new InvalidOperationException("Canvas is null."); }

        canvas.DrawImage(image.image, new SKRect(x, y, x + width, y + height), BitmapPaint);
    }

    /// <summary>
    /// Draws an image to the canvas.
    /// </summary>
    /// <param name="resourceName">The name of the resource (will be loaded with <see cref="LoadImage"/>).</param>
    /// <param name="x">The x coordinate of the top left corner of the image.</param>
    /// <param name="y">The y coordinate of the top left corner of the image.</param>
    /// <param name="width">The width of the image.</param>
    /// <param name="height">The height of the image.</param>
    public void Image(string resourceName, float x, float y, float width, float height)
        => Image(LoadImage(resourceName), x, y, width, height);

    /// <summary>
    /// Gets the width of the canvas.
    /// </summary>
    public float Width => imageInfo.Width;

    /// <summary>
    /// Gets the height of the canvas.
    /// </summary>
    public float Height => imageInfo.Height;

    /// <summary>
    /// Saves the current drawing style settings and transformations.
    /// </summary>
    /// <remarks>
    /// The <see cref="Push"/> and <see cref="Pop"/> methods allow you to change the
    /// drawing style and transformation settings and later return to what you had.
    /// Note that these methods are always used together. They allow you to change the
    /// style and transformation settings around a block of code without having to
    /// worry about what came before or after.
    /// </remarks>
    public void Push()
    {
        if (CurrentState != State.InDraw) { throw new InvalidOperationException("Push can only be called during Draw."); }
        if (canvas == null) { throw new InvalidOperationException("Canvas is null."); }

        canvas.Save();
    }

    /// <summary>
    /// Restores the previous drawing style settings and transformations.
    /// </summary>
    /// <remarks>
    /// The <see cref="Push"/> and <see cref="Pop"/> methods allow you to change the
    /// drawing style and transformation settings and later return to what you had.
    /// Note that these methods are always used together. They allow you to change the
    /// style and transformation settings around a block of code without having to
    /// worry about what came before or after.
    /// </remarks>
    public void Pop()
    {
        if (CurrentState != State.InDraw) { throw new InvalidOperationException("Pop can only be called during Draw."); }
        if (canvas == null) { throw new InvalidOperationException("Canvas is null."); }

        canvas.Restore();
    }

    public void Scale(float x, float y)
    {
        if (CurrentState != State.InDraw) { throw new InvalidOperationException("Scale can only be called during Draw."); }
        if (canvas == null) { throw new InvalidOperationException("Canvas is null."); }

        canvas.Scale(x, y);
    }

    public void Scale(float scale)
    {
        if (CurrentState != State.InDraw) { throw new InvalidOperationException("Scale can only be called during Draw."); }
        if (canvas == null) { throw new InvalidOperationException("Canvas is null."); }

        canvas.Scale(scale);
    }

    public void Translate(float x, float y)
    {
        if (CurrentState != State.InDraw) { throw new InvalidOperationException("Translate can only be called during Draw."); }
        if (canvas == null) { throw new InvalidOperationException("Canvas is null."); }

        canvas.Translate(x, y);
    }

    public void Translate(float distance)
    {
        if (CurrentState != State.InDraw) { throw new InvalidOperationException("Translate can only be called during Draw."); }
        if (canvas == null) { throw new InvalidOperationException("Canvas is null."); }

        canvas.Translate(distance, distance);
    }

    public void Stroke(string color) => CurrentStrokeColor = BuildColor(color);
    public void NoStroke() => CurrentStrokeColor = null;

    public void StrokeWidth(float width) => CurrentStrokeWeight = width;

    public void TextSize(float size) => CurrentTextSize = size;
    
    public void Rect(float x, float y, float width, float height)
    {
        if (CurrentState != State.InDraw) { throw new InvalidOperationException("Rect can only be called during Draw."); }
        if (canvas == null) { throw new InvalidOperationException("Canvas is null."); }

        if (CurrentStrokeColor != null)
        {
            var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                Color = CurrentStrokeColor ?? SKColors.Black,
                StrokeWidth = CurrentStrokeWeight,
            };

            canvas.DrawRect(x, y, width, height, paint);
        }
    }

    public bool KeyIsDown(Key key) => Keyboard.IsKeyDown(key);

    public bool DoesCollide(float x1, float y1, float width1, float height1, float x2, float y2, float width2, float height2)
    {
        var rect1 = new SKRect(x1, y1, x1 + width1, y1 + height1);
        var rect2 = new SKRect(x2, y2, x2 + width2, y2 + height2);

        return rect1.IntersectsWith(rect2);
    }

    public void Text(string text, float x, float y)
    {
        if (CurrentState != State.InDraw) { throw new InvalidOperationException("Text can only be called during Draw."); }
        if (canvas == null) { throw new InvalidOperationException("Canvas is null."); }

        var paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = CurrentStrokeColor ?? SKColors.Black,
            TextSize = CurrentTextSize,
            TextAlign = SKTextAlign.Center,
        };

        canvas.DrawText(text, x, y, paint);
    }
}

public class P5Image
{
    internal SKImage image;
    internal string ResourceName;

    public P5Image(string resourceName)
    {
        Stream? inputStream = null;
        try
        {
            if (File.Exists(resourceName))
            {
                inputStream = File.OpenRead(resourceName);
            }
            else
            {
                inputStream = Application.GetResourceStream(new Uri(resourceName, UriKind.Relative)).Stream;
            }

            using var bitmap = SKBitmap.Decode(inputStream);
            image = SKImage.FromBitmap(bitmap);
            this.ResourceName = resourceName;
        }
        finally
        {
            inputStream?.Dispose();
        }
    }

    public int Height => image.Height;

    public int Width => image.Width;
}