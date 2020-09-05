using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Documents;

using SVGImage.SVG;

namespace ClipArtViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string SamplesDir = @"..\..\..\..\Tests\Images";

        DependencyProperty SvgPathProperty = DependencyProperty.Register("SvgPath", typeof(string), typeof(MainWindow));
        public string SvgPath
        {
            get { return GetValue(SvgPathProperty) as string; }
            set { SetValue(SvgPathProperty, value); }
        }

        static DependencyProperty FilenameProperty = DependencyProperty.RegisterAttached("Filename", 
            typeof(string), typeof(MainWindow));
        public static void SetFilename(DependencyObject obj, string filename)
        {
            obj.SetValue(FilenameProperty, filename);
        }
        public static string GetFilename(DependencyObject obj)
        {
            return obj.GetValue(FilenameProperty) as string;
        }

        static DependencyProperty SVGImageProperty = DependencyProperty.RegisterAttached("SVGImage", 
            typeof(DrawingGroup), typeof(MainWindow));
        public static void SetSVGImage(DependencyObject obj, DrawingGroup svgimage)
        {
            obj.SetValue(SVGImageProperty, svgimage);
        }
        public static DrawingGroup GetSVGImage(DependencyObject obj)
        {
            return obj.GetValue(SVGImageProperty) as DrawingGroup;
        }
        
        List<SVGItem> m_items = new List<SVGItem>();

        public List<SVGItem> Items
        {
            get { return m_items; }
        }

        private bool _isShown;

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

            // default path
            string appPath = Process.GetCurrentProcess().MainModule.FileName;
            string appDir  = Path.GetDirectoryName(appPath);
            string path    = Path.GetFullPath(Path.Combine(appDir, SamplesDir));
            if (Directory.Exists(path) == false)
            {
                return;
            }
            this.SvgPath = path;

            ListFiles();
            DataContext = this;

            tabPages.SelectedIndex = 0;
        }

        private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {
            txtLogger.Document.Blocks.Clear();
            txtLogger.SetValue(Block.LineHeightProperty, 1.0);
            Trace.Listeners.Add(new TextBoxTraceListener(txtLogger));
        }

        void ListFiles()
        {
            string path = this.SvgPath;
            if (Directory.Exists(path) == false)
            {
                return;
            }
            string[] files = Directory.GetFiles(path, "*.svg");
            foreach (string file in files)
                m_items.Add(new SVGItem(file));

            //m_filelist.GetBindingExpression(ListBox.ItemsSourceProperty).UpdateTarget();
            m_filelist.Items.Refresh();
        }

        private void OnReloadItem(object sender, RoutedEventArgs e)
        {
            SVGItem item = m_filelist.SelectedItem as SVGItem;
            if (item != null)
                item.Reload();
        }

        private void OnReloadAll(object sender, RoutedEventArgs e)
        {
            m_items.Clear();
            ListFiles();
        }

        private void OnTabControlSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void OnBrowseButtonClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Not implemented yet!", "ClipArtViewer", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public class SVGItem
    {
        private bool _isLogged;
        DrawingGroup _drawing = null;
        public SVGItem(string fullpath)
        {
            FullPath = fullpath;
            Filename = Path.GetFileNameWithoutExtension(fullpath);
        }

        public string FullPath { get; private set; }
        public string Filename { get; private set; }
        public SVGRender SVGRender { get; private set; }
        public DrawingGroup SVGImage
        {
            get {
                EnsureLoaded();
                return _drawing;
            }
        }

        public void Reload()
        {
            _drawing = SVGRender.LoadDrawing(FullPath);
        }

        void EnsureLoaded()
        {
            try
            {
                if (_drawing != null)
                    return;
                SVGRender = new SVGRender();
                _drawing = SVGRender.LoadDrawing(FullPath);
            }
            catch (Exception ex)
            {
                if (_isLogged)
                {
                    return;
                }
                _isLogged = true;
                Trace.WriteLine("Exception Loading: " + this.FullPath);
                Trace.WriteLine(ex.ToString());
                Trace.WriteLine(string.Empty);
            }
        }
    }

    public class TextBoxTraceListener : TraceListener
    {
        private RichTextBox _textBox;

        public TextBoxTraceListener(RichTextBox textBox)
        {
            _textBox = textBox;
        }

        public override void Write(string message)
        {
            if (_textBox.Dispatcher.CheckAccess())
            {
                AppendText(message);
            }
            else
            {
                _textBox.Dispatcher.Invoke(new AppendTextDelegate(AppendText), message);
            }
        }

        public override void WriteLine(string message)
        {
            if (_textBox.Dispatcher.CheckAccess())
            {
                AppendText(message);
            }
            else
            {
                _textBox.Dispatcher.Invoke(new AppendTextDelegate(AppendText), message);
            }
        }

        private delegate void AppendTextDelegate(string message);

        private void AppendText(string message)
        {
            _textBox.AppendText(message + Environment.NewLine);
        }
    }
}
