using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Example
{
    public partial class AnimationFeaturesPage : UserControl
    {
        private sealed class Feature
        {
            public string Title { get; set; }
            internal string File, Description;
        }
        private bool started;
        public AnimationFeaturesPage()
        {
            InitializeComponent();
            Features.ItemsSource = new[] {
                new Feature { Title="Path morphing", File="PathMorph", Description="A closed path morphs between two silhouettes. Matching cubic segments interpolate their control points; the original path remains the underlying value." },
                new Feature { Title="Syncbase timing", File="Syncbase", Description="The first circle starts a repeating sequence. The second waits for first.end + 0.15s; the third waits for second.end + 0.15s. Animation IDs can be referenced before their definitions." },
                new Feature { Title="Animated masks", File="AnimatedMasks", Description="Top: a moving alpha-mask spotlight. Bottom: a colored luminance mask moving over white. Luminance content is composited before conversion to alpha, preserving occlusion." },
                new Feature { Title="Matrix decomposition", File="MatrixDecomposition", Description="Two matrix() values animate through translation, rotation, signed scale and residual skew. The arrow rotates smoothly instead of collapsing under coefficient interpolation." },
                new Feature { Title="Percentage translation", File="PercentageTranslations", Description="Both tiles translate by 300% with transform-box:fill-box. The tile twice as wide travels twice as far. Without fill-box, percentages use the SVG viewport." }
            };
        }
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!started) { started=true; Features.SelectedIndex=0; }
            else LoadSelected();
        }
        private void OnUnloaded(object sender, RoutedEventArgs e) { Preview.SeekAnimations(TimeSpan.Zero); }
        private void SelectFeature(object sender, SelectionChangedEventArgs e) { if (IsLoaded) LoadSelected(); }
        private void LoadSelected()
        {
            var feature=Features.SelectedItem as Feature;
            if (feature==null) return;
            Preview.SeekAnimations(TimeSpan.Zero);
            var uri=new Uri("pack://application:,,,/Example;component/Images/AnimationFeatures/"+feature.File+".svg");
            Description.Text=feature.Description;
            using(var stream=Application.GetResourceStream(uri).Stream)
            using(var reader=new StreamReader(stream)) SourceCode.Text=reader.ReadToEnd();
            Preview.SetImage(uri);
        }
        private void Restart(object sender, RoutedEventArgs e) { LoadSelected(); }
        private void Snapshot(object sender, RoutedEventArgs e) { Preview.SeekAnimations(TimeSpan.FromSeconds(2)); }
    }
}
