﻿using Spectre.Console;
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
    }
}
