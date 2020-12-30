using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompleteReader.Utilities
{
    public static class Constants
    {
        public const double PhoneWidthThreshold = 450;

        public static bool IsPhoneWidth()
        {
            return Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().VisibleBounds.Width < PhoneWidthThreshold;
        }

        public const pdftron.PDF.PDFViewCtrlPagePresentationMode DefaultPagePresentationMode = pdftron.PDF.PDFViewCtrlPagePresentationMode.e_single_page;
    }
}
