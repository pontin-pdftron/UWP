using System.Collections.Generic;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;

using PDFDouble = pdftron.Common.DoubleRef;
using PDFRect = pdftron.PDF.Rect;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PDFViewerUWP_WindowsInk
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Windows.UI.Xaml.Controls.Page
    {
        pdftron.PDF.PDFViewCtrl PDFViewCtrl;
        pdftron.PDF.Tools.ToolManager ToolManager;
        int mPageToTransfer = 1;

        public MainPage()
        {
            this.InitializeComponent();            

            // Initialize PDFNet and load PDF file into PDFViewer
            pdftron.PDFNet.Initialize("");
            PDFViewCtrl = new pdftron.PDF.PDFViewCtrl();
            pdftron.PDF.PDFDoc doc = new pdftron.PDF.PDFDoc("Resources/GettingStarted.pdf");
            PDFViewCtrl.SetDoc(doc);

            // Initialize ToolManager to allow annotation editing
            ToolManager = new pdftron.PDF.Tools.ToolManager(PDFViewCtrl);

            borderPDVView.Child = PDFViewCtrl;
        }

        private void TransferInkStroke()
        {
            // Get Ink Strokes
            var strokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();

            InsertInkAnnotWithPressure(strokes);
        }

        private void InsertInkAnnotWithPressure(IReadOnlyList<InkStroke> inkStrokes)
        {
            /* Thickness can vary from 1 to X
             *             
             */

            var rect = new pdftron.PDF.Rect(0, 0, 400, 400); // Get a fixed rectangle for sample
            pdftron.PDF.Annots.Ink ink;
            List<pdftron.PDF.Annots.Ink> inks = new List<pdftron.PDF.Annots.Ink>();

            // Cycle through all ink strokes and set ink annotation positions
            int i = 0;
            float segmentPressure = 0;
            PDFDouble x = new PDFDouble();
            PDFDouble y = new PDFDouble();

            foreach (InkStroke inkStroke in inkStrokes)
            {
                var segments = inkStroke.GetRenderingSegments();

                
                int j = 0;
                for (int u = 0; u < segments.Count; u++)
                {
                    if (segments[u].Pressure != segmentPressure)
                    {
                        // Start new Ink annot segment considering it's pressure
                        segmentPressure = segments[u].Pressure;
                        ink = pdftron.PDF.Annots.Ink.Create(PDFViewCtrl.GetDoc().GetSDFDoc(), rect);

                        pdftron.PDF.AnnotBorderStyle bs = ink.GetBorderStyle();
                        bs.width = segmentPressure;
                        ink.SetBorderStyle(bs);
                        //ink.SetOpacity(mOpacity);
                        
                    }

                    x.Value = segments[u].Position.X;
                    y.Value = segments[u].Position.Y;


                    // Ensure to properly convert to canvas coordinates
                    PDFViewCtrl.ConvPagePtToScreenPt(x, y, mPageToTransfer);
                    PDFViewCtrl.ConvScreenPtToAnnotationCanvasPt(x, y);

                    //ink.SetPoint(i, j, new pdftron.PDF.Point(x.Value, y.Value));

                    j++;
                }
                i++;
            }
        }

        private void InsertInkAnnot(IReadOnlyList<InkStroke> inkStrokes)
        {
            var rect =  new pdftron.PDF.Rect(0, 0, 400, 400); // Get a fixed rectangle for sample
            pdftron.PDF.Annots.Ink ink = pdftron.PDF.Annots.Ink.Create(PDFViewCtrl.GetDoc().GetSDFDoc(), rect);

            // Cycle through all ink strokes and set ink annotation positions
            int i = 0;
            PDFDouble x = new PDFDouble();
            PDFDouble y = new PDFDouble();

            foreach (InkStroke inkStroke in inkStrokes)
            {
                var segments = inkStroke.GetRenderingSegments();
                int j = 0;
                for (int u = 0; u < segments.Count; u++)
                {
                    x.Value = segments[u].Position.X;
                    y.Value = segments[u].Position.Y;
                    

                    // Ensure to properly convert to canvas coordinates
                    PDFViewCtrl.ConvPagePtToScreenPt(x, y, mPageToTransfer);
                    PDFViewCtrl.ConvScreenPtToAnnotationCanvasPt(x, y);

                    ink.SetPoint(i, j, new pdftron.PDF.Point(x.Value, y.Value));
                    
                    j++;
                }
                i++;
            }

            // Once Ink Annotation is finished, just add it to the PDF page
            ink.RefreshAppearance();

            pdftron.PDF.Page page = PDFViewCtrl.GetDoc().GetPage(mPageToTransfer);
            page.AnnotPushBack(ink);
            PDFViewCtrl.UpdateWithAnnot(ink, mPageToTransfer);            
        }

        private void btnGetStrokes_Click(object sender, RoutedEventArgs e)
        {
            TransferInkStroke();
            inkCanvas.InkPresenter.StrokeContainer.Clear();
        }
    }
}
