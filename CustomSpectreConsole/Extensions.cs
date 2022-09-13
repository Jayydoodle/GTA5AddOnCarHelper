using Spectre.Console;
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
        #region String Extensions

        public static string SplitByCase(this string str)
        {
            return Regex.Replace(str, @"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])|(?<=[0-9])(?=[A-Z][a-z])", " ");
        }

        #endregion

        #region Type Extensions

        public static bool HasAttribute<T>(this MemberInfo type)
            where T : Attribute
        {
            return type.CustomAttributes.Any(y => y.AttributeType == typeof(T));
        }

        #endregion



        #region IComparer

        public class NumericStringComparer : Comparer<object?>
        {
            public override int Compare(object? x, object? y)
            {
                if (x == null || y == null)
                    return Object.Equals(x, y) ? 1 : 0;

                bool isXNumeric = double.TryParse(x.ToString(), out double xN);
                bool isYNumeric = double.TryParse(y.ToString(), out double yN);

                if (isXNumeric && isYNumeric)
                    return xN.CompareTo(yN);

                if (x is IComparable && y is IComparable)
                    return ((IComparable)x).CompareTo((IComparable)y);

                return Object.Equals(x, y) ? 1 : 0;
            }
        }

        #endregion
    }
}
