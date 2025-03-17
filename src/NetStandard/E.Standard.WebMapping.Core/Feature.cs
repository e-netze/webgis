using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace E.Standard.WebMapping.Core;


public class Feature
{
    private Shape _shape;
    private Envelope _zoomEnvelope;
    private AttributesCollection _attributes, _dragAttributes;
    private bool _checked = true;
    private int _oid = -1;
    private string _globalOid = null;

    public Feature()
    {
        _shape = null;
        _zoomEnvelope = null;
        _attributes = new AttributesCollection();
    }

    public Feature(IEnumerable<Attribute> attributes)
        : this()
    {
        if (attributes != null)
        {
            foreach (var a in attributes)
            {
                _attributes.Add(a);
            }
        }
    }

    public int Oid
    {
        get { return _oid; }
        set { _oid = value; }
    }

    public Shape Shape
    {
        get { return _shape; }
        set { _shape = value; }
    }

    public Envelope ZoomEnvelope
    {
        get { return _zoomEnvelope; }
        set { _zoomEnvelope = value; }
    }

    public Shape HoverShape
    {
        get; set;
    }

    public string GlobalOid
    {
        get { return _globalOid; }
        set { _globalOid = value; }
    }

    public AttributesCollection Attributes
    {
        get { return _attributes; }
        set { _attributes = value; }
    }
    public AttributesCollection DragAttributes
    {
        get
        {
            if (_dragAttributes == null)
            {
                _dragAttributes = new AttributesCollection();
            }

            return _dragAttributes;
        }
    }

    public bool HasDragAttributes
    {
        get { return _dragAttributes != null; }
    }

    virtual public string this[string AttributeName]
    {
        get
        {
            if (AttributeName == "GML:ID" && this.Oid > 0)
            {
                return this.Oid.ToString();
            }

            Attribute attribute = Attributes[AttributeName];
            if (attribute != null)
            {
                return attribute.Value;
            }

            if (AttributeName.Contains(","))
            {
                string[] AttributeNames = AttributeName.Split(',');
                if (AttributeNames.Length <= 3)  // Felder zusammenfassen (unser GDB Seite): [KG,GN,|] => KG|GNR 
                {
                    char groupSep = ',';
                    if (AttributeNames.Length == 3)
                    {
                        groupSep = AttributeNames[AttributeNames.Length - 1][0];
                        Array.Resize<string>(ref AttributeNames, AttributeNames.Length - 1);
                    }

                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < AttributeNames.Length; i++)
                    {
                        attribute = Attributes[AttributeNames[i]];
                        if (attribute == null)
                        {
                            continue;
                        }

                        if (sb.Length > 0)
                        {
                            sb.Append(groupSep);
                        }

                        sb.Append(attribute.Value);
                    }
                    return sb.ToString();
                }
            }

            if (AttributeName.ToLower() == "shape.len")
            {
                attribute = Attributes.GetCaseInsensitiv(AttributeName);
                if (attribute != null)
                {
                    return attribute.Value;
                }
            }

            return String.Empty;
        }
        set
        {
            Attribute attribute = Attributes[AttributeName];
            if (attribute != null)
            {
                attribute.Value = value;
            }
        }
    }

    public bool Checked
    {
        get { return _checked; }
        set { _checked = value; }
    }

    public Feature Clone(bool cloneShape = false)
    {
        if (cloneShape == true)
        {
            throw new Exception("Clone Shape is not implemented");
        }

        var clone = new Feature();

        clone._shape = _shape;
        clone._zoomEnvelope = _zoomEnvelope;
        clone._checked = _checked;
        clone._oid = _oid;
        clone._globalOid = _globalOid;

        foreach (var attribute in _attributes)
        {
            clone._attributes.Add(new Attribute(attribute.Name, attribute.Value));
        }

        return clone;
    }
}
