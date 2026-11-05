using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FZScout.Tests.Xaml;

/// <summary>Draws an element as a PNG without showing it on screen, at 96 DPI so the size doesn't depend on the display. Call on the WPF thread.</summary>
static class Screenshot
{
    const double DeviceIndependentDpi = 96;

    /// <summary>
    /// A window that was never shown doesn't lay itself out, so this draws what's inside its frame: its background and content.
    /// The title bar and border are drawn by Windows, so they aren't included.
    /// </summary>
    public static byte[] TakePng(Window window)
    {
        window.ApplyTemplate();
        var insideFrame = (FrameworkElement)VisualTreeHelper.GetChild(window, 0);
        return TakePng(insideFrame, new Size(SizeOrUnlimited(window.Width), SizeOrUnlimited(window.Height)));
    }

    /// <summary>Lays the element out at the size it asks for.</summary>
    public static byte[] TakePng(FrameworkElement element) => TakePng(element, new Size(double.PositiveInfinity, double.PositiveInfinity));

    /// <summary>Lays out twice: bindings finish only once the first layout has run, and what they show can change the size.</summary>
    static byte[] TakePng(FrameworkElement element, Size availableSize)
    {
        LayOut(element, availableSize);
        WpfThread.RunQueuedWork();
        LayOut(element, availableSize);

        var bitmap = new RenderTargetBitmap(
            (int)Math.Ceiling(element.ActualWidth),
            (int)Math.Ceiling(element.ActualHeight),
            DeviceIndependentDpi,
            DeviceIndependentDpi,
            PixelFormats.Pbgra32);
        bitmap.Render(element);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var png = new MemoryStream();
        encoder.Save(png);
        return png.ToArray();
    }

    /// <summary>Fills the available size in each direction it's limited, like a window with a set Width or Height.</summary>
    static void LayOut(FrameworkElement element, Size availableSize)
    {
        element.Measure(availableSize);
        var width = Math.Max(element.DesiredSize.Width, LimitOrZero(availableSize.Width));
        var height = Math.Max(element.DesiredSize.Height, LimitOrZero(availableSize.Height));
        element.Arrange(new Rect(0, 0, width, height));
        element.UpdateLayout();
    }

    static double LimitOrZero(double size)
    {
        if (double.IsPositiveInfinity(size))
        {
            return 0;
        }

        return size;
    }

    /// <summary>A window sized to its content has no Width or Height set.</summary>
    static double SizeOrUnlimited(double windowSize)
    {
        if (double.IsNaN(windowSize))
        {
            return double.PositiveInfinity;
        }

        return windowSize;
    }
}
