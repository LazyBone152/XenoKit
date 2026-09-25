using EEPK_Organiser.Forms;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMM;

namespace XenoKit.Helper
{
    public static class WindowHelper
    {
        public static EmbEditForm GetActiveEmbForm(EMB_TextureFile _embFile)
        {
            foreach (var window in App.Current.Windows)
            {
                if (window is EmbEditForm)
                {
                    EmbEditForm _form = (EmbEditForm)window;

                    if (_form.EmbFile == _embFile)
                        return _form;
                }
            }

            return null;
        }

        public static MaterialsEditorForm GetActiveEmmForm(EMM_File _emmFile)
        {
            foreach (var window in App.Current.Windows)
            {
                if (window is MaterialsEditorForm)
                {
                    MaterialsEditorForm _form = (MaterialsEditorForm)window;

                    if (_form.EmmFile == _emmFile)
                        return _form;
                }
            }

            return null;
        }

    }
}
