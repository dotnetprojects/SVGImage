using System.Windows.Controls;
using System.Windows;

using SVGImage.SVG;

namespace SVGImage.Canvases
{
	class SVGObjectTree : TreeView
	{
		public static readonly DependencyProperty TagShapeProperty =
				DependencyProperty.RegisterAttached("TagShape", typeof(Shape), typeof(TreeViewItem));

		public Shape SelectedShape
		{
			get 
			{ 
				TreeViewItem item = this.SelectedItem as TreeViewItem;
				if (item == null)
					return null;
				return item.GetValue(TagShapeProperty) as Shape;
			}
		}
		public void Populate(SVG.SVG data)
		{
			this.Items.Clear();
			foreach (Shape shape in data.Elements)
			{
				TreeViewItem root = new TreeViewItem();
				root.Header = "root";
				this.Items.Add(root);
				this.AddShape(shape, root);
				//this.Items.Add(AddShape(shape, root));
			}
		}
		TreeViewItem AddShape(Shape shape, TreeViewItem parentitem)
		{
			TreeViewItem item = new TreeViewItem();
			item.Header = string.Format("{0}:{1}", shape.Id, shape.GetType());
			item.SetValue(TagShapeProperty, shape);
			if (parentitem != null)
				parentitem.Items.Add(item);
			if (shape is Group)
			{
				foreach (Shape child in ((Group)shape).Elements)
					this.AddShape(child, item);
			}
			return item;
		}
	}
}
