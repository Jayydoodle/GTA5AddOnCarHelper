using CustomSpectreConsole;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace GTA5AddOnCarHelper
{
    public sealed class PremiumDeluxeAutoManager : AddOnCarHelperFunctionBase
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
            dataSourcePrompt.AddChoices(new string[] { DataSourceTypeMeta, DataSourceTypeIni, CustomSpectreConsole.Constants.SelectionOptions.ReturnToMainMenu });

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
                        throw new Exception(CustomSpectreConsole.Constants.Commands.MENU);
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

        private PremiumDeluxeListFilter GetSelectionFilter()
        {
            return new PremiumDeluxeListFilter(Cars.Values);
        }

        private PremiumDeluxeListFilter GetPartialMatchFiler()
        {
            string prompt = string.Format("Enter in the text you would like to search for.  The list of cars " +
                "produced will be based on a partial text on the [blue]{0}[/] or [blue]{1}[/]: ", nameof(PremiumDeluxeCar.Name), nameof(PremiumDeluxeCar.Model));

            string input = Utilities.GetInput(prompt, x => !string.IsNullOrEmpty(x));

            PremiumDeluxeListFilter filter = new PremiumDeluxeListFilter(input);

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

        #endregion

        #region Private API: Prompt Functions

        private void ShowAllCars()
        {
            PremiumDeluxeListFilter filter = new PremiumDeluxeListFilter(Cars.Values);
            TableDisplay.BuildDisplay<PremiumDeluxeCar>(filter);
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
            EditOptions<PremiumDeluxeCar> options = EditOptions<PremiumDeluxeCar>.Get();
            List<PropertyInfo> propsToEdit = options.GetEditableProperties();

            PremiumDeluxeListFilter filter = new PremiumDeluxeListFilter(Cars.Values);

            foreach (PremiumDeluxeCar car in filter.FilterList())
            {
                AnsiConsole.MarkupLine(string.Format("\nNow Editing: [yellow]{0}[/]", car.GetDisplayName()));
                UpdateCarFields(car, propsToEdit);
            }
        }

        private void BulkEditCars()
        {
            SelectionPrompt<ListOption<PremiumDeluxeListFilter>> prompt = new SelectionPrompt<ListOption<PremiumDeluxeListFilter>>();
            prompt.Title = "Select the method you wish to use to filter down the list of cars that will be edited";
            prompt.AddChoice(new ListOption<PremiumDeluxeListFilter>("Filter By Class/Make", GetSelectionFilter));
            prompt.AddChoice(new ListOption<PremiumDeluxeListFilter>("Partial Text Match", GetPartialMatchFiler));
            prompt.AddChoices(new ListOption<PremiumDeluxeListFilter>(CustomSpectreConsole.Constants.SelectionOptions.ReturnToMenu, null));

            ListOption<PremiumDeluxeListFilter> selection = AnsiConsole.Prompt(prompt);
            PremiumDeluxeListFilter filter = selection.Function();

            AnsiConsole.WriteLine();
            IEnumerable<PremiumDeluxeCar> filteredCars = filter.FilterList();

            if (!filteredCars.Any()) 
            {
                AnsiConsole.WriteLine("No cars were found.");
                return;
            }

            TableDisplay.BuildDisplay<PremiumDeluxeCar>(filter);

            EditOptions<PremiumDeluxeCar> options =  EditOptions<PremiumDeluxeCar>.Get(false);
            List<PropertyInfo> propsToEdit = options.GetEditableProperties();

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
                .AddChoices(CustomSpectreConsole.Constants.SelectionOptions.Yes, CustomSpectreConsole.Constants.SelectionOptions.No)
            );

            if (confirmation == CustomSpectreConsole.Constants.SelectionOptions.No)
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

        public class PremiumDeluxeListFilter : ListFilter<PremiumDeluxeCar> 
        {
            public PremiumDeluxeListFilter(IEnumerable<PremiumDeluxeCar> list) : base(list)
            {
            }

            public PremiumDeluxeListFilter(string textMatch) : base(textMatch)
            {
            }

            protected override IEnumerable<PremiumDeluxeCar> ApplyTextMatch(IEnumerable<PremiumDeluxeCar> list)
            {
                return list.Where(x => (!string.IsNullOrEmpty(x.Model) && x.Model.Contains(TextMatch)) || (!string.IsNullOrEmpty(x.Name) && x.Name.Contains(TextMatch)));
            }

            protected override void Prompt(IEnumerable<PremiumDeluxeCar> cars)
            {
                MultiSelectionPrompt<EditOptionChoice> prompt = new MultiSelectionPrompt<EditOptionChoice>();
                prompt.Title = "Select the options you wish to use to filter the list of cars";
                prompt.InstructionsText = "[grey](Press [blue]<space>[/] to toggle an option, [green]<enter>[/] to begin)[/]\n";
                prompt.Required = false;
                prompt.PageSize = 20;

                prompt.AddChoice(new EditOptionChoice("Configure Ordering", nameof(PremiumDeluxeListFilter.OrderBys)));

                List<string> classes = cars.Where(x => !string.IsNullOrEmpty(x.Class)).Select(x => x.Class)
                                           .OrderBy(x => x).Distinct().ToList();
                if (classes.Any())
                {
                    EditOptionChoice filterClasses = new EditOptionChoice("Filter By Class", nameof(PremiumDeluxeCar.Class));

                    classes.ForEach(x => filterClasses.AddChild(x, x));
                    MultiSelectionPrompt<EditOptionChoice> p = prompt.AddChoiceGroup(filterClasses, filterClasses.Children);
                }

                List<string> makes = cars.Where(x => !string.IsNullOrEmpty(x.Make)).Select(x => x.Make)
                                         .OrderBy(x => x).Distinct().ToList();
                if (makes.Any())
                {
                    EditOptionChoice filterMakes = new EditOptionChoice("Filter By Make", nameof(PremiumDeluxeCar.Make));

                    makes.ForEach(x => filterMakes.AddChild(x, x));
                    MultiSelectionPrompt<EditOptionChoice> p = prompt.AddChoiceGroup(filterMakes, filterMakes.Children);
                }

                List<EditOptionChoice> choices = AnsiConsole.Prompt(prompt);

                IEnumerable<EditOptionChoice> classfilterChildren = choices.Where(x => x.Parent != null)
                                                                .Where(x => x.Parent.Details.Value == nameof(PremiumDeluxeCar.Class));

                IEnumerable<EditOptionChoice> makeFilterChildren = choices.Where(x => x.Parent != null)
                                                                .Where(x => x.Parent.Details.Value == nameof(PremiumDeluxeCar.Make));

                foreach (EditOptionChoice child in classfilterChildren)
                    AddFilter(nameof(PremiumDeluxeCar.Class), child.Details.Value);

                foreach (EditOptionChoice child in makeFilterChildren)
                    AddFilter(nameof(PremiumDeluxeCar.Make), child.Details.Value);

                if (choices.Any(x => x.Details.Value == nameof(PremiumDeluxeListFilter.OrderBys)))
                    OrderBys = GetOrderBys();
            }
        }

        #endregion
    }
}
