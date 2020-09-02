using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Resources;
using DotNetProjects.SVGImage.SVG.FileLoaders;

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

        public string FileSource
        {
            get { return (string)GetValue(FileSourceProperty); }
            set { SetValue(FileSourceProperty, value); }
        }

        public static readonly DependencyProperty FileSourceProperty =
            DependencyProperty.Register("FileSource", typeof(string), typeof(SVGImage), new PropertyMetadata(OnFileSourceChanged));
        static void OnFileSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SVGImage)d).SetImage(new FileStream(e.NewValue.ToString(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
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

        public bool UseAnimations
        {
            get { return (bool)GetValue(UseAnimationsProperty); }
            set { SetValue(UseAnimationsProperty, value); }
        }

        public static readonly DependencyProperty UseAnimationsProperty =
            DependencyProperty.Register("UseAnimations", typeof(bool), typeof(SVGImage), new PropertyMetadata(true));

        public Color? OverrideColor
        {
            get { return (Color?)GetValue(OverrideColorProperty); }
            set { SetValue(OverrideColorProperty, value); }
        }

        public static readonly DependencyProperty OverrideColorProperty =
            DependencyProperty.Register("OverrideColor", typeof(Color?), typeof(SVGImage), new PropertyMetadata(null));

        public Dictionary<string, Brush> CustomBrushes
        {
            get { return (Dictionary<string, Brush>)GetValue(CustomBrushesProperty); }
            set { SetValue(CustomBrushesProperty, value); }
        }

        public static readonly DependencyProperty CustomBrushesProperty =
            DependencyProperty.Register(nameof(CustomBrushes),
                typeof(Dictionary<string, Brush>),
                typeof(SVGImage),
                new FrameworkPropertyMetadata(default, FrameworkPropertyMetadataOptions.AffectsRender, CustomBrushesPropertyChanged));

        private static void CustomBrushesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SVGImage svgImage && e.NewValue is Dictionary<string, Brush> newBrushes)
            {
                if(svgImage._render != null)
                {
                    if (svgImage._render.CustomBrushes != null)
                    {
                        Dictionary<string, Brush> newCustomBrushes = new Dictionary<string, Brush>(svgImage._render.CustomBrushes);
                        foreach (var brush in newBrushes)
                        {
                            newCustomBrushes[brush.Key] = brush.Value;
                        }
                        svgImage._render.CustomBrushes = newCustomBrushes;
                    }
                    else
                    {
                        svgImage._render.CustomBrushes = newBrushes;
                    }
                }
                svgImage.InvalidateVisual();
                svgImage.ReRenderSvg();
            }
        }

        public IExternalFileLoader ExternalFileLoader
        {
            get { return (IExternalFileLoader)GetValue(ExternalFileLoaderProperty); }
            set { SetValue(ExternalFileLoaderProperty, value); }
        }

        public static readonly DependencyProperty ExternalFileLoaderProperty =
            DependencyProperty.Register("ExternalFileLoader", typeof(IExternalFileLoader), typeof(SVGImage), new PropertyMetadata(FileSystemLoader.Instance));


        private Drawing m_drawing;
        private TranslateTransform m_offsetTransform = new TranslateTransform();
        private ScaleTransform m_scaleTransform = new ScaleTransform();
        private SVGRender _render;

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
            loadImage = (render) =>
            {
                this.SetImage(render.LoadDrawing(svgFilename));
            };

            if (this.IsInitialized || System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                _render = new SVGRender();
                _render.ExternalFileLoader = this.ExternalFileLoader;
                _render.UseAnimations = false;
                _render.OverrideColor = OverrideColor;
                _render.CustomBrushes = CustomBrushes;
                loadImage(_render);
                loadImage = null;
            }
        }

        public SVG SVG
        {
            get
            {
                return _render?.SVG;
            }
        }

        public void ReRenderSvg()
        {
            if (_render != null)
                this.SetImage(_render.CreateDrawing(_render.SVG));
        }

        public void SetImage(Stream stream)
        {
            loadImage = (render) =>
            {
                this.SetImage(render.LoadDrawing(stream));
            };

            if (this.IsInitialized || System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                _render = new SVGRender();
                _render.ExternalFileLoader = this.ExternalFileLoader;
                _render.OverrideColor = OverrideColor;
                _render.CustomBrushes = CustomBrushes;
                _render.UseAnimations = false;
                loadImage(_render);
                loadImage = null;
            }
        }

        private Action<SVGRender> loadImage = null;

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            if (loadImage != null)
            {
                _render = new SVGRender();
                _render.ExternalFileLoader = this.ExternalFileLoader;
                _render.OverrideColor = OverrideColor;
                _render.CustomBrushes = CustomBrushes;
                _render.UseAnimations = this.UseAnimations;
                loadImage(_render);
                loadImage = null;
                var brushesFromSVG = new Dictionary<string, Brush>();
                foreach (var server in _render.SVG.PaintServers.GetServers())
                {
                    brushesFromSVG[server.Key] = server.Value.GetBrush();
                }
                CustomBrushes = brushesFromSVG;
            }
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
                dc.DrawRectangle(this.Background, null, new Rect(0, 0, this.ActualWidth, this.ActualHeight));
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
                        double width = r.Width * scale;
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
                        double height = r.Height * scale;
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
