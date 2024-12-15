using Spectre.Console;
using System;
using System.Collections;
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

        public static IEnumerable<string> ChunkSplit(this string str, int maxChunkSize)
        {
            for (int i = 0; i < str.Length; i += maxChunkSize)
                yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
        }

        #endregion

        #region Type Extensions

        public static bool HasAttribute<T>(this MemberInfo type)
            where T : Attribute
        {
            return type.CustomAttributes.Any(y => y.AttributeType == typeof(T));
        }

        /// <summary>
        /// Determines whether a type is or is a subclass of another type
        /// </summary>
        /// <param name="current">The type to evaluate</param>
        /// <param name="baseType">The type to evaluate against</param>
        public static bool IsOrSubclassOfType(this Type current, Type baseType)
        {
            do
            {
                if (current == baseType || current.IsSubclassOf(baseType))
                    return true;

                if (current.IsGenericType && current.GetGenericTypeDefinition() == baseType)
                    return true;
            }
            while ((current = current.BaseType) != null);
            return false;
        }

        /// <summary>
        /// Checks to see if the two specific types are compatible, meaning they are either of the same type,
        /// they are of the same base type, or one is a nullable type of the other
        /// </summary>
        /// <param name="type">The first type</param>
        /// <param name="otherType">The other type to compare</param>
        /// <param name="useStrictCompatability">Determines whether strict compatability rules should be used
        /// To make the comparison.  Under these rules a Collection type that has the same underlying type
        /// as a GenericType will be considered incompatible.  Ex. EList[int] and int == not compatible</param>
        public static bool IsCompatible(this Type type, Type otherType, bool useStrictCompatability = false)
        {
            bool compatible = true;

            if (!type.IsGenericType && !otherType.IsGenericType)
            {
                // Ex. int and string

                compatible = type.IsOrSubclassOfType(otherType);
            }
            else if (type.IsGenericType && !otherType.IsGenericType)
            {
                // Ex. EList<int> and int or int and int?

                if (typeof(IEnumerable).IsAssignableFrom(type))
                {
                    Type underlyingType = type.GenericTypeArguments[0];
                    return useStrictCompatability ? false : IsCompatible(underlyingType, otherType);
                }

                Type nullableType = Nullable.GetUnderlyingType(type);
                compatible = nullableType != null && nullableType.Name == otherType.Name;
            }
            else if (otherType.IsGenericType && !type.IsGenericType)
            {
                // do the above, but opposite

                return IsCompatible(otherType, type);
            }
            else if (typeof(ICollection).IsAssignableFrom(type) && !typeof(ICollection).IsAssignableFrom(otherType))
            {
                // Ex. EList<int> and int?

                Type underlyingType = type.GenericTypeArguments[0];
                return useStrictCompatability ? false : IsCompatible(underlyingType, otherType);
            }
            else if (typeof(ICollection).IsAssignableFrom(otherType) && !typeof(ICollection).IsAssignableFrom(type))
            {
                // do the above, but opposite

                return IsCompatible(otherType, type);
            }
            else if (typeof(ICollection).IsAssignableFrom(type) && typeof(ICollection).IsAssignableFrom(otherType))
            {
                // Ex. EList<int> and EList<string>

                Type underlyingType = type.GenericTypeArguments[0];
                Type underlyingOtherType = otherType.GenericTypeArguments[0];

                compatible = IsCompatible(underlyingType, underlyingOtherType);
            }

            return compatible;
        }

        #endregion

        #region Tasks

        public static async Task<T> AwaitTimeout<T>(this Task<T> task, bool logError = true, int timeout = 12000, Action onActionTimeout = null)
        {
            if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
            {
                if (task.IsFaulted && logError)
                    task.Exception.LogException();

                return !task.IsFaulted ? await task : default(T);
            }
            else
            {
                if (!task.IsCompleted && onActionTimeout != null)
                    onActionTimeout();

                return default(T);
            }
        }

        public static async Task AwaitTimeout(this Task task, int timeout = 12000)
        {
            if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
            {
                if (!task.IsFaulted)
                {
                    await task;
                }
                else
                {
                    task.Exception.LogException();
                    await Task.CompletedTask;
                }
            }
            else
            {
                await Task.CompletedTask;
            }
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
