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
		}
		public SizeTypeForm()
		{
			InitializeComponent();
		}
	}
}
