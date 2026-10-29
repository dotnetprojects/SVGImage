using System;
using System.IO;
using System.Windows;

namespace SVGImage.SVG.FileLoaders
{
    // Resolve an embedded SVG's companion CSS/images beside that SVG resource.
    internal sealed class ResourceFileLoader : IExternalFileLoader
    {
        private readonly Uri source;
        private readonly IExternalFileLoader fallback;
        internal ResourceFileLoader(Uri source, IExternalFileLoader fallback)
        {
            this.source = source;
            this.fallback = fallback;
        }
        internal Uri Resolve(string href) => new Uri(source,href);
        internal ResourceFileLoader RelativeTo(string href) => new ResourceFileLoader(Resolve(href),fallback);
        public Stream LoadFile(string href, string svgFilename)
        {
            Uri resource;
            if (Uri.TryCreate(source, href, out resource) && resource.Scheme == "pack")
            {
                try
                {
                    var info = resource.ToString().IndexOf("siteoforigin", StringComparison.OrdinalIgnoreCase) >= 0
                        ? Application.GetRemoteStream(resource) : Application.GetResourceStream(resource);
                    if (info != null) return info.Stream;
                }
                catch (IOException) { }
            }
            return fallback == null ? null : fallback.LoadFile(href, svgFilename);
        }
    }
}
