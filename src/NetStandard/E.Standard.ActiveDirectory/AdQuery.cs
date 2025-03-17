#pragma warning disable CA1416  // suppress only supports on windows warning

using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;

namespace E.Standard.ActiveDirectory;

public class AdQuery : IAdQuery
{
    private string? _dea;

    public void Initialize(string dea)
    {
        _dea = dea;
    }

    public IEnumerable<AdObject> FindAdObjects(string filter)
    {
        if (String.IsNullOrEmpty(_dea) || filter == "**")
        {
            return new AdObject[0];
        }

        while (filter.Contains("**"))
        {
            filter = filter.Replace("**", "*");
        }

        if (filter.Contains(@"\"))
        {
            filter = filter.Split('\\')[1];
        }

        if (filter.StartsWith("@@"))
        {
            filter = filter.Substring(2, filter.Length - 2);
        }

        try
        {
            using (DirectoryEntry o = new DirectoryEntry(_dea))
            {
                using (var searcher = new DirectorySearcher(o))
                {
                    searcher.SearchScope = SearchScope.Subtree;
                    searcher.Filter = $"(&(sAMAccountName={EscapeFilter(filter)}))";

                    List<AdObject> ret = new List<AdObject>();
                    foreach (System.DirectoryServices.SearchResult result in searcher.FindAll())
                    {
                        if (IsUser(result))
                        {
                            ret.Add(new AdUser(result.Properties["sAMAccountName"][0].ToString()!, result.Properties["Name"][0].ToString()!));
                            if (result.Properties["mail"] != null && result.Properties["mail"].Count > 0)
                            {
                                ((AdUser)ret[ret.Count - 1]).Email = result.Properties["mail"][0].ToString()!;
                            }
                        }
                        else if (IsGroup(result))
                        {
                            ret.Add(new AdGroup(result.Properties["sAMAccountName"][0].ToString()!, result.Properties["Name"][0].ToString()!));
                        }
                    }
                    return ret.ToArray();
                }
            }
        }
        catch
        {
            return new AdObject[0];
        }
    }

    public string[] UserRoles(string username, bool recursive)
    {
        List<string> roles = new List<string>();

        UserRoles(username, recursive, roles);

        return roles.ToArray();
    }

    #region Helper

    private bool IsUser(System.DirectoryServices.SearchResult result)
    {
        for (int i = 0, to = result.Properties["objectClass"].Count; i < to; i++)
        {
            if (result.Properties["objectClass"][i].ToString() == "user")
            {
                return true;
            }
        }
        return false;
    }

    private bool IsGroup(System.DirectoryServices.SearchResult result)
    {
        for (int i = 0, to = result.Properties["objectClass"].Count; i < to; i++)
        {
            if (result.Properties["objectClass"][i].ToString() == "group")
            {
                return true;
            }
        }
        return false;
    }

    private void UserRoles(string name, bool recursive, List<string> rolesBag)
    {
        if (_dea is null)
        {
            return;
        }

        string dea = _dea;

        using (DirectoryEntry obEntry = new DirectoryEntry(dea))
        using (DirectorySearcher srch = new DirectorySearcher(obEntry, "(sAMAccountName=" + name + ")"))
        {
            SearchResult? res = srch.FindOne();

            List<string> roles = new List<string>();
            if (null != res)
            {
                int countGroups = res.Properties["memberOf"].Count;

                for (int i = 0; i < countGroups; i++)
                {
                    string dn = (String)res.Properties["memberOf"][i];

                    //int equalsIndex = dn.IndexOf("=", 1);
                    //int commaIndex = dn.IndexOf(",", 1);
                    //if (-1 == equalsIndex)
                    //{
                    //    break;
                    //}

                    //string role = (dn.Substring((equalsIndex + 1),
                    //              (commaIndex - equalsIndex) - 1));

                    foreach (string v in SplitResultString(dn))
                    {
                        if (v.ToLower().StartsWith("cn="))
                        {
                            string role = v.Substring(3);

                            // Sollte schon in der SplitResultString funktion abgefangen werden
                            //if (role.StartsWith("\\"))   // \\>  if roles start with > they came with \> !!
                            //{
                            //    role = role.Substring(1);
                            //}

                            if (!roles.Contains(role))
                            {
                                roles.Add(role);
                            }
                        }
                    }
                }
            }
            if (recursive)
            {
                SubRoles(roles.ToArray(), true, rolesBag);
            }
            else
            {
                roles.AddRange(roles);
            }
        }
    }

    private void SubRoles(string[] names, bool recursive, List<string> rolesBag)
    {
        try
        {
            var queryNames = names.Where(n => !rolesBag.Contains(n))
                .Select(n =>
                {
                    //n = n.Contains(@"\>") ? n.Replace(@"\>", ">") : n;

                    // https://stackoverflow.com/questions/649149/how-to-escape-a-string-in-c-for-use-in-an-ldap-query
                    // https://naveenr.net/unicode-character-set-and-utf-8-utf-16-utf-32-encoding/

                    n = n.Contains(@"\") ? n.Replace(@"\", @"\5c") : n;
                    n = n.Contains(@"/") ? n.Replace(@"/", @"\2f") : n;

                    n = n.Contains(@"*") ? n.Replace(@"*", @"\2a") : n;
                    n = n.Contains(@"+") ? n.Replace(@"+", @"\2b") : n;

                    n = n.Contains(@"(") ? n.Replace(@"(", @"\28") : n;
                    n = n.Contains(@")") ? n.Replace(@")", @"\29") : n;

                    n = n.Contains(@">") ? n.Replace(@">", @"\3e") : n;
                    n = n.Contains(@"<") ? n.Replace(@"<", @"\3c") : n;

                    n = n.Contains(@"{") ? n.Replace(@"{", @"\7b") : n;
                    n = n.Contains(@"|") ? n.Replace(@"|", @"\7c") : n;
                    n = n.Contains(@"}") ? n.Replace(@"}", @"\7d") : n;

                    n = n.Contains(@",") ? n.Replace(@",", @"\2c") : n;
                    n = n.Contains(@";") ? n.Replace(@";", @"\3b") : n;

                    n = n.Contains("\"") ? n.Replace("\"", @"\22") : n;

                    n = n.Contains("\0") ? n.Replace("\0", @"\00") : n;
                    //n = n.Contains("/") ? n.Replace("/", @"\2f") : n;

                    return n;
                })
                .ToArray();

            rolesBag.AddRange(names.Where(n => !rolesBag.Contains(n)));

            if (queryNames.Length == 0 || _dea == null)
            {
                return;
            }

            string dea = _dea;

            string query = "(&(ObjectClass=Group)(|" + String.Concat(
                    queryNames.Select(n => "(CN=" + n + ")").ToArray()) +
                    "))";

            using (DirectoryEntry obEntry = new DirectoryEntry(dea))
            using (DirectorySearcher srch = new DirectorySearcher(obEntry, query))
            using (SearchResultCollection results = srch.FindAll())
            {
                //if (res == null && name.StartsWith(@"\>"))
                //{
                //    name = name.Replace(@"\>", ">");
                //    UserRoles2(name, recursive, roles);
                //}
                if (null != results)
                {
                    List<string> roles = new List<string>();

                    foreach (SearchResult result in results)
                    {
                        foreach (object val in result.Properties["memberof"])
                        {
                            if (val is null)
                            {
                                continue;
                            }

                            foreach (string v in SplitResultString(val.ToString()!) /*val.ToString().Split(',')*/)
                            {
                                if (v.ToLower().StartsWith("cn="))
                                {
                                    string groupCN = v.Substring(3);

                                    // Sollte schon in der SplitResultString funktion abgefangen werden
                                    //if (groupCN.StartsWith("\\"))   // \\> if roles start with > they came with \> !!
                                    //{
                                    //    groupCN = groupCN.Substring(1);
                                    //}

                                    if (!rolesBag.Contains(groupCN))
                                    {
                                        roles.Add(groupCN);
                                    }
                                    break;
                                }
                            }
                        }

                        AddNamesAndAccountNames(result, rolesBag);
                    }

                    if (recursive)
                    {
                        SubRoles(roles.ToArray(), recursive, rolesBag);
                    }
                    else
                    {
                        rolesBag.AddRange(roles);
                    }
                }
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    private void AddNamesAndAccountNames(SearchResult res, List<string> roles)
    {
        if (res == null)
        {
            return;
        }

        ResultPropertyValueCollection accountNames = res.Properties["sAMAccountName"];
        ResultPropertyValueCollection names = res.Properties["Name"];

        if (accountNames != null)
        {
            foreach (object accountName in accountNames)
            {
                if (accountName is string)
                {
                    if (!roles.Contains((string)accountName))
                    {
                        roles.Add((string)accountName);
                    }
                }
            }
        }
        if (names != null)
        {
            foreach (object name in names)
            {
                if (name is string)
                {
                    if (!roles.Contains((string)name))
                    {
                        roles.Add((string)name);
                    }
                }
            }
        }
    }

    #region Helper

    private string[] SplitResultString(string val)
    {
        // Beispiele
        // 
        // CN=Graz\, Neuholdaugasse,OU=SLX,OU=Gruppen,DC=domain,DC=at
        // CN=\> SAP-User TXX,OU=Verteile,OU=Gruppen,DC=domain,DC=at
        //
        // Weitere Zeichen, die mit \ escapted werden: https://ldapwiki.com/wiki/Best%20Practices%20For%20LDAP%20Naming%20Attributes
        // ,+"\<>;

        if (val == null)
        {
            return new string[0];
        }

        if (val.Contains("\\"))
        {
            val = val.Contains(@"\,") ? val.Replace(@"\,", @"\2c") : val;

            var result = val.Split(',')
                 .Select(v =>
                 {
                     v = v.Replace(@"\2c", @"\,");

                     return ParseResultString(v);
                 })
                 .Where(v => !string.IsNullOrEmpty(v))
                 .ToArray();

            return result;
        }
        else
        {
            return val.Split(',');
        }
    }

    private string ParseResultString(string val)
    {
        //
        // Zeichen, die mit \ escapted werden: https://ldapwiki.com/wiki/Best%20Practices%20For%20LDAP%20Naming%20Attributes
        // ,+"\<>;

        if (val != null)
        {
            if (val.Contains("\\"))
            {
                foreach (char c in @",+""\<>;")
                {
                    val = val.Replace(@"\" + c, c.ToString());
                }
            }
        }

        return val ?? string.Empty;
    }

    private string EscapeFilter(string filter)
    {
        foreach (char c in @",+""\<>;")
        {
            filter = filter.Replace(c.ToString(), @"\" + c.ToString());
        }

        return filter;
    }

    #endregion

    #endregion
}
