using E.Standard.Drawing;
using E.Standard.Web.Abstractions;
using MigraDocCore.DocumentObjectModel.MigraDoc.DocumentObjectModel.Shapes;
using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.IO;

namespace E.Standard.Plot;

public class Picture2Pdf
{
    public float MarginTop = 10, MarginBottom = 10, MarginRight = 10, MarginLeft = 10;
    private double _pageW = 210, _pageH = 297;
    private static object locker = new object();

    public double PageWidth { get { return _pageW; } set { _pageW = value; } }
    public double PageHeight { get { return _pageH; } set { _pageH = value; } }

    public System.IO.MemoryStream Convert(IHttpService httpService, MemoryStream imageStream)
    {
        if (ImageSource.ImageSourceImpl == null)
        {
            ImageSource.ImageSourceImpl = new DrawingImageSource(httpService);
        }

        lock (locker)
        {
            PdfDocument doc = new PdfDocument();
            PdfPage page = doc.AddPage();
            //page.Width = XUnit.FromMillimeter(PageWidth);
            //page.Height = XUnit.FromMillimeter(PageHeight);

            //page.TrimMargins = new TrimMargins()
            //{
            //    Top = XUnit.FromMillimeter(MarginTop),
            //    Left = XUnit.FromMillimeter(MarginLeft),
            //    Bottom = XUnit.FromMillimeter(MarginBottom),
            //    Right = XUnit.FromMillimeter(MarginRight)
            //};

            page.Width = XUnit.FromMillimeter(PageWidth > PageHeight ? PageHeight : PageWidth);      // muss umgedreht werden, da PDF-Writer es sonst nicht versteht...
            page.Height = XUnit.FromMillimeter(PageWidth > PageHeight ? PageWidth : PageHeight);

            page.Orientation = (PageWidth > PageHeight ? PageOrientation.Landscape : PageOrientation.Portrait);

            //var imageStream = new MemoryStream(File.ReadAllBytes(picFilename));

            using (XImage image = XImage.FromStream(() => imageStream))  //.FromFile(picFilename);
            {
                XGraphics gfx = XGraphics.FromPdfPage(page);
                //gfx.DrawImage(image,
                //    new XRect(0, 0, image.PointWidth, image.PointHeight),
                //    new XRect(0, 0, page.Width - XUnit.FromMillimeter(MarginLeft + MarginRight), page.Height - XUnit.FromMillimeter(MarginTop + MarginBottom)), XGraphicsUnit.Point);

                //gfx.DrawImage(image,
                //    new XRect(0, 0, image.PointWidth, image.PointHeight),
                //    new XRect(0, 0, page.Width, page.Height), XGraphicsUnit.Point);

                gfx.DrawImage(image,
                    XUnit.FromMillimeter(MarginLeft).Point,
                    XUnit.FromMillimeter(MarginTop).Point,
                    page.Width - XUnit.FromMillimeter(MarginLeft + MarginRight).Point,
                    page.Height - XUnit.FromMillimeter(MarginTop + MarginBottom).Point);

                MemoryStream ms = new MemoryStream();
                doc.Save(ms);
                return ms;
            }
        }
    }
}
