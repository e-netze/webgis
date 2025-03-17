using gView.GraphicsEngine;
using System;

namespace E.Standard.Drawing;

public class GraphicsEngines
{
    public enum Engines
    {
        SystemDefault = 0,
        GdiPlus = 1,
        Skia = 2
    }

    public static void Init(Engines engine)
    {
        engine = (engine, Platform.SystemInfo.IsWindows) switch
        {
            (Engines.SystemDefault, true) => Engines.GdiPlus,   // default on windows
            (_, false) => Engines.Skia,                         // not windows => always skia
            (_, true) => engine                                 // on windows => both a possible 
        };

        switch (engine)
        {
            case Engines.GdiPlus:
                Current.Engine = new gView.GraphicsEngine.GdiPlus.GdiGraphicsEngine(96.0f);
                Current.Encoder = new gView.GraphicsEngine.GdiPlus.GdiBitmapEncoding();
                break;
            case Engines.Skia:
                Current.Engine = new gView.GraphicsEngine.Skia.SkiaGraphicsEngine(96.0f);
                Current.Encoder = new gView.GraphicsEngine.Skia.SkiaBitmapEncoding();
                break;
            default:
                throw new NotSupportedException();
        }

        Platform.SystemInfo.DefaultFontName = Platform.SystemInfo.IsWindows
            ? new gView.GraphicsEngine.GdiPlus.GdiGraphicsEngine(96.0f).GetDefaultFontName()
            : Current.Engine.GetDefaultFontName();
    }
}
