using E.Standard.CMS.Core.IO.Abstractions;
using System;
using System.Collections.Generic;

namespace E.Standard.CMS.Core;

class VersionInfoList : List<VersionInfo>, IPersistable
{
    public new void Add(VersionInfo vi)
    {
        foreach (VersionInfo v in this)
        {
            if (v.Equals(vi))
            {
                return;
            }
        }

        base.Add(vi);
    }

    public VersionInfo this[string assembly]
    {
        get
        {
            foreach (VersionInfo vi in this)
            {
                if (vi.AssemblyName.ToLower() == assembly.ToLower())
                {
                    return vi;
                }
            }
            return null;
        }
    }

    public int Major
    {
        get
        {
            return this.MaxVersion.AssemblyVersion.Major;
        }
    }

    public int Minor
    {
        get
        {
            return this.MaxVersion.AssemblyVersion.Minor;
        }
    }

    public VersionInfo MaxVersion
    {
        get
        {
            VersionInfo v = new VersionInfo();

            foreach (VersionInfo vi in this)
            {
                if (vi.AssemblyVersion > v.AssemblyVersion)
                {
                    v = vi;
                }
            }

            return v;
        }
    }

    #region IPersistable Member

    public void Load(IStreamDocument stream)
    {
        this.Clear();
        int counter = 0;
        while (true)
        {
            string name = (string)stream.Load("assembly" + counter, String.Empty);
            if (String.IsNullOrEmpty(name))
            {
                return;
            }

            string vers = (string)stream.Load("version" + counter, "1.0.0.0");

            this.Add(new VersionInfo(name, new Version(vers)));
            counter++;
        }
    }

    public void Save(IStreamDocument stream)
    {
        int counter = 0;
        foreach (VersionInfo vi in this)
        {
            stream.Save("assembly" + counter, vi.AssemblyName);
            stream.Save("version" + counter, vi.AssemblyVersion.ToString());
            counter++;
        }
    }

    #endregion

    public int CompareTo(VersionInfoList cand)
    {
        //  1 größer
        //  0 gleich
        // -1 kleiner

        if (cand == null)
        {
            return 0;
        }

        if (this.Count > cand.Count)
        {
            return 1;
        }

        if (this.Count < cand.Count)
        {
            return -1;
        }

        foreach (VersionInfo vi in this)
        {
            VersionInfo vi_cand = cand[vi.AssemblyName];
            if (vi_cand == null)
            {
                return 1;
            }

            int c = vi.AssemblyVersion.CompareTo(vi_cand.AssemblyVersion);
            if (c != 0)
            {
                return c;
            }
        }

        return 0;
    }
}
