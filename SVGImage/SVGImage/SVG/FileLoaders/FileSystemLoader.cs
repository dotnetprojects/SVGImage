using System;
using System.IO;

namespace DotNetProjects.SVGImage.SVG.FileLoaders
{
    public class FileSystemLoader : IExternalFileLoader
    {
        static FileSystemLoader()
        {
            Instance = new FileSystemLoader();
        }

        public static FileSystemLoader Instance { get; }

        public Stream LoadFile(string hRef, string svgFilename)
        {
            var path = Environment.CurrentDirectory;
            if (!string.IsNullOrEmpty(svgFilename))
            {
                path = System.IO.Path.GetDirectoryName(svgFilename);
            }
            string filename = System.IO.Path.Combine(path, hRef);
            if (File.Exists(filename))
                return File.OpenRead(filename);
            return null;
        }
    }
}
