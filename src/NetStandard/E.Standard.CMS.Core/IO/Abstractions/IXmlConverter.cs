namespace E.Standard.CMS.Core.IO.Abstractions;

public interface IXmlConverter
{
    string ReadAllAsXmlString();
    bool WriteXmlData(string xml, bool overrideExisting = true);
}
