using System;

namespace E.Standard.WebGIS.Tools.Export.Exeption;

internal class MapSeriesPrintToManyPagesExeption : Exception
{
    public MapSeriesPrintToManyPagesExeption(int iterations)
      => (Iterations) = (iterations);

    public int Iterations { get; }
}
