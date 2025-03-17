using E.Standard.CMS.Core;
using E.Standard.Combinatorial;
using E.Standard.Platform;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace E.Standard.WebGIS.CMS;

public class QueryBuilder
{
    private StringBuilder _sb = new StringBuilder();
    private Clause _current = null;

    public QueryBuilder(string op)
    {
        _current = new Clause(op, null);
    }

    public void StartBracket(string op)
    {
        Bracket c = new Bracket(op, _current);
        _current.AddClause(c);
        _current = c;
    }
    public void EndBracket()
    {
        Bracket c = (Bracket)_current;
        _current = _current.Parent;
        if (c._clauses.Count == 0 && String.IsNullOrEmpty(c.WhereClause))
        {
            _current._clauses.Remove(c);
        }
    }
    public void AppendClause(CmsNode searchItem, ILayer layer, string val)
    {
        if (searchItem == null)
        {
            return;
        }

        AppendClause(((string)searchItem.Load("fields", String.Empty)).Split(';'),
                     (bool)searchItem.Load("useupper", false),
                      (QueryMethod)searchItem.Load("method", QueryMethod.Exact),
                      layer, val);
    }

    public void AppendClause(string[] fieldnames, bool useUpper, QueryMethod method, ILayer layer, string val)
    {
        if (layer == null)
        {
            return;
        }

        StringBuilder where = new StringBuilder();
        bool first = true;
        foreach (string fieldName in fieldnames)
        {
            string fieldname = fieldName.Trim();
            if (String.IsNullOrEmpty(fieldname))
            {
                continue;
            }

            if (fieldname.StartsWith("("))
            {
                int pos = fieldname.IndexOf(")");
                fieldname = fieldname.Substring(pos + 1, fieldname.Length - pos - 1);
            }

            IField field = layer.Fields.FindField(fieldname);
            if (field == null)
            {
                continue;
            }

            string quote = String.Empty;
            if (field.Type == FieldType.String || field.Type == FieldType.Date || field.Type == FieldType.Char)
            {
                quote = "'";
            }

            if (field.Type == FieldType.Double ||
                field.Type == FieldType.Float)
            {
                val = val.Replace(",", ".");
                double d;
                if (!val.TryToPlatformDouble(out d))
                {
                    continue;
                }
            }
            if (field.Type == FieldType.SmallInteger ||
                field.Type == FieldType.Interger ||
                field.Type == FieldType.BigInteger)
            {
                long l;
                if (!long.TryParse(val, out l))
                {
                    continue;
                }
            }
            if (!first)
            {
                where.Append(" OR ");
            }

            first = false;

            if (useUpper && field.Type == FieldType.String)
            {
                val = val.ToUpper();
                where.Append("UPPER(" + fieldname + ")");
            }
            else
            {
                where.Append(fieldname);
            }
            if (val.EndsWith("#"))
            {
                method = QueryMethod.Exact;
                val = val.Substring(0, val.Length - 1);
            }
            if (String.IsNullOrEmpty(quote) &&
                (method == QueryMethod.BeginningWildcard ||
                 method == QueryMethod.BothWildcards ||
                 method == QueryMethod.EndingWildcard ||
                 method == QueryMethod.SpacesToWildcardWithBeginningAndEndingWildcard ||
                 method == QueryMethod.SpacesToWildcardWithBeginningWildcard ||
                 method == QueryMethod.SpacesToWildcardWithEndingWildcard))
            {
                method = QueryMethod.Exact;
            }

            switch (method)
            {
                case QueryMethod.Exact:
                    where.Append((val.Contains("%") ? " like " : "=") + quote + val + quote);
                    break;
                case QueryMethod.BeginningWildcard:
                    where.Append(" like '%" + val + "'");
                    break;
                case QueryMethod.BothWildcards:
                    where.Append(" like '%" + val + "%'");
                    break;
                case QueryMethod.EndingWildcard:
                    where.Append(" like '" + val + "%'");
                    break;
                case QueryMethod.ExactOrWildcards:
                    if (val.Contains("*") || val.Contains("%"))
                    {
                        where.Append(" like '" + val.Replace("*", "%") + "'");
                    }
                    else
                    {
                        where.Append("=" + quote + val + quote);
                    }

                    break;
                case QueryMethod.LowerThan:
                    where.Append("<" + quote + val + quote);
                    break;
                case QueryMethod.LowerOrEqualThan:
                    where.Append("<=" + quote + val + quote);
                    break;
                case QueryMethod.GreaterThan:
                    where.Append(">" + quote + val + quote);
                    break;
                case QueryMethod.GreaterOrEqualThan:
                    where.Append(">=" + quote + val + quote);
                    break;
                case QueryMethod.Not:
                    where.Append("<>" + quote + val + quote);
                    break;
                case QueryMethod.In:
                    where.Append(" in(");
                    where.Append(String.Join(",", val.Split(',').Select(v => quote + v.Trim() + quote)));
                    where.Append(")");
                    break;
                case QueryMethod.NotIn:
                    where.Append(" not in(");
                    where.Append(String.Join(",", val.Split(',').Select(v => quote + v.Trim() + quote)));
                    where.Append(")");
                    break;
                case QueryMethod.SpacesToWildcard:
                    where.Append(" like '" + val.Replace("*", "%").Replace(" ", "%") + "'");
                    break;
                case QueryMethod.SpacesToWildcardWithEndingWildcard:
                    where.Append(" like '" + val.Replace("*", "%").Replace(" ", "%") + "%'");
                    break;
                case QueryMethod.SpacesToWildcardWithBeginningWildcard:
                    where.Append(" like '%" + val.Replace("*", "%").Replace(" ", "%") + "'");
                    break;
                case QueryMethod.SpacesToWildcardWithBeginningAndEndingWildcard:
                    where.Append(" like '%" + val.Replace("*", "%").Replace(" ", "%") + "%'");
                    break;
            }

            _current.AppendWhere(where.ToString());
        }
    }

