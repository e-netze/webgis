using E.Standard.Extensions.Text;
using E.Standard.Web.Extensions;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core;

class ImageMerger : IDisposable
{
    private readonly List<object> _bitmaps;
    private readonly List<int> _orderList;
    private readonly List<float> _opacity;
    protected int m_max = 0;
    protected string m_outputUrl = "";
    protected string m_outputPath = "";
    private object thisLock = new object();
    private ImageFormat _imageFormat = ImageFormat.Png;
    private IMap _map = null;
    private IRequestContext _requestContext;
    private bool _makeTransparent = false;

    public ImageMerger(IMap map, IRequestContext requestContext)
    {
        _bitmaps = new List<object>();
        _orderList = new List<int>();
        _opacity = new List<float>();

        _map = map;
        _requestContext = requestContext;
    }

    public void clear()
    {
        _bitmaps.Clear();
        _orderList.Clear();
        _opacity.Clear();
        m_max = 0;
    }
    public int Count
    {
        get
        {
            return _bitmaps.Count;
        }
    }
    public string outputPath
    {
        set { m_outputPath = value; }
        get { return m_outputPath; }
    }
    public string outputUrl
    {
        set { m_outputUrl = value; }
        get { return m_outputPath; }
    }

    public ImageFormat ImageFormat
    {
        get { return _imageFormat; }
        set { _imageFormat = value; }
    }

    public void Add(string path, int order, float opacity)
    {
        lock (thisLock)
        {
            for (int i = 0; i < _bitmaps.Count; i++)
            {
                int o = Convert.ToInt32(_orderList[i]);
                if (order < o)
                {
                    _bitmaps.Insert(i, path);
                    _orderList.Insert(i, order);
                    _opacity.Insert(i, opacity);
                    return;
                }
            }

            _bitmaps.Add(path);
            _orderList.Add(order);
            _opacity.Add(opacity);
        }
    }

    public void Add(IBitmap bitmap, int order, float opacity)
    {
        lock (thisLock)
        {
            if (bitmap == null)
            {
                this.max--;
                return;
            }
            for (int i = 0; i < _bitmaps.Count; i++)
            {
                int o = Convert.ToInt32(_orderList[i]);
                if (order < o)
                {
                    _bitmaps.Insert(i, bitmap);
                    _orderList.Insert(i, order);
                    _opacity.Insert(i, opacity);
                    return;
                }
            }

            _bitmaps.Add(bitmap);
            _orderList.Add(order);
            _opacity.Add(opacity);
        }
    }

    public int max
    {
        get { return m_max; }
        set { m_max = value; }
    }

    public bool MakeTransparent
    {
        get { return _makeTransparent; }
        set { _makeTransparent = value; }
    }

    public ArgbColor? TransparentColor { get; set; }


