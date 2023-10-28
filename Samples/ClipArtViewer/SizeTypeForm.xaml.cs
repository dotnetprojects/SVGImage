using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ClipArtViewer
{
    /// <summary>
    /// Interaction logic for SizeTypeForm.xaml
    /// </summary>
    public partial class SizeTypeForm : UserControl
    {
        public static DependencyProperty ImageSourcePoperty = DependencyProperty.Register("ImageSource",
            typeof(Drawing),
            typeof(SizeTypeForm),
            new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnImageSourceChanged)));

        static void OnImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SizeTypeForm)d).SetImage(e.NewValue as Drawing);
        }

        public Drawing ImageSource
        {
            get { return (Drawing)GetValue(ImageSourcePoperty); }
            set { SetValue(ImageSourcePoperty, value); }
        }

        public void SetImage(Drawing drawing)
        {
            m_canvas1.SetImage(drawing);
            m_canvas2.SetImage(drawing);
            m_canvas3.SetImage(drawing);
            m_canvas4.SetImage(drawing);
            m_canvas5.SetImage(drawing);

            m_canvas6.SetImage(drawing);
            m_canvas7.SetImage(drawing);
        }

        public SizeTypeForm()
        {
            InitializeComponent();
        }
    }
}
