namespace E.Standard.WebMapping.Core.Abstraction;

public interface IQueryValueConverter
{
    string ConvertQueryValue(IField field, string value);
}
