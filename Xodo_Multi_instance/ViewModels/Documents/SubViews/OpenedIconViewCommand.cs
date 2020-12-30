using System;
using static CompleteReader.ViewModels.Document.SubViews.FolderDocumentsViewModel;

namespace CompleteReader.ViewModels.Document.SubViews
{
    public partial class OpenedDocumentsViewModel
    {
        private void OpenedIconView(object parameter)
        {
            if (parameter is string param)
            {
                if (param.Equals("d", StringComparison.OrdinalIgnoreCase) && CurrentIconView != IconView.Default)
                {
                    CurrentIconView = IconView.Default;
                }
                else if (param.Equals("s", StringComparison.OrdinalIgnoreCase) && CurrentIconView != IconView.Small)
                {
                    CurrentIconView = IconView.Small;
                }
                else if (param.Equals("l", StringComparison.OrdinalIgnoreCase) && CurrentIconView != IconView.List)
                {
                    CurrentIconView = IconView.List;
                }
                else if (param.Equals("c", StringComparison.OrdinalIgnoreCase) && CurrentIconView != IconView.Cover)
                {
                    CurrentIconView = IconView.Cover;
                }
            }
        }
    }
}
