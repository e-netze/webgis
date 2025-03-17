using E.Standard.Drawing;
using E.Standard.Web.Abstractions;
using MigraDocCore.DocumentObjectModel.MigraDoc.DocumentObjectModel.Shapes;
using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System;
using System.IO;

namespace E.Standard.Plot;

public abstract class MultiPageDocument : IDisposable
{
    public void Dispose()
    {
        //foreach (Image image in _images)
        //    image.Dispose();
        //_images.Clear();
    }

    public abstract bool AddPage(
            IHttpService httpService, byte[] imageData,
            double pageWidth, double pageHeight,
            int marginTop = 10, int marginBottom = 10, int marginRight = 10, int marginLeft = 10,
            string footerText = ""
        );
    public abstract System.IO.MemoryStream Generate();

    public static MultiPageDocument CreateMultipageDocument()
    {
        return new ConvertToMultiPageDocument();
    }


    public class ConvertToMultiPageDocument : MultiPageDocument
    {

        private PdfDocument _doc;

        public ConvertToMultiPageDocument()
        {
            _doc = new PdfDocument();
        }

        override public bool AddPage(
                IHttpService httpService, byte[] imageData,
                double pageWidth, double pageHeight,
                int marginTop = 10, int marginBottom = 10, int marginRight = 10, int marginLeft = 10,
                string footerText = ""
            )
        {
            try
            {
                if (ImageSource.ImageSourceImpl == null)
                {
                    ImageSource.ImageSourceImpl = new DrawingImageSource(httpService);
                }

                // Section
                PdfPage page = _doc.AddPage();

                page.Width = XUnit.FromMillimeter(pageWidth > pageHeight ? pageHeight : pageWidth);      // muss umgedreht werden, da PDF-Writer es sonst nicht versteht...
                page.Height = XUnit.FromMillimeter(pageWidth > pageHeight ? pageWidth : pageHeight);

                page.Orientation = (pageWidth > pageHeight ? PageOrientation.Landscape : PageOrientation.Portrait);

                using (XImage image = XImage.FromStream(() => new MemoryStream(imageData)))
                {
                    XGraphics gfx = XGraphics.FromPdfPage(page);

                    gfx.DrawImage(image,
                            XUnit.FromMillimeter(marginLeft).Point,
                            XUnit.FromMillimeter(marginTop).Point,
                            page.Width - XUnit.FromMillimeter(marginLeft + marginRight).Point,
                            page.Height - XUnit.FromMillimeter(marginTop + marginBottom).Point
                        );

                    if (!String.IsNullOrEmpty(footerText))
                    {
                        var bottom = XUnit.FromMillimeter(marginBottom).Point;

                        var font = new XFont("Arial", Math.Min(marginBottom, 9.5), XFontStyle.Regular);
                        var textColor = XBrushes.Black;
                        var layout = new XRect(0, page.Height - bottom, page.Width, bottom);
                        var format = XStringFormats.Center;

                        gfx.DrawString(footerText, font, textColor, layout, format);
                    }
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        override public System.IO.MemoryStream Generate()
        {
            MemoryStream ms = new MemoryStream();
            _doc.Save(ms);
            return ms;
        }

        //static string ImageStream2Base64(Image gdiImage)
        //{
        //    MemoryStream imageStream = new MemoryStream();
        //    gdiImage.Save(imageStream, System.Drawing.Imaging.ImageFormat.Png);
        //    imageStream.Position = 0;

        //    int count = (int)imageStream.Length;
        //    byte[] data = new byte[count];
        //    imageStream.Read(data, 0, count);

        //    return "base64:" + Convert.ToBase64String(data);
        //}
    }
}
