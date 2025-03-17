using E.Standard.Platform;
using E.Standard.WebMapping.Core.Collections;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace E.Standard.WebMapping.Core.Miscellaneous;

static public class Extentions
{
    static public void Ranking(this FeatureCollection features, string searchText)
    {
        #region Ranking

        SimilarityTool stool = new SimilarityTool();
        foreach (Feature feature in features)
        {
            if (String.IsNullOrEmpty(feature["_rank"]))
            {
                feature.Attributes.Add(new Core.Attribute("_rank", stool.CompareStrings(searchText, feature["_fulltext"]).ToString().Replace(",", ".")));
            }
            else
            {
                feature["_rank"] = stool.CompareStrings(searchText, feature["_fulltext"]).ToString().Replace(",", ".");
            }
        }
        features.Sort(new RankCompare());

        #endregion
    }
}

class SimilarityTool
{
    public double CompareStrings(string str1, string str2)
    {
        List<string> pairs1 = WordLetterPairs(str1.ToUpper());
        List<string> pairs2 = WordLetterPairs(str2.ToUpper());

        int intersection = 0;
        int union = pairs1.Count + pairs2.Count;

        for (int i = 0; i < pairs1.Count; i++)
        {
            for (int j = 0; j < pairs2.Count; j++)
            {
                if (pairs1[i] == pairs2[j])
                {
                    intersection++;
                    pairs2.RemoveAt(j);//Must remove the match to prevent "GGGG" from appearing to match "GG" with 100% success

                    break;
                }
            }
        }

        return (2.0 * intersection) / union;
    }

    private List<string> WordLetterPairs(string str)
    {
        List<string> AllPairs = new List<string>();

        // Tokenize the string and put the tokens/words into an array
        string[] Words = Regex.Split(str, @"\s");

        // For each word
        for (int w = 0; w < Words.Length; w++)
        {
            if (!string.IsNullOrEmpty(Words[w]))
            {
                // Find the pairs of characters
                String[] PairsInWord = LetterPairs(Words[w]);

                for (int p = 0; p < PairsInWord.Length; p++)
                {
                    AllPairs.Add(PairsInWord[p]);
                }
            }
        }

        return AllPairs;
    }

    private string[] LetterPairs(string str)
    {
        int numPairs = str.Length - 1;

        string[] pairs = new string[numPairs];

        for (int i = 0; i < numPairs; i++)
        {
            pairs[i] = str.Substring(i, 2);
        }

        return pairs;
    }
}

class RankCompare : IComparer<Feature>
{
    //static NumberFormatInfo Nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;

    #region IComparer<Feature> Member

    public int Compare(Feature x, Feature y)
    {
        //if (!String.IsNullOrEmpty(x["_level"]) && !String.IsNullOrEmpty(y["_level"]))
        //{
        //    int lx = int.Parse(x["_level"]);
        //    int ly = int.Parse(y["_level"]);
        //    if (lx < ly) return -1;
        //    if (lx > ly) return 1;
        //}
        double rx = x["_rank"].ToPlatformDouble();
        double ry = y["_rank"].ToPlatformDouble();

        if (rx > ry)
        {
            return -1;
        }

        if (ry < rx)
        {
            return 1;
        }

        if (x["_fulltext"] != null && y["_fulltext"] != null)
        {
            return x["_fulltext"].CompareTo(y["_fulltext"]);
        }

        return 0;
    }

    #endregion
}
