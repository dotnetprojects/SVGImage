using System;
using System.Net;
using System.IO;
using System.Reflection;
using System.IO.Compression;
using System.Windows;

namespace IconConverterSample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            InitializeProtocol(); //For the web access

            // ICons credit: https://github.com/icons8/flat-color-icons
            string workingDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string iconsPath = Path.Combine(workingDir, PageMultiple.IconZipFile);
            if (!File.Exists(iconsPath))
            {
                return;
            }

            var iconsDir = new DirectoryInfo(Path.Combine(workingDir, PageMultiple.IconFolder));
            if (!iconsDir.Exists)
            {
                iconsDir.Create();

                ZipFile.ExtractToDirectory(iconsPath, workingDir);
            }
        }


        public void InitializeProtocol()
        {
#if DOTNET40
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                    | SecurityProtocolTypes.Tls11 | SecurityProtocolTypes.Tls12;
            }
            catch
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls;
                return;
            }
#else
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
#endif
        }

#if DOTNET40
        private static class SecurityProtocolTypes
        {
            public const SecurityProtocolType Tls12 = (SecurityProtocolType)0x00000C00;
            public const SecurityProtocolType Tls11 = (SecurityProtocolType)0x00000300;
            public const SecurityProtocolType SystemDefault = (SecurityProtocolType)0;
        }
#endif

    }

}
