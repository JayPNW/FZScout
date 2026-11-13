#:package Magick.NET-Q8-AnyCPU

// Renders an SVG as a square PNG at each size.
// Usage: dotnet run scripts/svg-to-pngs.cs -- <picture.svg> <output directory> <size>...
using ImageMagick;

var svgPath = args[0];
var outputDirectory = args[1];
foreach (var size in args[2..].Select(uint.Parse))
{
    var settings = new MagickReadSettings
    {
        BackgroundColor = MagickColors.Transparent,
        Width = size,
        Height = size,
    };
    using var image = new MagickImage(svgPath, settings);
    image.Write(Path.Combine(outputDirectory, $"{size}.png"), MagickFormat.Png32);
}
