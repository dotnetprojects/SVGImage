using System.IO;

namespace SVGImage.SVG.FileLoaders
{
    public interface IExternalFileLoader
    {
        Stream LoadFile(string hRef, string svgFilename);
    }
}
