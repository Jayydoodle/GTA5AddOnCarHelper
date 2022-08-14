using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CustomSpectreConsole
{
    public abstract class ListFilter
    {
        public List<string> OrderBys { get; set; }
        public Dictionary<string, List<object>> Filters { get; set; }
        public string TextMatch { get; set; }

        public ListFilter()
        {
            OrderBys = new List<string>();
            Filters = new Dictionary<string, List<object>>();
        }
    }

    public class ListFilter<T> : ListFilter
    {
        #region Properties

        private IEnumerable<T> List { get; set; }

        #endregion

        #region Constructor

        public ListFilter(IEnumerable<T> list, string textMatch) : base()
        {
            TextMatch = textMatch;
            List = list;
        }

        public ListFilter(IEnumerable<T> list) : base()
        {
            Prompt(list);
            List = list;
        }

        #endregion

        #region Public API

        public void AddFilters(List<EditOptionChoice> choices)
        {
            IEnumerable<EditOptionChoice> currentChoices = choices;

            if (currentChoices == null || !currentChoices.Any())
                return;

            foreach(EditOptionChoice choice in currentChoices)
            {
                if(choice.Value == nameof(OrderBys))
                {
                    OrderBys = GetOrderBys();
                    continue;
                }

                if (choice.Parent != null)
                    AddFilter(choice.Parent.Value, choice.Value);
                else
                    AddFilter(choice.Value, choice.Value);
            }

        }

        public virtual IEnumerable<T> FilterList()
        {
            IEnumerable<T> filteredList = List;

            foreach (KeyValuePair<string, List<object>> pair in Filters)
            {
                PropertyInfo prop = typeof(T).GetProperty(pair.Key);

                if (prop == null)
                    throw new Exception(String.Format("The type '{0}' does not contain a property named '{1}'", typeof(T).Name, pair.Key));

                if (pair.Value != null && pair.Value.Any())
                    filteredList = filteredList.Where(x => pair.Value.Contains(prop.GetValue(x)));
            }

            if (!string.IsNullOrEmpty(TextMatch) && filteredList != null)
                filteredList = ApplyTextMatch(filteredList);

            return filteredList ?? new List<T>();
        }

        #endregion

        #region Protected API

        protected void AddFilter(string propertyName, object value)
        {
            if (!Filters.ContainsKey(propertyName))
                Filters.Add(propertyName, new List<object>());

            Filters[propertyName].Add(value);
        }

        protected virtual void Prompt(IEnumerable<T> list) { }

        protected virtual IEnumerable<T> ApplyTextMatch(IEnumerable<T> list)
        {
            return list;
        }

        protected List<string> GetOrderBys()
        {
            IEnumerable<string> availableOrderBys = typeof(T).GetProperties().Select(x => x.Name);

            string promptTitleFormat = "Select the {0} field you would like to order the list of cars by, or select [bold green]{1}[/] to proceed";

            SelectionPrompt<string> prompt = new SelectionPrompt<string>();
            prompt.Title = string.Format(promptTitleFormat, "first", CustomSpectreConsole.Constants.SelectionOptions.Continue);
            prompt.AddChoice(CustomSpectreConsole.Constants.SelectionOptions.Continue);
            prompt.AddChoices(availableOrderBys);

            List<string> orderBys = new List<string>();
            string selection = null;

            while (selection != CustomSpectreConsole.Constants.SelectionOptions.Continue && availableOrderBys.Any())
            {
                selection = AnsiConsole.Prompt(prompt);
                availableOrderBys = availableOrderBys.Where(x => x != selection).ToList();

                if (selection != CustomSpectreConsole.Constants.SelectionOptions.Continue)
                    orderBys.Add(selection);

                prompt = new SelectionPrompt<string>();
                prompt.Title = string.Format(promptTitleFormat, "next", CustomSpectreConsole.Constants.SelectionOptions.Continue);
                prompt.AddChoice(CustomSpectreConsole.Constants.SelectionOptions.Continue);
                prompt.AddChoices(availableOrderBys);

                StringBuilder sb = new StringBuilder();
                string first = orderBys.FirstOrDefault();

                orderBys.ForEach(x =>
                {
                    string message = x == first ? string.Format("Ordering by [bold blue]{0}[/] ", x)
                                                : string.Format("Then by [bold blue]{0}[/] ", x);
                    sb.Append(message);
                });

                if (sb.Length > 0)
                    prompt.Title = string.Format("{0}\n{1}", prompt.Title, sb.ToString());
            }

            return orderBys;
        }

        #endregion
    }
}
