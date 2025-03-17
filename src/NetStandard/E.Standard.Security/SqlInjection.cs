using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace E.Standard.Security
{
    [Obsolete("Use E.Standard.Security.Core assembly")]
    static public class SqlInjection
    {

        static public void ParseBlackList(string term)
        {
            term = term.Trim();
            foreach (char c in "<>='\"?&*".ToCharArray())
                if (term.Contains(c.ToString()))
                    throw new InputValidationException("Invalid character (black list)");
        }

        static public void ParseWhiteList(string term, string whiteList = "^[0-9\\p{L} -_.,%/#]{0,120}$")    // \p{L} .... Unicode Letters
        {
            Regex reWhiteList = new Regex(whiteList);
            if (reWhiteList.IsMatch(term))
                return; // it's ok, proceed to step 2
            else
                throw new InputValidationException("Invalid character (white list)");// it's not ok, inform user they've entered invalid characters and try again
        }

        static public string Parse(string term, string whiteList = "^[0-9\\p{L} -_.,%/#]{0,120}$", bool parse = true)    // \p{L} .... Unicode Letters
        {
            if (parse)
            {
                ParseBlackList(term);
                ParseWhiteList(term, whiteList);
            }
            return term;
        }

        static public string ParsePro(string term, string whiteList = "^[0-9\\p{L} -_.,%/#]{0,120}$", string ignoreCharacters = "", bool parse = true)
        {
            if (parse)
            {
                string originalTerm = term;

                if ((!String.IsNullOrWhiteSpace(ignoreCharacters) && ignoreCharacters.Contains("'")) ||
                    (!String.IsNullOrWhiteSpace(whiteList) && whiteList.Contains("'")))
                {
                    originalTerm = originalTerm.Replace("'", "''");
                }

                if (!String.IsNullOrWhiteSpace(ignoreCharacters))
                {
                    foreach (char ignoreCharacter in ignoreCharacters)
                    {
                        term = term.Replace(ignoreCharacter.ToString(), "");
                    }
                }

                Parse(term, whiteList, true);
                return originalTerm;
            }

            return term;
        }

        static public string ParsePro(char separator, string term, string whiteList = "^[0-9\\p{L} -_.,%/#]{0,120}$", string ignoreCharacters = "", bool parse = true)
        {
            var terms = term.Split(separator);
            List<string> parsedTerms = new List<string>();
            foreach(var t in terms)
            {
                parsedTerms.Add(ParsePro(t, whiteList, ignoreCharacters, parse));
            }
            return string.Join(separator.ToString(), parsedTerms);
        }
    }
    

    public class InputValidationException : Exception
    {
        public InputValidationException(string message)
            :base(message)
        {

        }
    }
}
