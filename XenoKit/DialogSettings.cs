using MahApps.Metro.Controls.Dialogs;

public static class DialogSettings
{
    public static MetroDialogSettings Default
    {
        get
        {
            return new MetroDialogSettings()
            {
                AnimateShow = false,
                AnimateHide = false,
                DialogTitleFontSize = 16,
                DialogMessageFontSize = 12
            };
        }
    }


}