    async public Task<(string imagePath, string imageUrl, IEnumerable<Exception> expections)>
        Merge(int iWidth, int iHeight, bool cleanup = true)
    {
        string imageUrl = String.Empty;
        var exceptions = new List<Exception>();

        try
        {
            DateTime time = DateTime.Now;
            string fileName = $"merged_{time.Ticks}_{Guid.NewGuid().ToString("N").ToLower()}.{(_imageFormat == ImageFormat.Jpeg ? "jpg" : "png")}";

            List<string> cleanupFiles = new List<string>();

            using (var image = Current.Engine.CreateBitmap(iWidth, iHeight, PixelFormat.Rgba32))
            using (var canvas = image.CreateCanvas())
            {
                if (_makeTransparent)
                {
                    if (this.TransparentColor.HasValue)
                    {
                        using (var brush = Current.Engine.CreateSolidBrush(this.TransparentColor.Value))
                        {
                            canvas.FillRectangle(brush, new CanvasRectangle(0, 0, image.Width, image.Height));
                        }
                    }
                    else
                    {
                        try
                        {
                            image.MakeTransparent();
                        }
                        catch { }
                    }
                }
                else
                {
                    using (var brush = Current.Engine.CreateSolidBrush(_map.BackgroundColor))
                    {
                        canvas.FillRectangle(brush, new CanvasRectangle(0, 0, image.Width, image.Height));
                    }
                }

                for (int i = 0; i < _bitmaps.Count; i++)
                {
                    object pic = _bitmaps[i];
                    float opacity = (float)_opacity[i];

                    try
                    {
                        using (IBitmap img = ((pic is IBitmap) ? (IBitmap)pic : await pic.ToString().ImageFromUri(_requestContext.Http)))
                        {
                            if (cleanup)
                            {
                                if (pic is string &&
                                    !String.IsNullOrEmpty(pic.ToString()) &&
                                    !pic.ToString().ToLower().StartsWith("http://") &&
                                    !pic.ToString().ToLower().StartsWith("https://"))
                                {
                                    cleanupFiles.Add(pic.ToString());
                                }
                            }
                            _bitmaps[i] = null;

                            if (img != null)
                            {
                                if (opacity >= 0 && opacity < 1.0)
                                {
                                    canvas.DrawBitmap(img,
                                        new CanvasRectangle(0, 0, iWidth, iHeight),
                                        new CanvasRectangle(0, 0, img.Width, img.Height), opacity);
                                }
                                else
                                {
                                    canvas.DrawBitmap(img,
                                        new CanvasRectangle(0, 0, iWidth, iHeight),
                                        new CanvasRectangle(0, 0, img.Width, img.Height));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);

                        //Console.WriteLine($"{DateTime.Now.ToString()}: ImageMerger Exception ({ex.GetType().ToString()})");
                        //Console.WriteLine(ex.Message);
                        //Console.WriteLine(ex.StackTrace);
                        //Console.WriteLine("----------------------------------------------------");
                        //conn.LogString(ex.Message);

                        _requestContext.GetRequiredService<IExceptionLogger>()
                            .LogException(_map, String.Empty, String.Empty, "ImageMerge", ex);
                    }
                }

                #region Cleanup (delete files from disc) 

                foreach (var cleanupFile in cleanupFiles)
                {
                    try
                    {
                        var fi = new FileInfo(cleanupFile);
                        if (fi.Exists)
                        {
                            fi.Delete();
                        }
                    }
                    catch { }
                }

                #endregion

                if (this.MakeTransparent && this.TransparentColor.HasValue)
                {
                    try
                    {
                        image.MakeTransparent(this.TransparentColor.Value);
                    }
                    catch { }
                }

                imageUrl = m_outputUrl.AddUriPath(fileName);
                fileName = m_outputPath.AddUriPath(fileName);

                await image.SaveOrUpload(fileName, _imageFormat);
                return (fileName, imageUrl, exceptions);
            }
        }
        catch (Exception ex)
        {
            exceptions.Add(ex);

            _requestContext.GetRequiredService<IExceptionLogger>()
                .LogException(_map, String.Empty, String.Empty, "MergeImage", ex);

            return (String.Empty, String.Empty, exceptions);
        }
    }

    private PixelFormat NonIndedexedPixelFormat(IBitmap bm)
    {
        if (bm == null)
        {
            return PixelFormat.Rgba32;
        }

        switch (bm.PixelFormat)
        {
            case PixelFormat.Gray8:
            case PixelFormat.Rgb24:
                return PixelFormat.Rgba32;
        }

        return bm.PixelFormat;
    }

    #region IDisposable Member

    public void Dispose()
    {
        try
        {
            for (int i = 0; i < _bitmaps.Count; i++)
            {
                var bitmap = _bitmaps[i] as IDisposable;
                if (bitmap != null)
                {
                    bitmap.Dispose();
                }
            }
            _bitmaps.Clear();
        }
        catch (Exception ex)
        {
            _requestContext.GetRequiredService<IExceptionLogger>()
                .LogException(_map, String.Empty, String.Empty, "Dispose", ex);
        }
    }

    #endregion
}