    private void AddClause(Clause c)
    {
        _current.AddClause(c);
    }

    public override string ToString()
    {
        return _current.ToString();
    }

    public QueryBuilder CalcCombinations(string op)
    {
        if (_current._clauses.Count <= 2)
        {
            return this;
        }

        QueryBuilder qbuilder = new QueryBuilder(op);

        int[] intArray = new int[_current._clauses.Count];
        for (int i = 0; i < _current._clauses.Count; i++)
        {
            intArray[i] = i;
        }

        for (int l = _current._clauses.Count; l > 1; l--)
        {
            Combinations combs = new Combinations(intArray, l);
            while (combs.MoveNext())
            {
                Array thisCombs = (Array)combs.Current;
                qbuilder.StartBracket("AND");
                for (int i = 0; i < thisCombs.Length; i++)
                {
                    qbuilder.AddClause(_current._clauses[Convert.ToInt32(thisCombs.GetValue(i))].Clone(qbuilder._current));
                }
                qbuilder.EndBracket();
            }
        }

        return qbuilder;
    }

    public List<QueryBuilder> CalcCombinations2(string op)
    {
        if (_current._clauses.Count <= 2)
        {
            List<QueryBuilder> l = new List<QueryBuilder>();
            l.Add(this);
            return l;
        }

        int[] intArray = new int[_current._clauses.Count];
        for (int i = 0; i < _current._clauses.Count; i++)
        {
            intArray[i] = i;
        }

        List<QueryBuilder> qbuilders = new List<QueryBuilder>();
        for (int l = _current._clauses.Count; l > 1; l--)
        {
            QueryBuilder qbuilder = new QueryBuilder(op);

            Combinations combs = new Combinations(intArray, l);
            while (combs.MoveNext())
            {
                Array thisCombs = (Array)combs.Current;
                qbuilder.StartBracket("AND");
                for (int i = 0; i < thisCombs.Length; i++)
                {
                    qbuilder.AddClause(_current._clauses[Convert.ToInt32(thisCombs.GetValue(i))].Clone(qbuilder._current));
                }
                qbuilder.EndBracket();
            }
            qbuilders.Add(qbuilder);
        }
        return qbuilders;
    }

    #region Classes
    private class Clause
    {
        internal List<Clause> _clauses = new List<Clause>();
        protected string _op, _clause = String.Empty;
        protected Clause _parent;

        public Clause(string op, Clause parent)
        {
            _op = op;
            _parent = parent;
        }

        public void AppendWhere(string where)
        {
            if (String.IsNullOrEmpty(where))
            {
                return;
            }

            Clause w = new Clause(String.Empty, this);
            w.WhereClause = where;
            AddClause(w);
        }
        public string WhereClause
        {
            get { return _clause; }
            set { _clause = value; }
        }
        public void AddClause(Clause c)
        {
            if (c != null)
            {
                _clauses.Add(c);
            }
        }
        public Clause Parent
        {
            get { return _parent; }
        }

        public override string ToString()
        {
            if (_clauses.Count == 0)
            {
                return this.WhereClause;
            }

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < _clauses.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(" " + _op + " ");
                }

                sb.Append(_clauses[i].ToString());
            }

            return sb.ToString();
        }

        public virtual Clause Clone(Clause parent)
        {
            Clause clone = new Clause(_op, parent);
            clone._clause = _clause;

            foreach (Clause c in _clauses)
            {
                clone._clauses.Add(c.Clone(clone));
            }

            return clone;
        }
    }
    private class Bracket : Clause
    {
        public Bracket(string op, Clause parent)
            : base(op, parent)
        {
        }

        public override string ToString()
        {
            string c = base.ToString();

            if (c.Length == 0)
            {
                return String.Empty;
            }

            return "(" + c + ")";
        }

        public override Clause Clone(Clause parent)
        {
            Clause clone = new Bracket(_op, parent);
            clone.WhereClause = this.WhereClause;

            foreach (Clause c in _clauses)
            {
                clone._clauses.Add(c.Clone(clone));
            }

            return clone;
        }
    }
    #endregion
}
