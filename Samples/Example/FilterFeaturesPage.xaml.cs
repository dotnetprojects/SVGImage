using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Example
{
    public partial class FilterFeaturesPage : UserControl
    {
        private sealed class Feature
        {
            public string Title { get; set; }
            internal string File, Description;
        }
        private bool started;
        public FilterFeaturesPage()
        {
            InitializeComponent();
            Features.ItemsSource = new[] {
                new Feature { Title="Lighting", File="Lighting", Description="Diffuse shading with distant, point and spot sources, plus a specular highlight. Input alpha supplies the surface height; blurred edges form the bevels." },
                new Feature { Title="Image inputs", File="Images", Description="feImage reads a local SVG element, a relative embedded SVG resource and a data URI. External resources use the host's existing image loader." },
                new Feature { Title="Backdrop glass", File="Backdrop", Description="enable-background:new starts a background buffer. The glass panel blurs and desaturates preceding artwork through BackgroundImage." }
            };
        }
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!started) { started=true; Features.SelectedIndex=0; }
            else LoadSelected();
        }
        private void SelectFeature(object sender, SelectionChangedEventArgs e) { if (IsLoaded) LoadSelected(); }
        private void LoadSelected()
        {
            var feature=Features.SelectedItem as Feature;
            if (feature==null) return;
            Preview.SeekAnimations(TimeSpan.Zero);
            var uri=new Uri("pack://application:,,,/Example;component/Images/FilterFeatures/"+feature.File+".svg");
            Description.Text=feature.Description;
            using(var stream=Application.GetResourceStream(uri).Stream)
            using(var reader=new StreamReader(stream)) SourceCode.Text=reader.ReadToEnd();
            Preview.SetImage(uri);
        }
        private void Restart(object sender, RoutedEventArgs e) { LoadSelected(); }
    }
}
