using Spectre.Console;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static CustomSpectreConsole.Extensions;

namespace CustomSpectreConsole
{
    public class TableDisplay
    {
        public static void BuildDisplay(IEnumerable items)
        {
            Type enumerableType = items.GetType().GenericTypeArguments[0];
            Type filterType = typeof(ListFilter<>).MakeGenericType(enumerableType);
            
            object listFilter = Activator.CreateInstance(filterType, new object[] { items, true });

            MethodInfo displayMethod = typeof(TableDisplay).GetMethods().First(x => x.IsGenericMethod && x.Name == nameof(BuildDisplay));
            MethodInfo displayMethodConstructed = displayMethod.MakeGenericMethod(enumerableType);

            displayMethodConstructed.Invoke(null, new object[] { listFilter });
        }

        public static void BuildDisplay<T>(ListFilter<T> filter)
        {
            Table table = new Table();
            table.Border = TableBorder.Rounded;
            IEnumerable<T> items = filter.FilterList();

            List<PropertyInfo> props = typeof(T).GetProperties().Where(x => x.HasAttribute<TableColumnAttribute>()).ToList();

            if (!props.Any())
                throw new Exception(string.Format("Could not build a table display for type '{0}'.  The type does not have any properties that have a [{1}] attribute", typeof(T).Name, nameof(TableColumnAttribute)));

            props.ForEach(x => table.AddColumn(x.Name.SplitByCase()));

            IOrderedEnumerable<T> orderedItems = items.OrderBy(x => 1);

            StringBuilder message = new StringBuilder();

            if (filter != null && filter.OrderBys.Any())
            {
                message = new StringBuilder();

                for (int i = 0; i < filter.OrderBys.Count; i++)
                {
                    string propName = filter.OrderBys[i];
                    PropertyInfo prop = typeof(T).GetProperty(propName);

                    if (prop == null)
                        continue;

                    if (i == 0)
                    {
                        orderedItems = prop.PropertyType != typeof(int) ? orderedItems.OrderBy(x => prop.GetValue(x), new NumericStringComparer())
                                                                       : orderedItems.OrderByDescending(x => prop.GetValue(x), new NumericStringComparer());

                        message.Append(string.Format("Ordered by [blue]{0}[/] ", propName));
                    }
                    else
                    {
                        orderedItems = prop.PropertyType != typeof(int) ? orderedItems.ThenBy(x => prop.GetValue(x), new NumericStringComparer())
                                                                       : orderedItems.ThenByDescending(x => prop.GetValue(x), new NumericStringComparer());

                        message.Append(string.Format("Then by [blue]{0}[/] ", propName));
                    }
                }
            }

            foreach (T item in orderedItems)
            {
                List<string> values = new List<string>();

                props.ForEach(prop =>
                {
                    object value = prop.GetValue(item);

                    if (value != null)
                        values.Add(Markup.Escape(value.ToString().Trim()));
                    else
                        values.Add(string.Empty);
                });

                table.AddRow(values.ToArray()).Border = TableBorder.Rounded;
            }

            AnsiConsole.Write(table);

            AnsiConsole.Markup("[blue]{0}[/] items returned.  ", orderedItems.Count());

            if (message.Length > 0)
                AnsiConsole.Markup(message.ToString());

            AnsiConsole.WriteLine();
        }
    }
}
