using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SVGImage.SVG;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        string directory = Path.GetFullPath(args.Length > 0 ? args[0] : "Docs/images/demos");
        foreach (string source in Directory.GetFiles(directory, "*.svg"))
        {
            bool animated = Path.GetFileNameWithoutExtension(source) == "animation";
            var renderer = new SVGRender { UseAnimations = animated };
            var drawing = renderer.LoadDrawing(source);
            var box = renderer.SVG.ViewBox.Value;
            var gif = new GifBitmapEncoder();
            for (int frame = 0; frame < (animated ? 40 : 1); frame++)
            {
                if (animated) renderer.SeekAnimations(TimeSpan.FromSeconds(frame * .1));
                var visual = new DrawingVisual();
                using (var dc = visual.RenderOpen()) dc.DrawDrawing(drawing);
                var bitmap = new RenderTargetBitmap((int)box.Width, (int)box.Height, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(visual);
                if (!animated || frame == 20)
                {
                    var png = new PngBitmapEncoder();
                    png.Frames.Add(BitmapFrame.Create(bitmap));
                    using (var file = File.Create(Path.ChangeExtension(source, ".png"))) png.Save(file);
                }
                if (animated)
                {
                    var metadata = new BitmapMetadata("gif");
                    metadata.SetQuery("/grctlext/Delay", (ushort)10);
                    metadata.SetQuery("/grctlext/Disposal", (byte)2);
                    if (frame == 0)
                    {
                        metadata.SetQuery("/appext/application", Encoding.ASCII.GetBytes("NETSCAPE2.0"));
                        metadata.SetQuery("/appext/data", new byte[] { 3, 1, 0, 0, 0 });
                    }
                    gif.Frames.Add(BitmapFrame.Create(bitmap, null, metadata, null));
                }
            }
            if (animated)
            {
                using (var encoded = new MemoryStream())
                {
                    gif.Save(encoded);
                    File.WriteAllBytes(Path.ChangeExtension(source, ".gif"), WithGifTiming(encoded.ToArray()));
                }
            }
            Console.WriteLine("Rendered " + Path.GetFileName(source));
        }
    }

    // WIC's GIF encoder can discard animation metadata. Set timing/loop blocks
    // explicitly while preserving its image data and palette/transparency flags.
    private static byte[] WithGifTiming(byte[] data)
    {
        int position = 13 + ((data[10] & 128) != 0 ? 3 * (1 << ((data[10] & 7) + 1)) : 0);
        using (var output = new MemoryStream())
        {
            data[4] = (byte)'9';
            output.Write(data, 0, position);
            var loop = new byte[] { 33, 255, 11, 78, 69, 84, 83, 67, 65, 80, 69, 50, 46, 48, 3, 1, 0, 0, 0 };
            output.Write(loop, 0, loop.Length);
            bool control = false;
            while (position < data.Length && data[position] != 59)
            {
                int start = position;
                if (data[position] == 33)
                {
                    if (data[position + 1] == 249)
                    {
                        data[position + 4] = 10;
                        data[position + 5] = 0;
                        control = true;
                    }
                    position += 2;
                }
                else if (data[position] == 44)
                {
                    if (!control) output.Write(new byte[] { 33, 249, 4, 0, 10, 0, 0, 0 }, 0, 8);
                    control = false;
                    int packed = data[position + 9];
                    position += 10 + ((packed & 128) != 0 ? 3 * (1 << ((packed & 7) + 1)) : 0);
                    position++; // LZW minimum code size.
                }
                else throw new InvalidDataException("Unexpected GIF block.");
                while (data[position] != 0) position += data[position] + 1;
                position++;
                output.Write(data, start, position - start);
            }
            output.WriteByte(59);
            return output.ToArray();
        }
    }
}
