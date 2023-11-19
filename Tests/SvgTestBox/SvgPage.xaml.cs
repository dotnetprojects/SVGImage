using System;
using System.IO;
using System.IO.Compression;
using System.Xml;
using System.Text;
using System.Reflection;
using System.Diagnostics;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Markup;
using System.Windows.Media;

using Microsoft.Win32;
using IoPath = System.IO.Path;

using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Search;
using ICSharpCode.AvalonEdit.Indentation;

using SVGImage.SVG;
using SVGImage.SVG.FileLoaders;

namespace SvgTestBox
{
    /// <summary>
    /// Interaction logic for SvgPage.xaml
    /// </summary>
    public partial class SvgPage : Page
    {
        #region Private Fields

        private const string SvgFileName   = "TextBoxTestFile.svg";
        private const string BackFileName  = "TextBoxTestFile.bak";
        private const string XamlFileName  = "TextBoxTestFile.xaml";

        private const string AppTitle      = "Svg Test Box";
        private const string AppErrorTitle = "Svg Test Box - Error";

        private XamlPage _xamlPage;

        private FileSvgReader _fileReader;

        private DrawingGroup _currentDrawing;

        private string _currentFileName;

        private DirectoryInfo _directoryInfo;

        private string _svgFilePath;
        private string _xamlFilePath;
        private string _backFilePath;

        private FoldingManager _foldingManager;
        private XmlFoldingStrategy _foldingStrategy;

        private readonly SearchPanel _searchPanel;

        #endregion

        #region Constructors and Destructor

        public SvgPage()
        {
            InitializeComponent();

//            string workingDir = IoPath.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string workingDir = IoPath.GetFullPath("..\\");

            _svgFilePath  = IoPath.Combine(workingDir, SvgFileName);
            _xamlFilePath = IoPath.Combine(workingDir, XamlFileName);
            _backFilePath = IoPath.Combine(workingDir, BackFileName);

            _directoryInfo = new DirectoryInfo(workingDir);

            textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");
            TextEditorOptions options = textEditor.Options;
            if (options != null)
            {
                //options.AllowScrollBelowDocument = true;
                options.EnableHyperlinks = true;
                options.EnableEmailHyperlinks = true;
                options.EnableVirtualSpace = false;
                options.HighlightCurrentLine = true;
                //options.ShowSpaces               = true;
                //options.ShowTabs                 = true;
                //options.ShowEndOfLine            = true;              
            }

            textEditor.ShowLineNumbers = true;

            _foldingManager = FoldingManager.Install(textEditor.TextArea);
            _foldingStrategy = new XmlFoldingStrategy();

            _searchPanel = SearchPanel.Install(textEditor);

            textEditor.TextArea.IndentationStrategy = new DefaultIndentationStrategy();

            this.SetValue(TextOptions.TextFormattingModeProperty, TextFormattingMode.Display);

            this.Loaded += OnPageLoaded;
            this.SizeChanged += OnPageSizeChanged;
        }

        #endregion

        #region Public Properties

        public XamlPage XamlPage
        {
            get {
                return _xamlPage;
            }
            set {
                _xamlPage = value;

                _fileReader = new FileSvgReader(_xamlPage);
            }
        }

        #endregion

        #region Public Methods

        public bool InitializeDocument()
        {
            if (!string.IsNullOrWhiteSpace(_svgFilePath) && File.Exists(_svgFilePath))
            {
                return this.LoadDocument(_svgFilePath);
            }

            return true;
        }

