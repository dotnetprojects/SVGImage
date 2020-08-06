using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CustomBrushesExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContextChanged += OnDataContextChanged;
            UpdateDataContext();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateDataContext();
        }

        private void UpdateDataContext()
        {
            if (DataContext is MainWindowViewModel viewModel)
            {

                if (viewModel.MyBrushes == null)
                {
                    viewModel.MyBrushes = new Dictionary<string, Brush>();
                }

                var myCustomBrushes = TryFindResource("MyBrushes") as Dictionary<string, Brush>;
                if (myCustomBrushes != null)
                {
                    var newBrushes = new Dictionary<string, Brush>(viewModel.MyBrushes);
                    foreach (var brush in myCustomBrushes)
                    {
                        newBrushes[brush.Key] = brush.Value;
                    }
                    viewModel.MyBrushes = newBrushes;
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.ChangeBrush();
            }
        }
    }
}
