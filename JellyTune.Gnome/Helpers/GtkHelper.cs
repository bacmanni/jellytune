using GLib;
using SkiaSharp;

namespace JellyTune.Gnome.Helpers;

public abstract class GtkHelper
{
    public static void GtkDispatch(Action action)
    {
        MainContext.Default().InvokeFull(0, () =>
        {
            action();
            return false;
        });
    }

    public static byte[] CreateBlurredBytes(byte[] imageBytes, float blurRadius = 20f)
    {
        // 1. Decode byte array directly into an SKBitmap
        using var originalBitmap = SKBitmap.Decode(imageBytes);

        if (originalBitmap == null)
            throw new System.Exception("Failed to decode image bytes.");

        // 2. Create an off-screen surface matching the image dimensions
        using var surface = SKSurface.Create(new SKImageInfo(originalBitmap.Width, originalBitmap.Height));
        var canvas = surface.Canvas;

        // 3. Configure the paint filter with a Gaussian blur
        using var paint = new SKPaint
        {
            ImageFilter = SKImageFilter.CreateBlur(blurRadius, blurRadius)
        };

        // 4. Draw the bitmap onto the canvas through the blur filter
        canvas.DrawBitmap(originalBitmap, 0, 0, paint);
        canvas.Flush();

        // 5. Snapshot the surface and encode it back to PNG bytes in memory
        using var snapshot = surface.Snapshot();
        using var data = snapshot.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}