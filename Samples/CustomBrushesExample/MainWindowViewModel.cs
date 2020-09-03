using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace CustomBrushesExample
{
    class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private Dictionary<string, Brush> _myBrushes;
        
        public Dictionary<string, Brush> MyBrushes
        {
            get => _myBrushes;
            set
            {
                _myBrushes = value;
                OnPropertyChanged(nameof(MyBrushes));
            }
        }

        Random random = new Random();

        public void ChangeBrush()
        {
            var stopCollection = new GradientStopCollection();
            stopCollection.Add(new GradientStop(Colors.Red, offset: 0.0));
            stopCollection.Add(new GradientStop(Colors.White, offset: 0.5));
            stopCollection.Add(new GradientStop(Colors.Blue, offset: 1.0));

            var newLinearGradientBrush = new LinearGradientBrush()
            {
                GradientStops = stopCollection
            }; 

            Dictionary<string, Brush> newBrushes = new Dictionary<string, Brush>(MyBrushes);

            var brushKeys = newBrushes.Keys.ToArray();
            var selectedBrushKey = brushKeys[random.Next(0, brushKeys.Length)];

            newBrushes[selectedBrushKey] = newLinearGradientBrush;
            MyBrushes = newBrushes;
        }
    }
}
