using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IconConverterSample
{
    /// <summary>
    /// Interaction logic for PageOther.xaml
    /// </summary>
    public partial class PageOther : Page
    {
        private const string SvgFileName = @"..\Raster_80shades.svg";

        public static readonly DependencyProperty LocalFileNameProperty =
            DependencyProperty.Register("LocalFileName", typeof(string),
            typeof(PageOther), new PropertyMetadata(SvgFileName));

        public PageOther()
        {
            InitializeComponent();
        }

        public string LocalFileName
        {
            get { return (string)GetValue(LocalFileNameProperty); }
            set { SetValue(LocalFileNameProperty, value); }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".svg"; // Default file extension
            dlg.Filter     = "SVG File (.svg)|*.svg"; // Filter files by extension

            // Show open file dialog box
            var dlgRResult = dlg.ShowDialog();

            // Process open file dialog box results
            if (dlgRResult == true)
            {
                // Open document
                this.LocalFileName = dlg.FileName;
            }
        }
    }
}
