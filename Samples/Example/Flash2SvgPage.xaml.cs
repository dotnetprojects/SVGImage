using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace Example
{
    public partial class Flash2SvgPage : UserControl
    {
        private const string ExamplesUrl = "https://github.com/TomByrne/Flash2Svg/wiki/Examples";
        private const string DownloadUrl = "https://raw.githubusercontent.com/wiki/TomByrne/Flash2Svg/img/";
        internal static readonly string[] Names = {
            "BallBounce", "EyeOpening", "ShapeTween", "CrazyGirl", "BirdFlapClosed", "PeteyHead", "MattHead",
            "Viber1701", "Viber1705", "Viber3923", "Viber5200", "Viber5235", "LoveDota"
        };
        private readonly string cache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SVGImage", "Flash2Svg", "Examples");
        private string selectedFile;
        private string[] eventTargets = new string[0];
        private int generation;
        private bool started;

        public Flash2SvgPage()
        {
            InitializeComponent();
            Examples.ItemsSource = Names;
        }
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!started) { started = true; Examples.SelectedIndex = 0; }
            else if (selectedFile != null) LoadPreview(selectedFile, generation);
            else SelectExample(null, null);
        }
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            ++generation;
            // Stop this page's clocks while another sample tab is visible.
            Preview.SeekAnimations(TimeSpan.Zero);
        }
        private void SelectExample(object sender, SelectionChangedEventArgs e)
        {
            string name = Examples.SelectedItem as string;
            if (name == null) return;
            int request = ++generation;
            selectedFile = null;
            PlayButton.IsEnabled = false;
            Preview.SeekAnimations(TimeSpan.Zero);
            Preview.SetImage((System.Windows.Media.Drawing)null);
            string path = Path.Combine(cache, name + ".svg");
            Status.Text = "Loading " + name + "…";
            if (File.Exists(path)) { LoadPreview(path, request); return; }
            var worker = new BackgroundWorker();
            worker.DoWork += (s, args) =>
            {
                Directory.CreateDirectory(cache);
                string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    // WebClient keeps this sample compatible with the .NET 4.0 target.
#pragma warning disable SYSLIB0014
                    using (var client = new WebClient()) client.DownloadFile(DownloadUrl + name + ".svg", temporary);
#pragma warning restore SYSLIB0014
                    // Only install a complete SVG into the cache after a successful download.
                    var document = new System.Xml.XmlDocument { XmlResolver = null };
                    document.Load(temporary);
                    if (document.DocumentElement == null || document.DocumentElement.LocalName != "svg")
                        throw new InvalidDataException("The download was not an SVG document.");
                    if (!File.Exists(path)) File.Move(temporary, path);
                }
                finally { if (File.Exists(temporary)) File.Delete(temporary); }
            };
            worker.RunWorkerCompleted += (s, args) =>
            {
                worker.Dispose();
                if (request != generation || !IsLoaded) return;
                if (args.Error != null) Status.Text = "Could not download " + name + ": " + args.Error.Message + ". Select it again to retry.";
                else LoadPreview(path, request);
            };
            worker.RunWorkerAsync();
        }
        private void LoadPreview(string path, int request)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                if (request != generation || !IsLoaded) return;
                try
                {
                    var timer = Stopwatch.StartNew();
                    Preview.SetImage(path);
                    selectedFile = path;
                    eventTargets = Regex.Matches(File.ReadAllText(path), @"begin=""([^"";]+)\.click")
                        .Cast<Match>().Select(m => m.Groups[1].Value).Distinct().ToArray();
                    PlayButton.IsEnabled = true;
                    Status.Text = Path.GetFileNameWithoutExtension(path) + " · loaded in " + timer.ElapsedMilliseconds + " ms · " +
                        (eventTargets.Length > 0 ? "Click Play to start." : "Playing automatically.");
                }
                catch (Exception ex) { Status.Text = "Could not render this example: " + ex.Message; }
            }));
        }
        private void Play(object sender, RoutedEventArgs e)
        {
            if (selectedFile == null) return;
            Preview.SeekAnimations(TimeSpan.Zero);
            Preview.SetImage(selectedFile);
            foreach (string target in eventTargets) Preview.TriggerAnimationEvent(target);
        }
        private void PreviewClicked(object sender, System.Windows.Input.MouseButtonEventArgs e) { Play(sender, e); }
        private static void Navigate(string url) { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        private void OpenLink(object sender, RequestNavigateEventArgs e) { Navigate(e.Uri.AbsoluteUri); e.Handled = true; }
        private void OpenExamples(object sender, RoutedEventArgs e) { Navigate(ExamplesUrl); }
    }
}
