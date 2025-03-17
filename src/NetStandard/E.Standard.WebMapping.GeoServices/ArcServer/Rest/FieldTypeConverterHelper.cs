using E.Standard.WebMapping.Core;

namespace E.Standard.WebMapping.GeoServices.ArcServer;

class FieldTypeConverterHelper
{
    public static FieldType FType(string parsedFieldTypeString)
    {
        switch (parsedFieldTypeString)
        {
            case "esriFieldTypeBlob":
                return FieldType.Unknown;
            case "esriFieldTypeDate":
                return FieldType.Date;
            case "esriFieldTypeDouble":
                return FieldType.Double;
            case "esriFieldTypeGeometry":
                return FieldType.Shape;
            case "esriFieldTypeGlobalID":
                return FieldType.GlobalId;
            case "esriFieldTypeGUID":
                return FieldType.GUID;
            case "esriFieldTypeInteger":
                return FieldType.Interger;
            case "esriFieldTypeOID":
                return FieldType.ID;
            case "esriFieldTypeRaster":
                return FieldType.Unknown;
            case "esriFieldTypeSingle":
                return FieldType.Float;
            case "esriFieldTypeSmallInteger":
                return FieldType.SmallInteger;
            case "esriFieldTypeString":
                return FieldType.String;
            case "esriFieldTypeXML":
                return FieldType.Unknown;
        }
        return FieldType.Unknown;
    }
}
