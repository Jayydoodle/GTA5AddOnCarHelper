using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomSpectreConsole
{
    public abstract class SettingsNode
    {
        public string Name { get; set; }
        protected string Prompt { get; set; }
        public abstract SettingsNodeType Type { get; }

        public string GetPrompt()
        {
            return Prompt ?? "Enter your " + Name.SplitByCase();
        }
    }

    public abstract class SettingsNode<T> : SettingsNode, ISettingsNode
    where T : class
    {
        public static List<T> GetAll<T>()
        where T: class
        {
            List<T> items = new List<T>();

            typeof(T).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(x => x.PropertyType.IsOrSubclassOfType(typeof(SettingsNode)))
            .ToList()
            .ForEach(prop =>
            {
                T val = prop.GetValue(null) as T;

                if (val != null)
                    items.Add(val);
            });

            return items;
        }
    }

    public enum SettingsNodeType
    {
        Application,
        DatabaseConnection
    }
}