        public bool LoadDocument(string documentFilePath)
        {
            try
            {
                this.UnloadDocument();

                if (textEditor == null || string.IsNullOrWhiteSpace(documentFilePath)
                    || File.Exists(documentFilePath) == false)
                {
                    return false;
                }
                string fileExt = IoPath.GetExtension(documentFilePath);
                if (string.IsNullOrWhiteSpace(fileExt))
                {
                    return false;
                }
                if (!string.Equals(fileExt, ".svg", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(fileExt, ".svgz", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (string.Equals(fileExt, ".svgz", StringComparison.OrdinalIgnoreCase))
                {
                    using (FileStream fileStream = File.OpenRead(documentFilePath))
                    {
                        using (GZipStream zipStream = new GZipStream(fileStream, CompressionMode.Decompress))
                        {
                            // Text Editor does not work with this stream, so we read the data to memory stream...
                            MemoryStream memoryStream = new MemoryStream();
                            // Use this method is used to read all bytes from a stream.
                            int totalCount = 0;
                            int bufferSize = 512;
                            byte[] buffer = new byte[bufferSize];
                            while (true)
                            {
                                int bytesRead = zipStream.Read(buffer, 0, bufferSize);
                                if (bytesRead == 0)
                                {
                                    break;
                                }

                                memoryStream.Write(buffer, 0, bytesRead);
                                totalCount += bytesRead;
                            }

                            if (totalCount > 0)
                            {
                                memoryStream.Position = 0;
                            }

                            textEditor.Load(memoryStream);

                            memoryStream.Close();
                        }
                    }
                }
                else
                {
                    textEditor.Load(documentFilePath);
                }

                this.UpdateFoldings();
            }
            catch (Exception ex)
            {
                this.ReportError(ex);

                return false;
            }

            if (string.Equals(documentFilePath, _svgFilePath, StringComparison.OrdinalIgnoreCase))
            {
                return this.ConvertDocument();
            }

            if (this.SaveDocument() && File.Exists(_svgFilePath))
            {
                return this.ConvertDocument(documentFilePath);
            }

            return true;
        }

        public void UnloadDocument(bool clearText = false)
        {
            if (clearText)
            {
                if (textEditor != null)
                {
                    textEditor.Document.Text = string.Empty;
                }
            }
            if (svgDrawing != null)
            {
                //svgDrawing.Source = null;
                svgDrawing.SetImage((Drawing)null);
            }

            if (_xamlPage != null)
            {
                _xamlPage.UnloadDocument();
            }
        }

        #endregion

        #region Private Methods

        private void UpdateFoldings()
        {
            if (_foldingManager == null || _foldingStrategy == null)
            {
                _foldingManager = FoldingManager.Install(textEditor.TextArea);
                _foldingStrategy = new XmlFoldingStrategy();
            }
            _foldingStrategy.UpdateFoldings(_foldingManager, textEditor.Document);
        }

        private bool SaveDocument()
        {
            try
            {
                string inputText = textEditor.Document.Text;
                if (string.IsNullOrWhiteSpace(inputText))
                {
                    return false;
                }

                textEditor.Save(_backFilePath);

                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Parse,
                    XmlResolver = null
                };

                using (var textReader = new StreamReader(_backFilePath))
                {
                    using (var reader = XmlReader.Create(textReader, settings))
                    {
                        var document = new XmlDocument();
                        document.Load(reader);
                    }
                }

                File.Move(_backFilePath, _svgFilePath);

                return true;
            }
            catch (Exception ex)
            {
                this.ReportError(ex);

                return false;
            }
        }

        private bool ConvertDocument(string filePath = null)
        {
            if (string.IsNullOrWhiteSpace(filePath) || File.Exists(filePath) == false)
            {
                filePath = _svgFilePath;
            }
            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || File.Exists(filePath) == false)
                {
                    return false;
                }

                if (_fileReader == null)
                {
                    _fileReader = new FileSvgReader(_xamlPage);
                }

                DrawingGroup drawing = _fileReader.Read(filePath, _directoryInfo);
                if (drawing == null)
                {
                    return false;
                }

                if (_xamlPage != null && !string.IsNullOrWhiteSpace(_xamlFilePath))
                {
                    if (File.Exists(_xamlFilePath))
                    {
                        _xamlPage.LoadDocument(_xamlFilePath);

                        // Delete the file after loading it...
                        File.Delete(_xamlFilePath);
                    }
                    else
                    {
                        string xamlFilePath = IoPath.Combine(_directoryInfo.FullName, 
                            IoPath.GetFileNameWithoutExtension(filePath) + ".xaml");
                        if (File.Exists(xamlFilePath))
                        {
                            _xamlPage.LoadDocument(xamlFilePath);

                            // Delete the file after loading it...
                            File.Delete(xamlFilePath);
                        }
                    }
                }
                _currentDrawing = drawing;

                zoomBorder.Reset();

                svgDrawing.SetImage((Drawing)null);

                svgDrawing.SetImage(drawing);

                return true;
            }
            catch (Exception ex)
            {
                //svgDrawing.Source = null;
                svgDrawing.SetImage((Drawing)null);

                this.ReportError(ex);

                return false;
            }
        }

