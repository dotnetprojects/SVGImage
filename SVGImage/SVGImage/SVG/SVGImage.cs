using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Resources;

namespace SVGImage.SVG
{
	public class SVGImage : Control
	{
		public enum eSizeType
		{
			None,
			ContentToSizeNoStretch,
			ContentToSizeStretch,
			SizeToContent,
		}

		public static DependencyProperty SizeTypeProperty = DependencyProperty.Register("SizeType",
			typeof(eSizeType), 
			typeof(SVGImage), 
			new FrameworkPropertyMetadata(eSizeType.ContentToSizeNoStretch, 
				FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
				new PropertyChangedCallback(OnSizeTypeChanged)));

		static void OnSizeTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			SVGImage ctrl = d as SVGImage;
			ctrl.RecalcImage();
		}

		public eSizeType SizeType
		{
			get { return (eSizeType)this.GetValue(SizeTypeProperty); }
			set { this.SetValue(SizeTypeProperty, value); }
		}

        public string Source
        {
            get { return (string)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(string), typeof(SVGImage), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, new PropertyChangedCallback(OnSourceChanged)));
        static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            StreamResourceInfo resource = Application.GetResourceStream(new Uri(e.NewValue.ToString(), UriKind.Relative));
            ((SVGImage)d).SetImage(resource.Stream);
        }
        
		public static DependencyProperty ImageSourcePoperty = DependencyProperty.Register("ImageSource",
			typeof(Drawing),
			typeof(SVGImage),
			new FrameworkPropertyMetadata(null, 
				FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, 
				new PropertyChangedCallback(OnImageSourceChanged)));
		
		static void OnImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((SVGImage)d).SetImage(e.NewValue as Drawing);
		}
		public Drawing ImageSource
		{
			get { return (Drawing)this.GetValue(ImageSourcePoperty); }
			set { this.SetValue(ImageSourcePoperty, value); }
		}

		Drawing m_drawing;
		TranslateTransform m_offsetTransform = new TranslateTransform();
		ScaleTransform m_scaleTransform = new ScaleTransform();

		static SVGImage()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(SVGImage), new FrameworkPropertyMetadata(typeof(SVGImage)));
			ClipToBoundsProperty.OverrideMetadata(typeof(SVGImage), new FrameworkPropertyMetadata(true));
			SnapsToDevicePixelsProperty.OverrideMetadata(typeof(SVGImage), new FrameworkPropertyMetadata(true));
		}
		public SVGImage()
		{
			this.ClipToBounds = true;
			this.SnapsToDevicePixels = true;
		}
		public void SetImage(string svgFilename)
		{
			SVGRender render = new SVGRender();
			this.SetImage(render.LoadDrawing(svgFilename));
		}
        public void SetImage(Stream stream)
        {
            SVGRender render = new SVGRender();
            this.SetImage(render.LoadDrawing(stream));
        }
		public void SetImage(Drawing drawing)
		{
			this.m_drawing = drawing;
			this.InvalidateVisual();
			if (this.m_drawing != null && this.SizeType == eSizeType.SizeToContent)
				this.InvalidateMeasure();
			this.RecalcImage();
		}
		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);
			this.RecalcImage();
			this.InvalidateVisual();
		}
		
		// Notice TemplateBinding BackGround must be removed from the Border in the default template (or remove Border from the template)
		// Border renders the background AFTER the child render has been called
		// http://social.msdn.microsoft.com/Forums/en-US/wpf/thread/1575d2af-8e86-4085-81b8-a8bf24268e51/?prof=required
		protected override void OnRender(DrawingContext dc)
		{
			//base.OnRender(dc);
			if (this.Background != null)
				dc.DrawRectangle(this.Background, null, new Rect(0,0, this.ActualWidth, this.ActualHeight));
			if (this.m_drawing == null)
				return;
			dc.PushTransform(this.m_offsetTransform);
			dc.PushTransform(this.m_scaleTransform);
			dc.DrawDrawing(this.m_drawing);
			dc.Pop();
			dc.Pop();
		}
		void RecalcImage()
		{
			if (this.m_drawing == null)
				return;
			
			Rect r = this.m_drawing.Bounds;
			if (this.SizeType == eSizeType.None)
			{
				this.m_scaleTransform.ScaleX = 1;
				this.m_scaleTransform.ScaleY = 1;
				switch (this.HorizontalContentAlignment)
				{
					case System.Windows.HorizontalAlignment.Center:
						this.m_offsetTransform.X = this.ActualWidth / 2 - r.Width / 2 - r.Left;
						break;
					case System.Windows.HorizontalAlignment.Right:
						this.m_offsetTransform.X = this.ActualWidth - r.Right;
						break;
					default:
						this.m_offsetTransform.X = -r.Left; // move to left by default
						break;
				}
				switch (this.VerticalContentAlignment)
				{
					case System.Windows.VerticalAlignment.Center:
						this.m_offsetTransform.Y = this.ActualHeight / 2 - r.Height / 2;
						break;
					case System.Windows.VerticalAlignment.Bottom:
						this.m_offsetTransform.Y = this.ActualHeight - r.Height - r.Top;
						break;
					default:
						this.m_offsetTransform.Y = -r.Top; // move to top by default
						break;
				}
				return;
			}
			if (this.SizeType == eSizeType.ContentToSizeNoStretch)
			{
				this.SizeToContentNoStretch(this.HorizontalContentAlignment, this.VerticalContentAlignment);
				return;
			}
			if (this.SizeType == eSizeType.ContentToSizeStretch)
			{
				double xscale = this.ActualWidth / r.Width;
				double yscale = this.ActualHeight / r.Height;
				this.m_scaleTransform.CenterX = r.Left;
				this.m_scaleTransform.CenterY = r.Top;
				this.m_scaleTransform.ScaleX = xscale;
				this.m_scaleTransform.ScaleY = yscale;

				this.m_offsetTransform.X = -r.Left;
				this.m_offsetTransform.Y = -r.Top;
				return;
			}
			if (this.SizeType == eSizeType.SizeToContent)
			{
				if (r.Width > this.ActualWidth || r.Height > this.ActualHeight)
					this.SizeToContentNoStretch(HorizontalAlignment.Left, VerticalAlignment.Top);
				else
				{
					this.m_scaleTransform.CenterX = r.Left;
					this.m_scaleTransform.CenterY = r.Top;
					this.m_scaleTransform.ScaleX = 1;
					this.m_scaleTransform.ScaleY = 1;

					this.m_offsetTransform.X = -r.Left; // move to left by default
					this.m_offsetTransform.Y = -r.Top; // move to top by default
				}
				return;
			}
		}
		void SizeToContentNoStretch(HorizontalAlignment hAlignment, VerticalAlignment vAlignment)
		{
			Rect r = this.m_drawing.Bounds;
			double xscale = this.ActualWidth / r.Width;
			double yscale = this.ActualHeight / r.Height;
			double scale = xscale;
			if (scale > yscale)
				scale = yscale;

			this.m_scaleTransform.CenterX = r.Left;
			this.m_scaleTransform.CenterY = r.Top;
			this.m_scaleTransform.ScaleX = scale;
			this.m_scaleTransform.ScaleY = scale;

			this.m_offsetTransform.X = -r.Left;
			if (scale < xscale)
			{
				switch (this.HorizontalContentAlignment)
				{
					case System.Windows.HorizontalAlignment.Center:
						double width = r.Width*scale;
						this.m_offsetTransform.X = this.ActualWidth / 2 - width / 2 - r.Left;
						break;
					case System.Windows.HorizontalAlignment.Right:
						this.m_offsetTransform.X = this.ActualWidth - r.Right * scale;
						break;
				}
			}
			this.m_offsetTransform.Y = -r.Top;
			if (scale < yscale)
			{
				switch (this.VerticalContentAlignment)
				{
					case System.Windows.VerticalAlignment.Center:
						double height = r.Height*scale;
						this.m_offsetTransform.Y = this.ActualHeight / 2 - height / 2 - r.Top;
						break;
					case System.Windows.VerticalAlignment.Bottom:
						this.m_offsetTransform.Y = this.ActualHeight - r.Height * scale - r.Top;
						break;
				}
			}
		}
		protected override Size MeasureOverride(Size constraint)
		{	
			Size result = base.MeasureOverride(constraint);
			if (this.SizeType == eSizeType.SizeToContent)
			{
				if (this.m_drawing != null && !this.m_drawing.Bounds.Size.IsEmpty)
					result = this.m_drawing.Bounds.Size;
			}			
			if (constraint.Width > 0 && constraint.Width < result.Width)
				result.Width = constraint.Width;
			if (constraint.Height > 0 && constraint.Height < result.Height)
				result.Height = constraint.Height;
			//Console.WriteLine("MasureOverride({0}), SizeType = {1}, result {2}", constraint.ToString(), SizeType.ToString(), result.ToString());
			return result;
		}
		protected override Size ArrangeOverride(Size arrangeBounds)
		{
			Size result = base.ArrangeOverride(arrangeBounds);
			if (this.SizeType == eSizeType.SizeToContent)
			{
				if (this.m_drawing != null && !this.m_drawing.Bounds.Size.IsEmpty)
					result = this.m_drawing.Bounds.Size;
			}						 
			if (arrangeBounds.Width > 0 && arrangeBounds.Width < result.Width)
				result.Width = arrangeBounds.Width;
			if (arrangeBounds.Height > 0 && arrangeBounds.Height < result.Height)
				result.Height = arrangeBounds.Height;
			//Console.WriteLine("ArrangeOverride({0}), SizeType = {1}, result {2}", arrangeBounds.ToString(), SizeType.ToString(), result.ToString());
			return result;
		}

	}
}
