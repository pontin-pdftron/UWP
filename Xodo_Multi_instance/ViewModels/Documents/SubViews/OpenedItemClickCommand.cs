using CompleteReader.ViewModels.Viewer.Helpers;
using Windows.UI.Xaml.Controls;

namespace CompleteReader.ViewModels.Document.SubViews
{
    public partial class OpenedDocumentsViewModel
    {
        private void OpenedItemClick(object parameter)
        {
            if (parameter is ItemClickEventArgs args && args.ClickedItem is CompleteReaderPDFViewCtrlTabInfo tab)
            {
                OpenedFileSelected?.Invoke(tab.OriginalFile);
            }
        }
    }
}