        private void ReportInfo(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            Trace.TraceInformation(message);

            MessageBox.Show(message, AppErrorTitle, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ReportError(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            Trace.TraceError(message);

            MessageBox.Show(message, AppErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ReportError(Exception ex)
        {
            if (ex == null)
            {
                return;
            }

            Trace.TraceError(ex.ToString());

            MessageBox.Show(ex.ToString(), AppErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion

        #region Private Event Handlers

        private void OnOpenFileClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.CheckFileExists = true;
            dlg.Title = "Select An SVG File";
            dlg.DefaultExt = "*.svg";
            dlg.Filter = "All SVG Files (*.svg,*.svgz)|*.svg;*.svgz"
                                + "|Svg Uncompressed Files (*.svg)|*.svg"
                                + "|SVG Compressed Files (*.svgz)|*.svgz";
            if (dlg.ShowDialog() ?? false)
            {
                _currentFileName = dlg.FileName;

                if (!string.IsNullOrWhiteSpace(_svgFilePath))
                {
                    if (File.Exists(_svgFilePath))
                    {
                        File.Delete(_svgFilePath);
                    }
                }
                if (!string.IsNullOrWhiteSpace(_backFilePath))
                {
                    if (File.Exists(_backFilePath))
                    {
                        File.Delete(_backFilePath);
                    }
                }

                this.LoadDocument(_currentFileName);
            }
        }

        private void OnSaveFileClick(object sender, EventArgs e)
        {
            if (_currentFileName == null)
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Title      = "Save As";
                dlg.Filter     = "SVG Files|*.svg;*.svgz";
                dlg.DefaultExt = ".svg";
                if (dlg.ShowDialog() ?? false)
                {
                    _currentFileName = dlg.FileName;
                }
                else
                {
                    return;
                }
            }

            string fileExt = Path.GetExtension(_currentFileName);
            if (string.Equals(fileExt, ".svg", StringComparison.OrdinalIgnoreCase))
            {
                textEditor.Save(_currentFileName);
            }
            else if (string.Equals(fileExt, ".svgz", StringComparison.OrdinalIgnoreCase))
            {
                using (FileStream svgzDestFile = File.Create(_currentFileName))
                {
                    using (GZipStream zipStream = new GZipStream(svgzDestFile, 
                        CompressionMode.Compress, true))
                    {
                        textEditor.Save(zipStream);
                    }
                }
            }               
        }

        //private void OnPasteText(object sender, DataObjectPastingEventArgs args)
        //{
        //    string clipboard = args.DataObject.GetData(typeof(string)) as string;
        //}

        private void OnFormatInputClick(object sender, RoutedEventArgs e)
        {
            if (textEditor == null)
            {
                return;
            }
            string inputText = textEditor.Document.Text;
            if (string.IsNullOrWhiteSpace(inputText))
            {
                return;
            }

            MemoryStream mStream = new MemoryStream();
            XmlTextWriter writer = new XmlTextWriter(mStream, Encoding.UTF8);
            XmlDocument document = new XmlDocument();
            document.XmlResolver = null;

            try
            {
                // Load the XmlDocument with the XML.
                document.LoadXml(inputText);

                writer.Formatting = Formatting.Indented;

                // Write the XML into a formatting XmlTextWriter
                document.WriteContentTo(writer);
                writer.Flush();
                mStream.Flush();

                // Have to rewind the MemoryStream in order to read
                // its contents.
                mStream.Position = 0;

                // Read MemoryStream contents into a StreamReader.
                StreamReader sReader = new StreamReader(mStream);

                // Extract the text from the StreamReader.
                textEditor.Document.Text = sReader.ReadToEnd();
            }
            catch (XmlException)
            {
                // Handle the exception
            }

            mStream.Close();
            writer.Close();
        }

        private void OnSearchTextClick(object sender, RoutedEventArgs e)
        {
            if (_searchPanel == null)
            {
                return;
            }

            string searchText = searchTextBox.Text;

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                _searchPanel.SearchPattern = searchText;
            }

            _searchPanel.Open();
            _searchPanel.Reactivate();
        }

        private void OnSearchTextBoxKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            // your event handler here
            e.Handled = true;

            this.OnSearchTextClick(sender, e);
        }

        private void OnHighlightingSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            RowDefinition rowTab = rightGrid.RowDefinitions[2];
            rowTab.Height = new GridLength((this.ActualHeight - 8) / 2, GridUnitType.Pixel);

            DispatcherTimer foldingUpdateTimer = new DispatcherTimer();
            foldingUpdateTimer.Interval = TimeSpan.FromSeconds(2);
            foldingUpdateTimer.Tick += delegate { UpdateFoldings(); };
            foldingUpdateTimer.Start();

            textEditor.Focus();
        }

        private void OnPageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            RowDefinition rowTab = rightGrid.RowDefinitions[2];
            rowTab.Height = new GridLength((this.ActualHeight - 8) / 2, GridUnitType.Pixel);
        }

