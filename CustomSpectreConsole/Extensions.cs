using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CustomSpectreConsole
{
    public static class Extensions
    {
        #region Property Extensions

        public static bool HasAttribute<T>(this PropertyInfo prop)
        {
            return prop.CustomAttributes.Any(y => y.AttributeType == typeof(T));
        }

        #endregion

        #region String Extensions

        public static string SplitByCase(this string str)
        {
            return Regex.Replace(Regex.Replace(str, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"), @"(\p{Ll})(\P{Ll})", "$1 $2");
        }

        #endregion
    }
}
