using System.Windows;
using System.Reflection;

namespace SVGImage.SVG.Utils
{

    public static class DpiUtil
    {
        static DpiUtil()
        {
            try
            {
                var sysPara = typeof(SystemParameters);
                var dpiXProperty = sysPara.GetProperty("DpiX", BindingFlags.NonPublic | BindingFlags.Static);
                var dpiYProperty = sysPara.GetProperty("Dpi", BindingFlags.NonPublic | BindingFlags.Static);
                DpiX = (int)dpiXProperty.GetValue(null, null);
                DpiX = (int)dpiYProperty.GetValue(null, null);
            }
            catch
            {
                DpiX = 96;
                DpiY = 96;
            }
#if DPI_AWARE
            DpiScale = new DpiScale(DpiX / 96.0, DpiY / 96.0);
#endif
        }

        public static int DpiX { get; private set; }
        public static int DpiY { get; private set; }
#if DPI_AWARE
        public static DpiScale DpiScale { get; private set; }
#endif
        public static double PixelsPerDip => GetPixelsPerDip();

        public static double GetPixelsPerDip()
        {
            return DpiY / 96.0;
        }




    }




}
