using Spectre.Console;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GTA5AddOnCarHelper
{
    public sealed class PremiumDeluxeAutoManager : ProgramFunctionBase
    {
        #region Constants

        private const string DataSourceTypeIni = "Premium Deluxe Ini Files";
        private const string DataSourceTypeMeta = "Vehicle.meta Files";

        #endregion

        #region Properties

        public override string DisplayName => nameof(PremiumDeluxeAutoManager).SplitByCase();
        protected override string WorkingDirectoryName => nameof(PremiumDeluxeAutoManager);

        private static readonly Lazy<PremiumDeluxeAutoManager> _instance = new Lazy<PremiumDeluxeAutoManager>(() => new PremiumDeluxeAutoManager());
        public static PremiumDeluxeAutoManager Instance => _instance.Value;

        private Dictionary<string, PremiumDeluxeCar> Cars { get; set; }

        #endregion

        #region Constructor

        private PremiumDeluxeAutoManager()
        {
        }

        #endregion

        #region Public API

        public override void Run()
        {
            Initialize();
            Cars = null;

            SelectionPrompt<string> dataSourcePrompt = new SelectionPrompt<string>();
            dataSourcePrompt.Title = "Select a datasource:";
            dataSourcePrompt.AddChoices(new string[] { DataSourceTypeMeta, DataSourceTypeIni, Constants.SelectionOptions.ReturnToMainMenu });

            while(Cars == null || !Cars.Any())
            {
                string dataSource = AnsiConsole.Prompt(dataSourcePrompt);

                switch(dataSource)
                {
                    case DataSourceTypeMeta:
                        Cars = PremiumDeluxeCar.GetFromMetaFiles();
                        break;
                    case DataSourceTypeIni:
                        Cars = PremiumDeluxeCar.GetFromIniDirectory(WorkingDirectory);

                        if (!Cars.Any())
                        {
                            string message = string.Format("The directory [green]{0}[/] has no ini files, or the " +
                                "ini files present in the directory contain invalid data\n", WorkingDirectory.FullName);

                            AnsiConsole.MarkupLine(message);
                        }
                        break;
                    default:
                        throw new Exception(Constants.Commands.MENU);
                }
            }

            RunProgramLoop();
        }

        #endregion

        #region Private API

        protected override List<ListOption> GetListOptions()
        {
            List<ListOption> listOptions = new List<ListOption>();

            listOptions.Add(new ListOption("Show All Cars", ShowAllCars));
            listOptions.Add(new ListOption("Edit A Car", EditCar));
            listOptions.Add(new ListOption("Edit Cars By Filter", EditCarsByFilter));
            listOptions.Add(new ListOption("Bulk Edit Cars", BulkEditCars));
            listOptions.Add(new ListOption("Save Changes", SaveChanges));
            listOptions.AddRange(base.GetListOptions());

            return listOptions;
        }

        private EditOptions GetEditOptions(bool isBulkEdit = false)
        {
            MultiSelectionPrompt<EditOptionChoice> prompt = new MultiSelectionPrompt<EditOptionChoice>();
            prompt.Title = "Select the fields you wish to edit";
            prompt.InstructionsText = "[grey](Press [blue]<space>[/] to toggle an option, [green]<enter>[/] to begin)[/]\n";
            prompt.PageSize = 20;

            IEnumerable<PropertyInfo> baseChoices = typeof(EditOptions).GetProperties().Where(x => x.PropertyType == typeof(bool));

            if (isBulkEdit)
                baseChoices = baseChoices.Where(x => !x.CustomAttributes.Any(y => y.AttributeType == typeof(ProtectedAttribute)));

            foreach (PropertyInfo prop in baseChoices)
            {
                EditOptionChoice choice = new EditOptionChoice(prop.Name.SplitByCase(), prop.Name);
                prompt.AddChoice(choice).Select();
            }

            List<EditOptionChoice> choices = AnsiConsole.Prompt(prompt);

            EditOptions options = new EditOptions();

            foreach (PropertyInfo prop in baseChoices)
            {
                if (choices.Any(x => x.Details.Value == prop.Name))
                    prop.SetValue(options, true);
            }

            if (isBulkEdit)
                return options;

            ListFilter filter = GetSelectionFilter();
            options.Filter = filter;

            return options;
        }

        private ListFilter GetSelectionFilter()
        {
            MultiSelectionPrompt<EditOptionChoice> prompt = new MultiSelectionPrompt<EditOptionChoice>();
            prompt.Title = "Select the options you wish to use to filter the list of cars";
            prompt.InstructionsText = "[grey](Press [blue]<space>[/] to toggle an option, [green]<enter>[/] to begin)[/]\n";
            prompt.Required = false;
            prompt.PageSize = 20;

            prompt.AddChoice(new EditOptionChoice("Configure Ordering", nameof(ListFilter.OrderBys)));

            List<string> classes = Cars.Values.Where(x => !string.IsNullOrEmpty(x.Class)).Select(x => x.Class)
                                              .OrderBy(x => x).Distinct().ToList();
            if (classes.Any())
            {
                EditOptionChoice filterClasses = new EditOptionChoice("Filter By Class", nameof(ListFilter.Classes));

                classes.ForEach(x => filterClasses.AddChild(x, x));
                MultiSelectionPrompt<EditOptionChoice> p = prompt.AddChoiceGroup(filterClasses, filterClasses.Children);
            }

            List<string> makes = Cars.Values.Where(x => !string.IsNullOrEmpty(x.Make)).Select(x => x.Make)
                                            .OrderBy(x => x).Distinct().ToList();
            if (makes.Any())
            {
                EditOptionChoice filterMakes = new EditOptionChoice("Filter By Make", nameof(ListFilter.Makes));

                makes.ForEach(x => filterMakes.AddChild(x, x));
                MultiSelectionPrompt<EditOptionChoice> p = prompt.AddChoiceGroup(filterMakes, filterMakes.Children);
            }

            List<EditOptionChoice> choices = AnsiConsole.Prompt(prompt);
            ListFilter filter = new ListFilter();

            IEnumerable<EditOptionChoice> classfilterChildren = choices.Where(x => x.Parent != null)
                                                            .Where(x => x.Parent.Details.Value == nameof(ListFilter.Classes));

            IEnumerable<EditOptionChoice> makeFilterChildren = choices.Where(x => x.Parent != null)
                                                            .Where(x => x.Parent.Details.Value == nameof(ListFilter.Makes));

            foreach (EditOptionChoice child in classfilterChildren)
                filter.Classes.Add(child.Details.Value);

            foreach (EditOptionChoice child in makeFilterChildren)
                filter.Makes.Add(child.Details.Value);

            if (choices.Any(x => x.Details.Value == nameof(ListFilter.OrderBys)))
                filter.OrderBys = GetOrderBys();

            return filter;
        }

        private List<string> GetOrderBys()
        {
            IEnumerable<string> availableOrderBys = typeof(PremiumDeluxeCar).GetProperties()
                                                     .Where(x => x.Name != nameof(PremiumDeluxeCar.GXT))
                                                     .Select(x => x.Name);

            string promptTitleFormat = "Select the {0} field you would like to order the list of cars by, or select [bold green]{1}[/] to proceed";

            SelectionPrompt<string> prompt = new SelectionPrompt<string>();
            prompt.Title = string.Format(promptTitleFormat, "first", Constants.SelectionOptions.Continue);
            prompt.AddChoice(Constants.SelectionOptions.Continue);
            prompt.AddChoices(availableOrderBys);

            List<string> orderBys = new List<string>();
            string selection = null;

            while (selection != Constants.SelectionOptions.Continue && availableOrderBys.Any())
            {
                selection = AnsiConsole.Prompt(prompt);
                availableOrderBys = availableOrderBys.Where(x => x != selection).ToList();

                if (selection != Constants.SelectionOptions.Continue)
                    orderBys.Add(selection);

                prompt = new SelectionPrompt<string>();
                prompt.Title = string.Format(promptTitleFormat, "next", Constants.SelectionOptions.Continue);
                prompt.AddChoice(Constants.SelectionOptions.Continue);
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

        private ListFilter GetPartialMatchFiler()
        {
            string prompt = string.Format("Enter in the text you would like to search for.  The list of cars " +
                "produced will be based on a partial text on the [blue]{0}[/] or [blue]{1}[/]: ", nameof(PremiumDeluxeCar.Name), nameof(PremiumDeluxeCar.Model));

            string input = Utilities.GetInput(prompt, x => !string.IsNullOrEmpty(x));

            ListFilter filter = new ListFilter();
            filter.TextMatch = input;

            return filter;
        }

        private void UpdateCarFields(PremiumDeluxeCar car, List<PropertyInfo> props = null)
        {
            if (props == null)
                props = typeof(PremiumDeluxeCar).GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();

            if(Cars.ContainsKey(car.Model))
                Cars[car.Model] = null;

            AnsiConsole.MarkupLine("Press [green]ENTER[/] to skip editing the current field.\n");

            foreach (PropertyInfo prop in props)
            {
                object currentValue = prop.GetValue(car);

                AnsiConsole.WriteLine(string.Format("Current {0}: {1}", prop.Name, currentValue));
                string input = Utilities.GetInput(string.Format("{0}:", prop.Name));

                bool isNumeric = int.TryParse(input, out int numValue);

                if(isNumeric && prop.PropertyType == typeof(string)
                    || !isNumeric && prop.PropertyType == typeof(int) && input != string.Empty)
                {
                    string message = prop.PropertyType == typeof(string) ? "Input cannot be a number" : "Input must be a number";
                    AnsiConsole.WriteLine(message);
                    continue;
                };

                if(isNumeric && numValue < 0)
                {
                    AnsiConsole.WriteLine("Number must be greater than 0");
                    continue;
                }

                object value = isNumeric ? numValue : !string.IsNullOrEmpty(input) ? input : null;

                if (value != null)
                    prop.SetValue(car, value);
            }

            Cars[car.Model] = car;
        }

        private void BuildCarDisplay(IEnumerable<PremiumDeluxeCar> filteredCars, ListFilter filter)
        {
            Table table = new Table();
            table.AddColumn(nameof(PremiumDeluxeCar.Name));
            table.AddColumn(nameof(PremiumDeluxeCar.Model));
            table.AddColumn(nameof(PremiumDeluxeCar.Make));
            table.AddColumn(nameof(PremiumDeluxeCar.Class));
            table.AddColumn(nameof(PremiumDeluxeCar.Price));

            IOrderedEnumerable<PremiumDeluxeCar> orderedCars = filteredCars.OrderBy(x => x.Model);
            StringBuilder message = new StringBuilder();
            message.Append("Ordered by [blue]Model[/] ");

            if (filter != null && filter.OrderBys.Any())
            {
                message = new StringBuilder();

                for (int i = 0; i < filter.OrderBys.Count; i++)
                {
                    string propName = filter.OrderBys[i];
                    PropertyInfo prop = typeof(PremiumDeluxeCar).GetProperty(propName);

                    if (i == 0)
                    {
                        orderedCars = prop.PropertyType != typeof(int) ? orderedCars.OrderBy(x => prop.GetValue(x))
                                                                       : orderedCars.OrderByDescending(x => prop.GetValue(x));

                        message.Append(string.Format("Ordered by [blue]{0}[/] ", propName));
                    }
                    else
                    {
                        orderedCars = prop.PropertyType != typeof(int) ? orderedCars.ThenBy(x => prop.GetValue(x))
                                                                       : orderedCars.ThenByDescending(x => prop.GetValue(x));

                        message.Append(string.Format("Then by [blue]{0}[/] ", propName));
                    }
                }
            }

            AnsiConsole.MarkupLine(message.ToString());

            foreach (PremiumDeluxeCar car in orderedCars)
                table.AddRow(new string[] { car.Name, car.Model, car.Make, car.Class, car.Price.ToString() });

            AnsiConsole.Write(table);
        }

        private IEnumerable<PremiumDeluxeCar> FilterCarList(ListFilter filter)
        {
            IEnumerable<PremiumDeluxeCar> filteredCars = Cars.Values;

            if (filter.Classes.Any())
                filteredCars = filteredCars.Where(x => filter.Classes.Contains(x.Class));

            if (filter.Makes.Any())
                filteredCars = filteredCars.Where(x => filter.Makes.Contains(x.Make));

            if (!string.IsNullOrEmpty(filter.TextMatch))
                filteredCars = filteredCars.Where(x => x.Model.Contains(filter.TextMatch) || x.Name.Contains(filter.TextMatch));

            return filteredCars;
        }

        #endregion

        #region Private API: Prompt Functions

        private void ShowAllCars()
        {
            ListFilter filter = GetSelectionFilter();
            IEnumerable<PremiumDeluxeCar> filteredCars = FilterCarList(filter);

            BuildCarDisplay(filteredCars, filter);
        }

        private void EditCar()
        {
            string modelName = Utilities.GetInput("Enter the model name of the car you wish to edit: ", x => !string.IsNullOrEmpty(x));
            Cars.TryGetValue(modelName, out PremiumDeluxeCar car);

            if (car == null) {
                AnsiConsole.MarkupLine(string.Format("A car with the model name [green]{0}[/] was not found", modelName));
                return;
            }

            UpdateCarFields(car);
        }

        private void EditCarsByFilter()
        {
            EditOptions options = GetEditOptions();

            List<string> editTypeNames = typeof(EditOptions).GetProperties()
                                         .Where(x => x.PropertyType == typeof(bool))
                                         .Where(x => ((bool)x.GetValue(options)))
                                         .Select(x => x.Name.Replace(EditOptions.PropPrefix, string.Empty))
                                         .ToList();

            List<PropertyInfo> propsToEdit = typeof(PremiumDeluxeCar).GetProperties()
                                             .Where(x => editTypeNames.Contains(x.Name)).ToList();

            IEnumerable<PremiumDeluxeCar> filteredCars = FilterCarList(options.Filter);

            foreach(PremiumDeluxeCar car in filteredCars)
            {
                AnsiConsole.MarkupLine(string.Format("\nNow Editing: [yellow]{0}[/]", car.GetDisplayName()));
                UpdateCarFields(car, propsToEdit);
            }
        }

        private void BulkEditCars()
        {
            SelectionPrompt<ListOption<ListFilter>> prompt = new SelectionPrompt<ListOption<ListFilter>>();
            prompt.Title = "Select the method you wish to use to filter down the list of cars that will be edited";
            prompt.AddChoice(new ListOption<ListFilter>("Filter By Class/Make", GetSelectionFilter));
            prompt.AddChoice(new ListOption<ListFilter>("Partial Text Match", GetPartialMatchFiler));
            prompt.AddChoices(new ListOption<ListFilter>(Constants.SelectionOptions.ReturnToMenu, null));

            ListOption<ListFilter> selection = AnsiConsole.Prompt(prompt);
            ListFilter filter = selection.Function();

            AnsiConsole.WriteLine();
            IEnumerable<PremiumDeluxeCar> filteredCars = FilterCarList(filter);

            if (!filteredCars.Any()) 
            {
                AnsiConsole.WriteLine("No cars were found.");
                return;
            }

            BuildCarDisplay(filteredCars, filter);

            EditOptions options = GetEditOptions(true);

            List<string> editTypeNames = typeof(EditOptions).GetProperties()
                                         .Where(x => x.PropertyType == typeof(bool))
                                         .Where(x => ((bool)x.GetValue(options)))
                                         .Select(x => x.Name.Replace(EditOptions.PropPrefix, string.Empty))
                                         .ToList();

            List<PropertyInfo> propsToEdit = typeof(PremiumDeluxeCar).GetProperties()
                                             .Where(x => editTypeNames.Contains(x.Name)).ToList();

            Dictionary<string, object> enteredValues = new Dictionary<string, object>();

            foreach (PropertyInfo prop in propsToEdit)
            {
                string input = Utilities.GetInput(string.Format("{0}:", prop.Name));

                bool isNumeric = int.TryParse(input, out int numValue);

                if (isNumeric && prop.PropertyType == typeof(string)
                    || !isNumeric && prop.PropertyType == typeof(int) && input != string.Empty)
                {
                    string message = prop.PropertyType == typeof(string) ? "Input cannot be a number" : "Input must be a number";
                    AnsiConsole.WriteLine(message);
                    continue;
                };

                if (isNumeric && numValue < 0)
                {
                    AnsiConsole.WriteLine("Number must be greater than 0");
                    continue;
                }

                object value = isNumeric ? numValue : !string.IsNullOrEmpty(input) ? input : null;

                if (value != null)
                    enteredValues.Add(prop.Name, value);
            }

            if (!enteredValues.Any())
                return;

            AnsiConsole.MarkupLine(string.Format("\nThe [bold red]{0}[/] cars shown in the grid above will be updated as follows:", filteredCars.Count()));
            
            foreach(KeyValuePair<string, object> pair in enteredValues)
                AnsiConsole.MarkupLine(string.Format("[bold blue]{0}[/]: {1}", pair.Key, pair.Value));

            string confirmation = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                .Title("\nWould you like to proceed?")
                .AddChoices(Constants.SelectionOptions.Yes, Constants.SelectionOptions.No)
            );

            if (confirmation == Constants.SelectionOptions.No)
                return;

            foreach(PremiumDeluxeCar car in filteredCars)
            {
                foreach (PropertyInfo prop in propsToEdit)
                {
                    enteredValues.TryGetValue(prop.Name, out object value);
                    prop.SetValue(car, value);
                }

                Cars[car.Model] = car;
            }
        }

        private void SaveChanges()
        {
            Dictionary<string, List<PremiumDeluxeCar>> carsByClass = Cars.Values.GroupBy(x => x.Class)
                                                                     .ToDictionary(x => x.Key, x => x.OrderBy(y => y.Model).ToList());

            Utilities.ArchiveFiles(WorkingDirectory, "*.ini", carsByClass.Keys.ToList());

            foreach (KeyValuePair<string, List<PremiumDeluxeCar>> pair in carsByClass)
            {
                StringBuilder content = new StringBuilder();

                pair.Value.ForEach(car =>
                {
                    content.AppendLine(car.Save());
                });

                Utilities.WriteToFile(WorkingDirectory, string.Format("{0}.ini", pair.Key), content);
            };
        }

        #endregion

        #region Helper Classes

        private class EditOptions
        {
            public const string PropPrefix = "Edit";

            [Protected]
            public bool EditName { get; set; }
            public bool EditMake { get; set; }
            [Protected]
            public bool EditModel { get; set; }
            public bool EditClass { get; set; }
            [Protected]
            public bool EditGXT { get; set; }
            public bool EditPrice { get; set; }
            public ListFilter Filter { get; set; }

            public EditOptions()
            {
                Filter = new ListFilter();
            }
        }

        private class ListFilter
        {
            public List<string> OrderBys { get; set; }
            public List<string> Makes { get; set; }
            public List<string> Classes { get; set; }
            public string TextMatch { get; set; }

            public ListFilter()
            {
                OrderBys = new List<string>();
                Makes = new List<string>();
                Classes = new List<string>();
            }
        }

        private enum ListFilterType
        {
            FilterSelection,
            PartialTextMatch
        }

        private class EditOptionChoice : IMultiSelectionItem<EditOptionChoiceDetails>
        {
            public EditOptionChoice Parent { get; set; }
            public List<EditOptionChoice> Children { get; set; }
            public EditOptionChoiceDetails Details { get; set; }
            public bool IsSelected { get; set; }

            public EditOptionChoice(EditOptionChoiceDetails details)
            {
                Details = details;
            }

            public EditOptionChoice(string displayName, string value)
            {
                Details = new EditOptionChoiceDetails(displayName, value);
            }

            public ISelectionItem<EditOptionChoiceDetails> AddChild(string displayName, string value)
            {
                EditOptionChoiceDetails childDetails = new EditOptionChoiceDetails(displayName, value);
                return AddChild(childDetails);
            }

            public ISelectionItem<EditOptionChoiceDetails> AddChild(EditOptionChoiceDetails details)
            {
                EditOptionChoice child = new EditOptionChoice(details);
                child.Parent = this;

                if (Children == null)
                    Children = new List<EditOptionChoice>();

                Children.Add(child);

                return child;
            }

            public IMultiSelectionItem<EditOptionChoiceDetails> Select()
            {
                IsSelected = true;

                if (Parent != null)
                    Parent.Select();

                return this;
            }

            public override string ToString()
            {
                return Details.DisplayName;
            }
        }

        private class EditOptionChoiceDetails
        {
            public string DisplayName { get; set; }
            public string Value { get; set; }

            public EditOptionChoiceDetails(string displayName, string value)
            {
                DisplayName = displayName;
                Value = value;
            }
        }

        #endregion
    }
}
