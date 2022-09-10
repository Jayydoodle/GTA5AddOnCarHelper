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
        protected Dictionary<string, List<object>> Filters { get; set; }

        public ListFilter()
        {
            OrderBys = new List<string>();
            Filters = new Dictionary<string, List<object>>();
        }
    }

    public class ListFilter<T> : ListFilter
    {
        #region Properties

        private List<Func<T, bool>> CustomFilters { get; set; }
        private IEnumerable<T> List { get; set; }

        #endregion

        #region Constructor

        public ListFilter(IEnumerable<T> list, bool prompt = true) : base()
        {
            List = list;
            CustomFilters = new List<Func<T, bool>>();

            if(prompt)
                Prompt(list);
        }

        #endregion

        #region Public API

        public void AddCustomFilter(Func<T, bool> filter)
        {
            CustomFilters.Add(filter);
        }

        public void AddFilters(List<EditOptionChoice<T>> choices)
        {
            IEnumerable<EditOptionChoice<T>> currentChoices = choices;

            if (currentChoices == null || !currentChoices.Any())
                return;

            foreach(EditOptionChoice<T> choice in currentChoices)
            {
                if(choice.Value == nameof(OrderBys))
                {
                    OrderBys = GetOrderBys();
                    continue;
                }

                if (choice.Parent != null)
                    AddFilter(choice.Parent.Value, choice);
                else
                    AddFilter(choice.Value, choice);
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

            CustomFilters.ForEach(x => filteredList = filteredList.Where(x));

            return filteredList ?? new List<T>();
        }

        #endregion

        #region Protected API

        protected void AddFilter(string propertyName, EditOptionChoice<T> choice)
        {
            if(choice.Action != null)
            {
                AddCustomFilter(choice.Action);
                return;
            }

            if (!Filters.ContainsKey(propertyName))
                Filters.Add(propertyName, new List<object>());

            Filters[propertyName].Add(choice.Value);
        }

        protected virtual void Prompt(IEnumerable<T> list) 
        {
            if (list == null)
                return;

            MultiSelectionPrompt<EditOptionChoice<T>> prompt = new MultiSelectionPrompt<EditOptionChoice<T>>();
            prompt.Title = "Select the options you wish to use to filter the list";
            prompt.InstructionsText = "[grey](Press [blue]<space>[/] to toggle an option, [green]<enter>[/] to begin)[/]\n";
            prompt.Required = false;
            prompt.PageSize = 20;

            prompt.AddChoice(EditOptionChoice<T>.OrderByOption());

            List<EditOptionChoice<T>> choices = AnsiConsole.Prompt(prompt);
            AddFilters(choices);
        }

        protected List<string> GetOrderBys()
        {
            IEnumerable<string> availableOrderBys = typeof(T).GetProperties().Where(x => x.HasAttribute<TableColumnAttribute>()).Select(x => x.Name);

            string promptTitleFormat = "Select the {0} field you would like to order the list of vehicles by and press [bold orange1]<enter>[/], or select [bold green]{1}[/] to proceed";

            SelectionPrompt<string> prompt = new SelectionPrompt<string>();
            prompt.Title = string.Format(promptTitleFormat, "first", GlobalConstants.SelectionOptions.Continue);
            prompt.AddChoice(GlobalConstants.SelectionOptions.Continue);
            prompt.AddChoices(availableOrderBys);

            List<string> orderBys = new List<string>();
            string selection = null;

            while (selection != GlobalConstants.SelectionOptions.Continue && availableOrderBys.Any())
            {
                selection = AnsiConsole.Prompt(prompt);
                availableOrderBys = availableOrderBys.Where(x => x != selection).ToList();

                if (selection != GlobalConstants.SelectionOptions.Continue)
                    orderBys.Add(selection);

                prompt = new SelectionPrompt<string>();
                prompt.Title = string.Format(promptTitleFormat, "next", GlobalConstants.SelectionOptions.Continue);
                prompt.AddChoice(GlobalConstants.SelectionOptions.Continue);
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
