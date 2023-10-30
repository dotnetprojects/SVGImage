using System;

using System.Windows;
using System.Windows.Controls;

using SVGImage.SVG;
using SVGImage.SVG.Shapes;

namespace ClipArtViewer
{
    class SVGObjectTree : TreeView
    {
        public static DependencyProperty TagShapeProperty =
                DependencyProperty.RegisterAttached("TagShape", typeof(Shape), typeof(TreeViewItem), new PropertyMetadata());

        public Shape SelectedShape
        {
            get 
            { 
                TreeViewItem item = SelectedItem as TreeViewItem;
                if (item == null)
                    return null;
                return item.GetValue(TagShapeProperty) as Shape;
            }
        }

        public void Populate(SVG data)
        {
            this.Items.Clear();
            if (data == null)
            {
                return;
            }
            foreach (Shape shape in data.Elements)
            {
                TreeViewItem root = new TreeViewItem();
                root.Header = "root";
                this.Items.Add(root);
                AddShape(shape, root);
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
                    AddShape(child, item);
            }
            return item;
        }
    }
}
