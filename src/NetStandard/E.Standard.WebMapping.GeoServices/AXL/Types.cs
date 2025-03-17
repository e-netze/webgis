using System;
using System.Xml;

namespace E.Standard.WebMapping.GeoServices.AXL;

public class Types
{
    static public bool IsNumber(string val)
    {
        try
        {
            foreach (string val_ in val.Split(';'))
            {
                string v = val_;
                double test = Convert.ToDouble(v.Replace(".", ","));
            }
        }
        catch
        {
            return false;
        }
        return true;
    }
    static public bool isHNR(string val)
    {
        if (val.Length == 0)
        {
            return false;
        }

        if (IsNumber(val))
        {
            return true;
        }

        if (!IsNumber(val.Substring(0, val.Length - 1)))
        {
            return false;
        }

        return true;
    }
    static public void splitHNR(string hnr, out string hnr1, out string hnr2)
    {
        if (!IsNumber(hnr))
        {
            hnr1 = hnr.Substring(0, hnr.Length - 1);
            hnr2 = hnr.Substring(hnr.Length - 1, 1);
        }
        else
        {
            hnr1 = hnr;
            hnr2 = "";
        }
    }
    static public int hex2int(string val)
    {
        int ret = 0;
        for (int i = val.Length - 1, p = 0; i >= 0; i--, p++)
        {
            int z = 0;
            val = val.ToLower();
            switch (val[i])
            {
                case '1': z = 1; break;
                case '2': z = 2; break;
                case '3': z = 3; break;
                case '4': z = 4; break;
                case '5': z = 5; break;
                case '6': z = 6; break;
                case '7': z = 7; break;
                case '8': z = 8; break;
                case '9': z = 9; break;
                case 'a': z = 10; break;
                case 'b': z = 11; break;
                case 'c': z = 12; break;
                case 'd': z = 13; break;
                case 'e': z = 14; break;
                case 'f': z = 15; break;
            }
            ret += z * (int)Math.Pow(16, p);
        }
        return ret;
    }
    static public string hex2RGBString(string val)
    {
        try
        {
            int r = hex2int(val.Substring(0, 2)),
                g = hex2int(val.Substring(2, 2)),
                b = hex2int(val.Substring(4, 2));

            return r.ToString() + "," + g.ToString() + "," + b.ToString();
        }
        catch
        {
            return "255,0,0";
        }
    }
    static public string Umlaute2Esri(string val)
    {
        val = val.Replace("ä", "&#228;");
        val = val.Replace("ö", "&#246;");
        val = val.Replace("ü", "&#252;");
        val = val.Replace("Ä", "&#196;");
        val = val.Replace("Ö", "&#214;");
        val = val.Replace("Ü", "&#220;");
        val = val.Replace("ß", "&#223;");

        return val;
    }
    static public string getFieldValue(XmlNode feature, string name)
    {
        return Globals.getFieldValue(feature, name);
        /*
        XmlNodeList fields=feature.SelectNodes("FIELDS/FIELD");
        name=name.ToUpper();
        foreach(XmlNode field in fields) 
        {
            string fieldname=field.Attributes["name"].Value.ToString().ToUpper();
            int index=fieldname.IndexOf("."+name);
            int pos=fieldname.Length-name.Length-1;
            if(fieldname==name ||
                (index!=-1 && index==pos)) 
            {
                string val=field.Attributes["value"].Value.ToString();
                return val;
            }
        }
        return "";
        */
    }
    static public XmlNode getField(XmlNode feature, string name)
    {
        XmlNodeList fields = feature.SelectNodes("FIELDS/FIELD");
        name = name.ToUpper();
        foreach (XmlNode field in fields)
        {
            string fieldname = field.Attributes["name"].Value.ToString().ToUpper();
            int index = fieldname.IndexOf("." + name);
            int pos = fieldname.Length - name.Length - 1;
            if (fieldname == name ||
                (index != -1 && index == pos))
            {
                return field;
            }
        }
        return null;
    }
}
