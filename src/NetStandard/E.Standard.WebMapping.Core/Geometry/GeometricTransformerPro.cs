namespace E.Standard.WebMapping.Core.Geometry;

public sealed class GeometricTransformerPro : IGeometricTransformer2
{
    private int _from, _to;
    private string _fromProj4 = string.Empty, _toProj4 = string.Empty;
    private bool _isConfigured;

    public GeometricTransformerPro(ISpatialReferenceStore store, int from, int to)
        : this(store.SpatialReferences, from, to) { }

    public GeometricTransformerPro(SpatialReferenceCollection sRefCollection, int from, int to)
    {
        _from = from;
        _to = to;

        if (sRefCollection == null || from <= 0 || to <= 0)
        {
            return;
        }

        Init(sRefCollection.ById(from), sRefCollection.ById(to));
    }

    public GeometricTransformerPro(SpatialReference from, SpatialReference to)
    {
        _from = from?.Id ?? 0;
        _to = to?.Id ?? 0;

        Init(from, to);
    }

    private void Init(SpatialReference from, SpatialReference to)
    {
        if (from == null || to == null || from.Id == to.Id)
        {
            if (from != null && to != null)
            {
                _fromProj4 = from.Proj4;
                _toProj4 = to.Proj4;
                _isConfigured = true;
            }

            return;
        }

        this.Transformer = new GeometricTransformer();
        Transformer.FromSpatialReference(from);
        Transformer.ToSpatialReference(to);
        _fromProj4 = from.Proj4;
        _toProj4 = to.Proj4;
        _isConfigured = true;
    }

    private GeometricTransformer Transformer { get; set; }

    public bool ToIsProjective => this.Transformer != null ? this.Transformer.ToIsProjective : true;
    public bool FromIsProjective => this.Transformer != null ? this.Transformer.FromProjective : true;

    public string ToProj4 => _toProj4;
    public string FromProj4 => _fromProj4;

    #region IDisposable Member

    public void Dispose()
    {
        if (this.Transformer != null)
        {
            this.Transformer.Dispose();
            this.Transformer = null;
        }
    }

    #endregion

    #region IGeometricTransformer Member

    public void Transform(double[] x, double[] y)
    {
        if (Transformer != null)
        {
            Transformer.Transform(x, y);
        }
    }

    public void Transform(Shape shape)
    {
        Transform(shape, ShapeSrsProperties.SrsId);
    }

    public void InvTransform(Shape shape)
    {
        InvTransform(shape, ShapeSrsProperties.SrsId);
    }

    #endregion

    #region IGeometricTransformer2 Member

    public void Transform(Shape shape, ShapeSrsProperties setSrsProperties = ShapeSrsProperties.SrsId)
    {
        if (_isConfigured && shape != null)
        {
            Transformer?.Transform2D(shape, ShapeSrsProperties.None);

            if (setSrsProperties.HasFlag(ShapeSrsProperties.SrsId))
            {
                shape.SrsId = _to;
            }

            if (setSrsProperties.HasFlag(ShapeSrsProperties.SrsProj4Parameters))
            {
                shape.SrsP4Parameters = _toProj4;
            }
        }
    }

    public void InvTransform(Shape shape, ShapeSrsProperties setSrsProperties = ShapeSrsProperties.SrsId)
    {
        if (_isConfigured && shape != null)
        {
            Transformer?.InvTransform2D(shape, ShapeSrsProperties.None);

            if (setSrsProperties.HasFlag(ShapeSrsProperties.SrsId))
            {
                shape.SrsId = _from;
            }

            if (setSrsProperties.HasFlag(ShapeSrsProperties.SrsProj4Parameters))
            {
                shape.SrsP4Parameters = _fromProj4;
            }
        }
    }

    public int FromSrsId => _from;
    public int ToSrsId => _to;

    #endregion
}
