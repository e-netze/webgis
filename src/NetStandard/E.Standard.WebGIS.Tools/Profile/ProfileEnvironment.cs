using E.Standard.WebMapping.Core.Api.Bridge;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace E.Standard.WebGIS.Tools.Profile;

class ProfileEnvironment
{
    internal const string CmsConfigParameter = "verticalalignmentconfig";

    private string[] _configs = null;

    public ProfileEnvironment(IBridge bridge)
    {
        this.ProfileRootPath = $"{bridge.AppEtcPath}/profiles/";
        this.AppConfigPath = bridge.AppConfigPath;

        _configs = bridge.ToolConfigValues<string>(CmsConfigParameter);
    }

    #region Properties

    public string ProfileRootPath { get; private set; }
    public string AppConfigPath { get; private set; }

    public Profile[] Profiles
    {
        get
        {
            List<Profile> profiles = new List<Profile>();

            foreach (string config in _configs)
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(this.ProfileRootPath + config);

                foreach (XmlNode node in doc.SelectNodes("profiles/profile[@name]"))
                {
                    profiles.Add(new Profile(this, node));
                }
            }

            return profiles.ToArray();
        }
    }

    public Profile this[string name]
    {
        get
        {
            foreach (string config in _configs)
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(this.ProfileRootPath + config);

                XmlNode node = doc.SelectSingleNode(($"profiles/profile[@name='{name}']"));
                if (node != null)
                {
                    return new Profile(this, node);
                }
            }
            return null;
        }
    }

    #endregion

    #region Classes

    public class Profile
    {
        private static NumberFormatInfo _nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;

        private ProfileEnvironment _profileEnvironment;
        private XmlNode _node;

        public Profile(ProfileEnvironment profileEnvironment, XmlNode node)
        {
            _profileEnvironment = profileEnvironment;
            _node = node;
        }

        #region Properties

        public string Name { get { return _node.Attributes["name"].Value; } }
        public int SrsId { get { return GetConfigInt("srs", 4326); } }
        public string Scale { get { return GetConfigString("scale", "1:100"); } }
        public double VertexIncrement { get { return GetConfigDouble("vertexincrement", 0D); } }
        public double VertexLimit { get { return GetConfigDouble("vertexlimit", 1000); } }
        public double MinumHeight { get { return GetConfigDouble("minheight"); } }
        public double SuperElevation { get { return GetConfigDouble("superelevation", 1D); } }
        public double VerticalLineStep { get { return GetConfigDouble("v_linestep", 0D); } }
        public double HorizontalLineStep { get { return GetConfigDouble("h_linestep", 0D); } }
        public bool ShowHeightBar { get { return GetConfigBool("show_heightbar"); } }
        public bool ShowStatBar { get { return GetConfigBool("show_statbar"); } }
        public bool ShowVerticesHeightBar { get { return GetConfigBool("show_verticesheightbar"); } }
        public bool ShowVerticesStatBar { get { return GetConfigBool("show_verticesstatbar"); } }
        public bool ShowPoints { get { return GetConfigBool("show_points", true); } }
        public bool UseRasterInfoPro { get { return GetConfigBool("rasterinfopro"); } }
        public string[] EarthColor { get { return GetConfigStringArray("earthcolor"); } }
        public string[] Server { get { return GetConfigStringArray("server"); } }
        public string[] Service { get { return GetConfigStringArray("service"); } }
        public string RasterTheme { get { return GetConfigString("rastertheme"); } }
        public int ResultIndex { get { return GetConfigInt("result_index", 0); } }
        public string ServiceUser { get { return GetConfigString("user"); } }
        public string ServicePassword { get { return GetConfigString("pwd"); } }
        public string ServiceType { get { return GetConfigString("service_type", "ims").ToLower(); } }
        public string ArcInfoGrid { get { return GetConfigString("aigrid"); } }
        public string Layout { get { return GetConfigString("layout"); } }


        #endregion

        #region Helper

        private string GetConfigString(string name, string defaultValue = "")
        {
            if (_node != null && _node.Attributes[name] != null)
            {
                return _node.Attributes[name].Value;
            }

            return defaultValue;
        }

        private string[] GetConfigStringArray(string name)
        {
            if (_node != null && _node.Attributes[name] != null)
            {
                return _node.Attributes[name].Value.Split(';');
            }

            return new string[] { };
        }

        private int GetConfigInt(string name, int defaultValue = 0)
        {
            string val = GetConfigString(name);
            if (!String.IsNullOrWhiteSpace(val))
            {
                return int.Parse(val);
            }

            return defaultValue;
        }

        private double GetConfigDouble(string name, double defaultValue = 0D)
        {
            string val = GetConfigString(name);
            if (!String.IsNullOrWhiteSpace(val))
            {
                return double.Parse(val.Replace(",", "."), _nhi);
            }

            return defaultValue;
        }

        private bool GetConfigBool(string name, bool defaultValue = false)
        {
            string val = GetConfigString(name);
            if (!String.IsNullOrWhiteSpace(val))
            {
                return val.ToLower() == "true";
            }

            return defaultValue;
        }

        public List<string> EarthColorHex
        {
            get
            {
                List<string> hexColors = new List<string>();
                //string ret = this.EarthColor;
                string[] colors = this.EarthColor;
                string hexColor;
                foreach (string color in colors)
                {
                    hexColor = String.Empty;
                    if (color.Contains(","))
                    {
                        string[] rgb = color.Split(',');
                        if (rgb.Length >= 3)
                        {
                            hexColor = "#" + int.Parse(rgb[0]).ToString("X2") + int.Parse(rgb[1]).ToString("X2") + int.Parse(rgb[2]).ToString("X2");
                            hexColors.Add(hexColor);
                        }
                    }
                }
                return hexColors;
            }
        }

        #endregion
    }

    #endregion
}
