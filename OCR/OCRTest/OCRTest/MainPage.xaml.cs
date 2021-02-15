using System;
using Windows.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;
using pdftron.PDF;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace OCRTest
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Windows.UI.Xaml.Controls.Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void ApplyJSONToPDF()
        {
            var doc = new PDFDoc("myDoc");
            pdftron.PDF.Page page = doc.PageCreate();
            doc.PagePushBack(page);

            // Sample JSON
            JObject json = new JObject
            {
                ["Page"] = new JObject
                {
                    ["Para"] = new JObject
                    {
                        ["Line"] = new JObject
                        {
                            ["Word"] = new JObject
                            (
                                new JProperty("font-size", "27"),
                                new JProperty("length", "64"),
                                new JProperty("orientation", "2"),
                                new JProperty("text", "Hello"),
                                new JProperty("x", "273"),
                                new JProperty("y", "265")

                            ),
                            ["box"] = new JArray("273", "265", "64", "29")
                        }
                    },
                    ["num"] = new JValue("1")
                }
            };

            // Sample XML
            string xml = @"<Doc><Page num=""1""><Para id=""1""><Line id=""1"" box=""49 85 242 10""><Word box=""49 47 11 10"" orientation=""2"">32</Word><Word box=""175 47 55 10"" orientation=""2"">Prudentius</Word></Line></Para></Page></Doc>";

            try
            {
                OCRModule.ApplyOCRJsonToPDF(doc, json.ToString());
                OCRModule.ApplyOCRXmlToPDF(doc, xml);
            }
            catch
            (Exception)
            {
            }
        }
    }
}
