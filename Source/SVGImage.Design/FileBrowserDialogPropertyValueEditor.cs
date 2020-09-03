using System;
using System.ComponentModel;
using System.Windows;
using Microsoft.Windows.Design.Metadata;
using Microsoft.Windows.Design.PropertyEditing;
using Microsoft.Win32;

namespace SVGImage.Design
{
    public class FileBrowserDialogPropertyValueEditor : DialogPropertyValueEditor
    {
        private EditorResources res = new EditorResources();

        public FileBrowserDialogPropertyValueEditor()
        {
            this.InlineEditorTemplate = res["FileBrowserInlineEditorTemplate"] as DataTemplate;
        }

        public override void ShowDialog(
            PropertyValue propertyValue,
            IInputElement commandSource)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = false;
     
            if (ofd.ShowDialog() == true)
            {
                propertyValue.StringValue = ofd.FileName;
            }
        }
    }
}
