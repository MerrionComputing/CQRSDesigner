using System.ComponentModel;
using System.Drawing.Design;

namespace CQRSAzure.CQRSdsl.CustomCode.UI
{
    public class CodeGenerationSettingsTypeEditor : UITypeEditor
    {


        public override UITypeEditorEditStyle GetEditStyle
        (ITypeDescriptorContext context)
        {
            if (context != null)
            {
                return UITypeEditorEditStyle.Modal;
            }
            return base.GetEditStyle(context);
        }

    }
}
