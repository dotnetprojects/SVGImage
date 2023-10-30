using System;

using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

namespace ClipArtViewer
{
    /// <summary>
    /// Interaction logic for FullSizeForm.xaml
    /// </summary>
    public partial class FullSizeForm : UserControl
    {
        public static DependencyProperty ImageSourcePoperty = DependencyProperty.Register("ImageSource",
            typeof(Drawing),
            typeof(FullSizeForm),
            new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnImageSourceChanged)));
        
        static void OnImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FullSizeForm)d).SetImage(e.NewValue as Drawing);
        }

        public Drawing ImageSource
        {
            get { return (Drawing)GetValue(ImageSourcePoperty); }
            set { SetValue(ImageSourcePoperty, value); }
        }

        public void SetImage(Drawing drawing)
        {
            m_canvas1.SetImage(drawing);
        }

        public FullSizeForm()
        {
            InitializeComponent();

            this.Loaded += OnFullSizeFormLoaded;
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            if (m_canvas1 != null && m_sizeTypeCombo != null)
            {
                m_canvas1.SizeType = SVGImage.SVG.SVGImage.eSizeType.ContentToSizeNoStretch;
                m_sizeTypeCombo.SelectedItem = m_canvas1.SizeType.ToString();
            }
        }

        private void OnFullSizeFormLoaded(object sender, RoutedEventArgs e)
        {
            m_canvas1.SetImage(this.ImageSource);
            m_sizeTypeCombo.SelectedItem = m_canvas1.SizeType.ToString();
        }
    }
}