        private void OnConvertInputClick(object sender, RoutedEventArgs e)
        {
            this.UnloadDocument();

            try
            {
                if (!string.IsNullOrWhiteSpace(_svgFilePath))
                {
                    if (File.Exists(_svgFilePath))
                    {
                        File.Delete(_svgFilePath);
                    }
                }
                if (!string.IsNullOrWhiteSpace(_backFilePath))
                {
                    if (File.Exists(_backFilePath))
                    {
                        File.Delete(_backFilePath);
                    }
                }

                if (this.SaveDocument() && File.Exists(_svgFilePath))
                {
                    this.ConvertDocument();
                }
            }
            catch (Exception ex)
            {
                this.ReportError(ex);
            }
        }

        #endregion

        #region Drag/Drop Methods

        private void OnDragEnter(object sender, DragEventArgs de)
        {
            if (de.Data.GetDataPresent(DataFormats.Text) ||
               de.Data.GetDataPresent(DataFormats.FileDrop))
            {
                de.Effects = DragDropEffects.Copy;
            }
            else
            {
                de.Effects = DragDropEffects.None;
            }
        }

        private void OnDragLeave(object sender, DragEventArgs e)
        {

        }

        private void OnDragDrop(object sender, DragEventArgs de)
        {
            string fileName = "";
            if (de.Data.GetDataPresent(DataFormats.Text))
            {
                fileName = (string)de.Data.GetData(DataFormats.Text);
            }
            else if (de.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] fileNames;
                fileNames = (string[])de.Data.GetData(DataFormats.FileDrop);
                fileName = fileNames[0];
            }

