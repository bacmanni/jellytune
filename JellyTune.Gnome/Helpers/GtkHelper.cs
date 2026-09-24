using Gdk;
using GLib;

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

    public static Gdk.Texture? CreateBlurredTextureFromBytes(byte[]? imageBytes, float blurRadius = 20f)
    {
        using var image = NetVips.Image.NewFromBuffer(imageBytes);
        using var blurred = image.Gaussblur(blurRadius);
    
        // 3. Export back to memory as PNG bytes
        var pngBytes = blurred.WriteToBuffer(".png");

        using var glibBytes = GLib.Bytes.New(pngBytes);
        return Gdk.Texture.NewFromBytes(glibBytes);
    }
}