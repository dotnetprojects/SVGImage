using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.ComponentModel;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SvgTestBox
{
    /// <summary>
    /// Interaction logic for SvgResourceColors.xaml
    /// </summary>
    public partial class SvgResourceColors : Window
    {
        private enum UpdateValueType
        {
            None,
            Rgb,
            Hsv,
            Hex,
            Alpha,
            All
        }

        private bool _isUpdating;
        private Color _brdrOrgColor;

        #region Constructors
        public SvgResourceColors()
        {
            InitializeComponent();

            this.OriginalColor = Colors.Transparent;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the color selected in this dialog.
        /// </summary>
        public Color SelectedColor { get; private set; } = Colors.Transparent;

        /// <summary>
        /// Gets the dialog result of this dialog, based upon whether the user accepted the changes.
        /// </summary>
        public new bool DialogResult { get; private set; } = false;

        public Color OriginalColor
        {
            get {
                return _brdrOrgColor;
            }
            set {
                _brdrOrgColor = value;

                brdrOrgColor.Background = new SolidColorBrush(value);
            }
        }

        #endregion

        private void UpdateSelectedColor(Color color, bool includeSliders = true)
        {
            this.SelectedColor = color;
            brdrSelColor.Background = new SolidColorBrush(color);

            if (_brdrOrgColor == Colors.Transparent)
            {
                this.OriginalColor = color;
            }

            if (includeSliders)
            {
                UpdateValues(color, UpdateValueType.None);
            }
        }

        #region Private Event Handlers

        private void OnColorClicked(object sender, RoutedEventArgs e)
        {
            UpdateSelectedColor(((sender as Button).Background as System.Windows.Media.SolidColorBrush).Color);
        }

        private void OnRValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isUpdating)
            {
                nudR.Value = (int)e.NewValue;
            }
        }

        private void OnGValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isUpdating)
            {
                nudG.Value = (int)e.NewValue;
            }
        }

        private void OnBValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isUpdating)
            {
                nudB.Value = (int)e.NewValue;
            }
        }

        private void OnHexTextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.IsLoaded == false)
            {
                return;
            }
            try
            {
                Color col = (Color)ColorConverter.ConvertFromString(txtHex.Text);
                lblQ.Visibility = Visibility.Hidden;

                UpdateValues(col, UpdateValueType.Hex);
            }
            catch (Exception)
            {
                lblQ.Visibility = Visibility.Visible;
            }
        }

        private void OnAlphaChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
        {
            if (this.IsLoaded == false || _isUpdating)
            {
                return;
            }

            Color col = Color.FromArgb((byte)nudAlpha.IntegerValue, (byte)nudR.IntegerValue, (byte)nudG.IntegerValue, (byte)nudB.IntegerValue);
            UpdateValues(col, UpdateValueType.Alpha);
        }

        private void OnRgbChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
        {
            if (this.IsLoaded == false || _isUpdating)
            {
                return;
            }

            Color col = Color.FromArgb((byte)nudAlpha.IntegerValue, (byte)nudR.IntegerValue, (byte)nudG.IntegerValue, (byte)nudB.IntegerValue);
            UpdateValues(col, UpdateValueType.Rgb);
        }

        private void OnApplyClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion

        #region Color Conversion Functions

        private void UpdateValues(Color color, UpdateValueType except)
        {
            if (except == UpdateValueType.All)
            {
                return;
            }

            _isUpdating = true;

            if (except != UpdateValueType.Rgb)
            {
                nudR.Value = color.R;
                nudG.Value = color.G;
                nudB.Value = color.B;
            }

            if (except != UpdateValueType.Hex)
            {
                txtHex.Text = color.ToString();
            }

            if (except != UpdateValueType.Alpha)
            {
                nudAlpha.IntegerValue = color.A;
            }

            UpdateSelectedColor(color, false);

            _isUpdating = false;
        }

        #endregion
    }

}
