using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Windows;
using System.Threading;
using SVGImage.SVG;

namespace Example
{
    public class IconData
    {
        private Uri _uri;
        private string _title;

        public IconData()
        {
        }

        public IconData(FileInfo source)
        {
            if (source != null)
            {
                _title = Path.GetFileNameWithoutExtension(source.Name);
                _uri   = new Uri(source.FullName);
            }
        }

        public string ImageTitle
        {
            get { return _title; }
            set { _title = value; }
        }

        public Uri ImageUri
        {
            get { return _uri; }
            set { _uri = value; }
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string SamplesDir  = @"..\..\..\..\..\Tests";
        private const string IconZipFile = "svg-icons.zip";
        private const string IconFolder  = "Svg-Icons";

        private bool _isShown;
        private bool _loadingDetailedIllustration;
        private bool _showMessage;
        private string _iconsPath;

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += OnMainWindowLoaded;
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            if (_isShown)
            {
                return;
            }
            _isShown = true;

            if (_showMessage && !string.IsNullOrWhiteSpace(_iconsPath))
            {
                _showMessage = false;
                MessageBox.Show("For the .NET 4.0 build, the grid of icons may not be shown at bottom panel of the SVG Icons." +
                    "\nIn this case, manually extract the following zip file and restart this sample application." +
                    "\n\n" + _iconsPath, "SVG Icons Tab", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {
            LoadDetailedIllustration();
            this.EnsureIcons();

            string workingDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SamplesDir));

            string iconsPath = Path.Combine(workingDir, IconZipFile);
            if (!File.Exists(iconsPath))
            {
                return;
            }

            var iconsDir = new DirectoryInfo(Path.Combine(workingDir, IconFolder));
            if (!iconsDir.Exists)
            {
                return;
            }

            FileInfo[] iconFiles = iconsDir.GetFiles("*.svg", SearchOption.TopDirectoryOnly);
            if (iconFiles == null || iconFiles.Length == 0)
            {
                return;
            }

            List<IconData> sourceData = new List<IconData>(iconFiles.Length);
            foreach (var iconFile in iconFiles)
            {
                sourceData.Add(new IconData(iconFile));
            }
//            sourceData.Add(new IconData()); //Test-Empty

            this.DataContext = sourceData;
        }

        private void LoadDetailedIllustration()
        {
            if (_loadingDetailedIllustration) return;
            _loadingDetailedIllustration = true;
            // The car contains many raster blur layers. Render this static SVG
            // on its own STA so the window can open and respond immediately.
            var resource = Application.GetResourceStream(new Uri("/Example;component/Images/171.svg", UriKind.Relative));
            var data = new MemoryStream();
            using (var source = resource.Stream) source.CopyTo(data);
            data.Position = 0;
            var worker = new Thread(() =>
            {
                try
                {
                    System.Windows.Media.DrawingGroup drawing;
                    using (data) drawing = new SVGRender { UseAnimations = false }.LoadDrawing(data);
                    drawing.Freeze();
                    UpdateIllustration(new Action(() =>
                    {
                        if (!IsLoaded) return;
                        DetailedIllustration.SetImage(drawing);
                        DetailedIllustrationStatus.Visibility = System.Windows.Visibility.Collapsed;
                    }));
                }
                catch (Exception ex)
                {
                    UpdateIllustration(new Action(() =>
                    {
                        if (IsLoaded) DetailedIllustrationStatus.Text = "Could not load illustration: " + ex.Message;
                    }));
                }
            }) { IsBackground = true };
            worker.SetApartmentState(ApartmentState.STA);
            worker.Start();
        }

        private void UpdateIllustration(Action update)
        {
            if (Dispatcher.HasShutdownStarted) return;
            try { Dispatcher.BeginInvoke(update); }
            catch (InvalidOperationException) { if (!Dispatcher.HasShutdownStarted) throw; }
        }

        private void EnsureIcons()
        {
            // Icons credit: https://github.com/icons8/flat-color-icons
            string workingDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SamplesDir));

            string iconsPath = Path.Combine(workingDir, IconZipFile);
            if (!File.Exists(iconsPath))
            {
                return;
            }
            var iconsDir = new DirectoryInfo(Path.Combine(workingDir, IconFolder));

            _iconsPath = iconsPath;

#if !DOTNET40 
            if (!iconsDir.Exists)
            {
                iconsDir.Create();

                ZipFile.ExtractToDirectory(iconsPath, workingDir);
            }
#else
            if (!iconsDir.Exists)
            {
                _showMessage = true;
            }
#endif
        }

        private void SVGImage_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var rnd = new Random().Next(0, 3);

            OverrideColorTest.OverrideColor =
                rnd == 0 ? System.Windows.Media.Colors.White :
                rnd == 1 ? System.Windows.Media.Colors.Magenta :
                System.Windows.Media.Colors.Black;
        }

        private void SVGImage_MouseDoubleClickSeparateOverride(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            System.Windows.Media.Color[] colors =
            {
                System.Windows.Media.Colors.White,
                System.Windows.Media.Colors.Magenta,
                System.Windows.Media.Colors.DarkGreen,
                System.Windows.Media.Colors.DarkSalmon,
                System.Windows.Media.Colors.DarkBlue,
                System.Windows.Media.Colors.Black,
            };
            var ran = new Random();
            var rndFill = ran.Next(0, colors.Length);
            OverrideSeparateColorTest.OverrideFillColor = colors[rndFill];

            var rndStroke = ran.Next(0, colors.Length);
            OverrideSeparateColorTest.OverrideStrokeColor = colors[rndStroke];
        }
    }
}
