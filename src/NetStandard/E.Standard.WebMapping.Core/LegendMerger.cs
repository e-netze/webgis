using E.Standard.Platform;
using E.Standard.Web.Extensions;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core;

class LegendMerger : IDisposable
{
    private List<IBitmap> _picList;
    private List<string> _titleList;
    private List<int> _orderList;
    private object thisLock = new object();
    protected string _outputUrl = "";
    protected string _outputPath = "";
    private IMap _map = null;
    private IRequestContext _requestContext;

    public LegendMerger(IMap map, IRequestContext requestContext)
    {
        _picList = new List<IBitmap>();
        _titleList = new List<string>();
        _orderList = new List<int>();

        _map = map;
        _requestContext = requestContext;
    }

    public string outputPath
    {
        set { _outputPath = value; }
        get { return _outputPath; }
    }
    public string outputUrl
    {
        set { _outputUrl = value; }
        get { return _outputPath; }
    }

    public void Clear()
    {
        foreach (var image in _picList)
        {
            if (image != null)
            {
                image.Dispose();
            }
        }
        _picList.Clear();
        _titleList.Clear();
        _orderList.Clear();
    }

    public void Add(IBitmap image, string title, int order)
    {
        if (image == null ||
            image.Height <= 5)
        {
            return;
        }

        lock (thisLock)
        {
            for (int i = 0; i < _picList.Count; i++)
            {
                int o = Convert.ToInt32(_orderList[i]);
                if (order < o)
                {
                    _picList.Insert(i, image);
                    _orderList.Insert(i, order);
                    _titleList.Insert(i, title);
                    return;
                }
            }

            _picList.Add(image);
            _orderList.Add(order);
            _titleList.Add(title);
        }
    }

    async public Task<(string imagePath, string imageUrl)> Merge()
    {
        string imageUrl = String.Empty;
        try
        {
            int width = 1, height = 1;

            using (var bm = Current.Engine.CreateBitmap(1, 1))
            using (var canvas = bm.CreateCanvas())
            using (var font = Current.Engine.CreateFont(SystemInfo.DefaultFontName, 11, FontStyle.Bold))
            {
                int i = 0;
                foreach (var image in _picList)
                {
                    width = Math.Max(image.Width, width);
                    width = Math.Max(width, (int)canvas.MeasureText(_titleList[i], font).Width + 2);
                    height += image.Height + 20;
                }
            }

            using (var bm = Current.Engine.CreateBitmap(width, height))
            using (var gr = bm.CreateCanvas())
            using (var font = Current.Engine.CreateFont(SystemInfo.DefaultFontName, 11, FontStyle.Bold))
            using (var whiteBrush = Current.Engine.CreateSolidBrush(ArgbColor.White))
            using (var blackBrush = Current.Engine.CreateSolidBrush(ArgbColor.Black))
            {
                gr.FillRectangle(whiteBrush, new CanvasRectangle(0, 0, bm.Width, bm.Height));
                int y = 0, i = 0;
                var stringFormat = Current.Engine.CreateDrawTextFormat();
                stringFormat.Alignment = StringAlignment.Near;
                stringFormat.LineAlignment = StringAlignment.Near;

                foreach (var image in _picList)
                {
                    gr.DrawText(_titleList[i], font, blackBrush, 1f, 1f + y, stringFormat);
                    gr.DrawBitmap(image, new CanvasPoint(0, y + 20));
                    y += image.Height + 20;
                    i++;
                }

                string fileName = "legend_" + Guid.NewGuid().ToString("N") + ".png";

                imageUrl = _outputUrl + "/" + fileName;
                fileName = _outputPath + @"/" + fileName;

                await bm.SaveOrUpload(fileName, ImageFormat.Png);
                return (fileName, imageUrl);
            }
        }
        catch (Exception ex)
        {
            _requestContext.GetRequiredService<IExceptionLogger>()
                .LogException(_map, String.Empty, String.Empty, "MergeLegend", ex);

            return (String.Empty, String.Empty);
        }
    }

    #region IDisposable Member

    public void Dispose()
    {
        this.Clear();
    }

    #endregion
}
