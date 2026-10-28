using System;
using System.Windows;

namespace SVGImage.SVG.Filters
{
    // SVG 1.1 seeded, four-channel Perlin noise and stitching rules:
    // https://www.w3.org/TR/SVG11/filters.html#feTurbulenceElement
    internal sealed class TurbulenceNoise
    {
        private readonly int[] lattice = new int[514];
        private readonly double[,,] gradient = new double[4, 514, 2];
        internal TurbulenceNoise(double seedValue)
        {
            long seed = (long)Math.Max(-2147483646, Math.Min(2147483646, seedValue));
            if (seed <= 0) seed = -(seed % 2147483646) + 1;
            for (int channel = 0; channel < 4; channel++)
                for (int i = 0; i < 256; i++)
                {
                    lattice[i] = i;
                    double x, y, length;
                    do
                    {
                        seed = Random(seed); x = (seed % 512 - 256) / 256.0;
                        seed = Random(seed); y = (seed % 512 - 256) / 256.0;
                        length = Math.Sqrt(x * x + y * y);
                    } while (length == 0);
                    gradient[channel, i, 0] = x / length; gradient[channel, i, 1] = y / length;
                }
            for (int i = 255; i > 0; i--)
            {
                seed = Random(seed); int j = (int)(seed % 256);
                int temp = lattice[i]; lattice[i] = lattice[j]; lattice[j] = temp;
            }
            for (int i = 0; i < 258; i++)
            {
                lattice[256 + i] = lattice[i];
                for (int c = 0; c < 4; c++) for (int axis = 0; axis < 2; axis++) gradient[c, 256 + i, axis] = gradient[c, i, axis];
            }
        }
        private static long Random(long seed)
        {
            long result = 16807 * (seed % 127773) - 2836 * (seed / 127773);
            return result <= 0 ? result + 2147483647 : result;
        }
        private static double Mix(double t, double a, double b) => a + t * (b - a);
        private double Dot(int c, int i, double x, double y) => gradient[c, i, 0] * x + gradient[c, i, 1] * y;
        private double Noise(int channel, double x, double y, int width, int height, int wrapX, int wrapY, bool stitch)
        {
            x += 4096; y += 4096;
            int bx = (int)Math.Floor(x), by = (int)Math.Floor(y), bx1 = bx + 1, by1 = by + 1;
            double rx = x - Math.Floor(x), ry = y - Math.Floor(y);
            if (stitch)
            {
                if (bx >= wrapX) bx -= width; if (bx1 >= wrapX) bx1 -= width;
                if (by >= wrapY) by -= height; if (by1 >= wrapY) by1 -= height;
            }
            int i = lattice[bx & 255], j = lattice[bx1 & 255];
            int b00 = lattice[i + (by & 255)], b10 = lattice[j + (by & 255)], b01 = lattice[i + (by1 & 255)], b11 = lattice[j + (by1 & 255)];
            double sx = rx * rx * (3 - 2 * rx), sy = ry * ry * (3 - 2 * ry);
            return Mix(sy, Mix(sx, Dot(channel, b00, rx, ry), Dot(channel, b10, rx - 1, ry)),
                Mix(sx, Dot(channel, b01, rx, ry - 1), Dot(channel, b11, rx - 1, ry - 1)));
        }
        private static double Frequency(double value, double size)
        {
            if (value == 0 || size <= 0) return value;
            double low = Math.Floor(size * value) / size, high = Math.Ceiling(size * value) / size;
            return value / low < high / value ? low : high;
        }
        internal double Sample(int channel, double x, double y, double fx, double fy, int octaves, bool fractal, bool stitch, Rect tile)
        {
            if (stitch) { fx = Frequency(fx, tile.Width); fy = Frequency(fy, tile.Height); }
            int width = (int)(tile.Width * fx + .5), height = (int)(tile.Height * fy + .5);
            int wrapX = (int)(tile.X * fx + 4096 + width), wrapY = (int)(tile.Y * fy + 4096 + height);
            x *= fx; y *= fy;
            double sum = 0, ratio = 1;
            for (int octave = 0; octave < Math.Min(9, octaves); octave++)
            {
                double value = Noise(channel, x, y, width, height, wrapX, wrapY, stitch);
                sum += (fractal ? value : Math.Abs(value)) / ratio;
                x *= 2; y *= 2; ratio *= 2; width *= 2; height *= 2; wrapX = 2 * wrapX - 4096; wrapY = 2 * wrapY - 4096;
            }
            return Math.Max(0, Math.Min(1, fractal ? (sum + 1) / 2 : sum));
        }
    }
}
