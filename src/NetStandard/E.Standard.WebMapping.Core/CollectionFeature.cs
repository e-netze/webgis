using E.Standard.WebMapping.Core.Collections;
using System;
using System.Collections.Generic;
using System.Text;

namespace E.Standard.WebMapping.Core;

public class CollectionFeature : Feature
{
    private readonly FeatureCollection _featureColl;
    private readonly char _seperator;

    public CollectionFeature(FeatureCollection featureColl, char seperator)
        : base()
    {
        _featureColl = featureColl;
        _seperator = seperator;
    }

    override public string this[string AttributName]
    {
        get
        {
            if (_featureColl == null)
            {
                return String.Empty;
            }

            StringBuilder sb = new StringBuilder();

            if (AttributName.Contains(","))
            {
                string[] AttributNames = AttributName.Split(',');
                if (AttributNames.Length <= 3)  // für unsere GDB Seite: KG|GNR|GNR|...;KG|GNR|GNR|GNR|...,... Distinct nach KG und danach auflistung der Grundstücke....
                {
                    char groupSep = ',';
                    if (AttributNames.Length == 3)
                    {
                        groupSep = AttributNames[AttributNames.Length - 1][0];
                        Array.Resize<string>(ref AttributNames, AttributNames.Length - 1);
                    }

                    FeatureCollection coll = Distinct(_featureColl, AttributNames[0], groupSep);

                    foreach (CollectionFeature collFeat in coll)
                    {
                        if (sb.Length > 0)
                        {
                            sb.Append(_seperator);
                        }

                        sb.Append(collFeat._featureColl[0][AttributNames[0]]);
                        sb.Append(groupSep);
                        sb.Append(collFeat[AttributNames[1]]);
                    }
                }
            }
            else
            {
                foreach (Feature feature in _featureColl)
                {
                    string val = feature[AttributName];
                    if (String.IsNullOrEmpty(val))
                    {
                        continue;
                    }

                    if (sb.Length > 0)
                    {
                        sb.Append(_seperator);
                    }

                    sb.Append(val);
                }
            }
            return sb.ToString();
        }
    }

    private FeatureCollection Distinct(FeatureCollection features, string fieldName, char seperator)
    {
        FeatureCollection coll = new FeatureCollection();

        List<string> vals = new List<string>();
        foreach (Feature feature in features)
        {
            string val = feature[fieldName];

            if (!vals.Contains(val))
            {
                vals.Add(val);
                CollectionFeature collFeature = new CollectionFeature(new FeatureCollection(), seperator);
                collFeature._featureColl.Add(feature);
                coll.Add(collFeature);
            }
            else
            {
                foreach (CollectionFeature collFeature in coll)
                {
                    if (collFeature._featureColl[0][fieldName] == val)
                    {
                        collFeature._featureColl.Add(feature);
                        break;
                    }
                }
            }
        }

        return coll;
    }
}
