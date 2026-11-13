#:package Magick.NET-Q8-AnyCPU

// Combines PNGs, one per size, into a single .ico.
// Usage: dotnet run scripts/pngs-to-ico.cs -- <output.ico> <size.png>...
using ImageMagick;

var iconPath = args[0];
using var images = new MagickImageCollection();
foreach (var pngPath in args[1..])
{
    var image = new MagickImage(pngPath);
    // Leaving out the PNG date and time chunks keeps the same picture producing the same bytes.
    image.Settings.SetDefine(MagickFormat.Png, "exclude-chunk", "date,time");
    // Entries this size and up are stored as PNG, which is what Windows expects at 256; smaller ones as 32-bit BMPs, which everything reads.
    image.Settings.SetDefine(MagickFormat.Icon, "png-compression-size", "256");
    images.Add(image);
}
images.Write(iconPath, MagickFormat.Ico);