            if (!string.IsNullOrEmpty(fileName))
            {
            }
            if (string.IsNullOrEmpty(fileName) || !File.Exists(fileName))
            {
                return;
            }
            string fileExt = IoPath.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(fileExt))
            {
                return;
            }
            if (!string.Equals(fileExt, ".svg", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(fileExt, ".svgz", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                this.Cursor = Cursors.Wait;
                this.ForceCursor = true;

                if (!string.IsNullOrWhiteSpace(_svgFilePath))
                {
                    if (File.Exists(_svgFilePath))
                    {
                        File.Delete(_svgFilePath);
                    }
                }
                if (!string.IsNullOrWhiteSpace(_backFilePath))
                {
                    if (File.Exists(_backFilePath))
                    {
                        File.Delete(_backFilePath);
                    }
                }

                this.LoadDocument(fileName);
            }
            catch (Exception ex)
            {
                this.ReportError(ex);
            }
            finally
            {
                this.Cursor = Cursors.Arrow;
                this.ForceCursor = false;
            }
        }

        #endregion

        #region FileSvgReader Class

        private sealed class FileSvgReader : IDisposable
        {
            public const string GZipSignature = "H4sI";

            private Color? _overrideFill;
            private Color? _overrideStroke;
            private bool _isDisposed;

            private XamlPage _xamlPage;

            public FileSvgReader(XamlPage xamlPage)
            {
                _xamlPage = xamlPage;
            }

            public FileSvgReader(Color? overrideFill, Color? overrideStroke, XamlPage xamlPage)
                : this(xamlPage)
            {
                _overrideFill = overrideFill;
                _overrideStroke = overrideStroke;
            }

            ~FileSvgReader()
            {
                Dispose(false);
            }

            public bool IsDisposed
            {
                get {
                    return _isDisposed;
                }
            }

            public Color? OverrideFill
            {
                get {
                    return _overrideFill;
                }
                set {
                    _overrideFill = value;
                }
            }

            public Color? OverrideStroke
            {
                get
                {
                    return _overrideStroke;
                }
                set
                {
                    _overrideStroke = value;
                }
            }

            public DrawingGroup Read(string filePath, DirectoryInfo workingDir)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return new DrawingGroup();
                }

                var svgRender = new SVGRender(new FileSystemLoader());
                svgRender.OverrideFill = _overrideFill;
                svgRender.OverrideStroke = _overrideStroke;
                svgRender.UseAnimations = true;

                var drawingGroup = svgRender.LoadDrawing(filePath);

                string xamlFilePath = IoPath.Combine(workingDir.FullName,
                    IoPath.GetFileNameWithoutExtension(filePath) + ".xaml");

                //svgDrawing.RenderDiagrams(drawing);
                if (drawingGroup != null)
                {
                    if (_xamlPage != null)
                    {
                        //using (MemoryStream stream = new MemoryStream())
                        using (FileStream stream = File.Create(xamlFilePath))
                        {
                            XmlWriterSettings writerSettings = new XmlWriterSettings();
                            writerSettings.Indent = true;
                            writerSettings.OmitXmlDeclaration = true;
                            writerSettings.Encoding = Encoding.UTF8;

                            using (XmlWriter writer = XmlWriter.Create(stream, writerSettings))
                            {
                                XamlWriter.Save(drawingGroup, writer);
                            }

                            if (stream.Length > 0)
                            {
                                stream.Position = 0;
                            }

                            _xamlPage.LoadDocument(stream);
                        }
                    }
                }

                return drawingGroup;
            }

            public DrawingGroup Read(Uri fileUri)
            {
                if (fileUri == null)
                {
                    return new DrawingGroup();
                }

                var svgRender = new SVGRender(new FileSystemLoader());
                svgRender.OverrideFill = _overrideFill;
                svgRender.OverrideStroke = _overrideStroke;
                svgRender.UseAnimations = true;

                return svgRender.LoadDrawing(fileUri);
            }

            public DrawingGroup Read(Stream stream)
            {
                if (stream == null)
                {
                    return new DrawingGroup();
                }

                var svgRender = new SVGRender(new FileSystemLoader());
                svgRender.OverrideFill = _overrideFill;
                svgRender.OverrideStroke = _overrideStroke;
                svgRender.UseAnimations = true;

                return svgRender.LoadDrawing(stream);
            }

            public static string RemoveWhitespace(string str)
            {
                if (str == null || str.Length == 0)
                {
                    return string.Empty;
                }
                var len = str.Length;
                var src = str.ToCharArray();
                int dstIdx = 0;

                for (int i = 0; i < len; i++)
                {
                    var ch = src[i];

                    switch (ch)
                    {
                        case '\u0020':
                        case '\u00A0':
                        case '\u1680':
                        case '\u2000':
                        case '\u2001':
                        case '\u2002':
                        case '\u2003':
                        case '\u2004':
                        case '\u2005':
                        case '\u2006':
                        case '\u2007':
                        case '\u2008':
                        case '\u2009':
                        case '\u200A':
                        case '\u202F':
                        case '\u205F':
                        case '\u3000':
                        case '\u2028':
                        case '\u2029':
                        case '\u0009':
                        case '\u000A':
                        case '\u000B':
                        case '\u000C':
                        case '\u000D':
                        case '\u0085':
                            continue;
                        default:
                            src[dstIdx++] = ch;
                            break;
                    }
                }
                return new string(src, 0, dstIdx);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                _isDisposed = true;
            }
        }

        #endregion
    }
}
