using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace CQRSAzure.CQRSdsl.Dsl.CustomCode.UI
{
    /// <summary>
    /// A list of identifier groups which might be the parent of this one 
    /// </summary>
    public sealed class IdentifierGroupNameUITypeEditor
        : UITypeEditor
    {
        private IWindowsFormsEditorService _editorService;

        /// <summary>
        /// Editor type for this property is a drop down list
        /// </summary>
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            if (null != context)
            {
                return UITypeEditorEditStyle.DropDown;
            }
            else
            {
                return base.GetEditStyle(context);
            }
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if ((null != context) && (null != provider))
            {
                _editorService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));

                IdentityGroup idg = null;
                // Note - context.Instance may be either a DSL shape or the underlying domain class
                // so if we add a shape we need to look up the underlying domain class here
                idg = context.Instance as IdentityGroup;
                if (null == idg)
                {
                    IdentityGroupGeometryShape idgs = null;
                    idgs = context.Instance as IdentityGroupGeometryShape;
                    if (null != idgs )
                    {
                        idg = idgs.ModelElement as IdentityGroup;
                    }
                }
                if (null != idg)
                {
                    // Create a listbox to hold all the identifier group names in the parent projection
                    ListBox lb = new ListBox();
                    lb.SelectionMode = SelectionMode.One;
                    lb.SelectedValueChanged += OnListBoxSelectedValueChanged;

                    // Add an Empty item for an identifier group that has no parent
                    lb.Items.Add("");

                    foreach (string idgName in idg.AggregateIdentifier.GetIdentityGroupNames())
                    {
                        lb.Items.Add(idgName);
                    }


                    if (!lb.Items.Contains(@"All"))
                    {
                        // Add the global 
                        lb.Items.Add(@"All");
                    }

                    _editorService.DropDownControl(lb);
                    if (lb.SelectedItem == null) // no selection, return the passed-in value as is
                        return value;

                    return lb.SelectedItem;

                }
            }
            return base.EditValue(context, provider, value);
        }


        private void OnListBoxSelectedValueChanged(object sender, EventArgs e)
        {
            if (null != _editorService)
            {
                // close the drop down as soon as something is clicked
                _editorService.CloseDropDown();
            }

        }
    }
}
