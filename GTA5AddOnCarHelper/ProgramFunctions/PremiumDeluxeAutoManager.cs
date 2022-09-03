using CustomSpectreConsole;
using Spectre.Console;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static GTA5AddOnCarHelper.LanguageDictionary;
using static System.Net.WebRequestMethods;

namespace GTA5AddOnCarHelper
{
    public sealed class PremiumDeluxeAutoManager : AddOnCarHelperFunctionBase<PremiumDeluxeAutoManager>
    {
        #region Constants

        private const string Summary = "A utility that uses extracted vehicles.meta files to generate .ini files that are compatible " +
        "with the Premium Deluxe Motorsport Car Dealership mod. [violet]https://www.gta5-mods.com/scripts/premium-deluxe-motorsports-car-shop[/]";

        private const string PriceGeneratorOutputFileName = "price_generator_search_results.txt";

        #endregion

        #region Properties

        private Dictionary<string, PremiumDeluxeCar> Cars { get; set; }

        #endregion

        #region Public API

        [Documentation(Summary)]
        public override void Run()
        {
            Initialize();
            Cars = PremiumDeluxeCar.GetFromIniDirectory(WorkingDirectory);
            bool importMeta = false;

            if (Cars.Any())
            {
                AnsiConsole.MarkupLine("Imported [blue]{0}[/] vehicles from the directory [blue]{1}[/]\n", Cars.Count(), WorkingDirectoryName);
                string prompt = string.Format("Would you like to check for new [green]{0}[/] files to add to this list?", Constants.FileNames.VehicleMeta + Constants.Extentions.Meta);

                importMeta = Utilities.GetConfirmation(prompt);

                if(importMeta)
                {
                    Dictionary<string, PremiumDeluxeCar> metaCars = PremiumDeluxeCar.GetFromMetaFiles();
                    int importCount = 0;

                    foreach (KeyValuePair<string, PremiumDeluxeCar> pair in metaCars)
                    {
                        if (!Cars.ContainsKey(pair.Key))
                        {
                            Cars.Add(pair.Key, pair.Value);
                            importCount++;
                        }
                    }

                    AnsiConsole.MarkupLine("Imported [blue]{0}[/] new vehicles\n", importCount);
                }
            }
            else 
            {
                string prompt = String.Format("No valid [blue]{0}[/] files were found in the directory [blue]{1}[/].  " +
                    "Would you like to import [blue]{2}[/] files?", Constants.Extentions.Ini, WorkingDirectoryName, Constants.FileNames.VehicleMeta + Constants.Extentions.Meta);

                importMeta = Utilities.GetConfirmation(prompt);

                if (!importMeta)
                    return;

                Cars = PremiumDeluxeCar.GetFromMetaFiles();
            }

            if (Cars.Any())
                RunProgramLoop();
        }

        #endregion

        #region Private API

        protected override List<ListOption> GetListOptions()
        {
            List<ListOption> listOptions = new List<ListOption>();

            listOptions.Add(new ListOption("Show All Vehicles", ShowAllVehicles));
            listOptions.Add(new ListOption("Edit A Vehicle", EditVehicle));
            listOptions.Add(new ListOption("Edit Multiple Vehicles", GetBulkEditOptions));
            listOptions.Add(new ListOption("Save Changes", SaveChanges));
            listOptions.AddRange(base.GetListOptions());
            listOptions.Add(GetHelpOption());

            return listOptions;
        }

        private PremiumDeluxeListFilter GetPartialMatchFiler()
        {
            string prompt = string.Format("Enter in the text you would like to search for.  The list of vehicles " +
                "produced will be based on a partial text on the [blue]{0}[/] or [blue]{1}[/]: ", nameof(PremiumDeluxeCar.Name), nameof(PremiumDeluxeCar.Model));

            string input = Utilities.GetInput(prompt, x => !string.IsNullOrEmpty(x));

            PremiumDeluxeListFilter filter = new PremiumDeluxeListFilter(Cars.Values, input);

            return filter;
        }

        private void UpdateCarFields(PremiumDeluxeCar car, List<PropertyInfo> props = null)
        {
            string oldModel = car.Model;

            if (props == null)
                props = typeof(PremiumDeluxeCar).GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();

            Dictionary<string, object> enteredValues = Utilities.GetEditInput(car, props);

            foreach(KeyValuePair<string, object> pair in enteredValues)
            {
                PropertyInfo prop = props.FirstOrDefault(x => x.Name == pair.Key);

                if (prop != null)
                    prop.SetValue(car, pair.Value);
            }

            if (Cars.ContainsKey(oldModel))
                Cars[oldModel] = null;

            Cars[car.Model] = car;
        }

        private bool AutoCalculateVehiclePrices(ConcurrentDictionary<string, List<int>> pricesByCar)
        {
            string max = "Take the highest value (recommended)";
            string min = "Take the lowest value";
            string average = "Take the average of the values";

            SelectionPrompt<string> prompt = new SelectionPrompt<string>();
            prompt.Title = "How would you like to handle the list of prices found for each vehicle?";
            prompt.AddChoices(new string[] { max, min, average, GlobalConstants.SelectionOptions.Cancel });

            string choice = AnsiConsole.Prompt(prompt);

            if (choice == GlobalConstants.SelectionOptions.Cancel)
                throw new Exception(GlobalConstants.Commands.CANCEL);

            foreach (KeyValuePair<string, PremiumDeluxeCar> pair in Cars)
            {
                pricesByCar.TryGetValue(pair.Key, out List<int> prices);

                if (prices == null || !prices.Any())
                    continue;

                if(choice == max)
                    pair.Value.Price = prices.Max();
                else if (choice == min)
                    pair.Value.Price = prices.Min();
                else if (choice == average)
                    pair.Value.Price = (int)prices.Average();
            }

            return true;
        }

        private bool ManuallyAssignVehiclePrices(ConcurrentDictionary<string, List<int>> pricesByCar)
        {
            foreach (KeyValuePair<string, PremiumDeluxeCar> pair in Cars)
            {
                pricesByCar.TryGetValue(pair.Key, out List<int> prices);

                if (prices == null || !prices.Any())
                    continue;

                List<string> choices = new List<string>() { "Skip", GlobalConstants.SelectionOptions.Cancel };
                choices.AddRange(prices.Select(x => string.Format("{0:n0}", x)));

                SelectionPrompt<string> prompt = new SelectionPrompt<string>();
                prompt.Title = String.Format("How much is a [blue]{0}[/] [pink1]{1}[/]?", pair.Value.Make, pair.Value.GetDisplayName());
                prompt.AddChoices(choices);

                string choice = AnsiConsole.Prompt(prompt);

                if(choice == GlobalConstants.SelectionOptions.Cancel)
                    throw new Exception(GlobalConstants.Commands.CANCEL);

                bool couldParse = int.TryParse(choice, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out int result);

                if(couldParse)
                    pair.Value.Price = result;  
            }

            return true;
        }

        #endregion

        #region Private API: Prompt Functions

        [Documentation("Displays a grid containing all of the currently loaded vehicles")]
        private void ShowAllVehicles()
        {
            PremiumDeluxeListFilter filter = new PremiumDeluxeListFilter(Cars.Values);
            TableDisplay.BuildDisplay<PremiumDeluxeCar>(filter);
        }

        [Documentation("Edit a single vehicle from the list of currently loaded vehicles.  You will be prompted from the model name of the vehicle, which you should be able to copy " +
        "from the grid after using the 'Show All Vehicles' option - and then you will be asked to edit the vehicle's properties one by one.  To skip editing a field, leave the input " +
        "blank and press [blue]<enter>[/].")]
        private void EditVehicle()
        {
            string modelName = Utilities.GetInput("Enter the model name of the vehicle you wish to edit: ", x => !string.IsNullOrEmpty(x));
            Cars.TryGetValue(modelName, out PremiumDeluxeCar car);

            if (car == null) {
                AnsiConsole.MarkupLine(string.Format("A vehicle with the model name [green]{0}[/] was not found", modelName));
                return;
            }

            EditOptions<PremiumDeluxeCar> options = new EditOptions<PremiumDeluxeCar>();
            UpdateCarFields(car, options.GetEditableProperties());
        }

        [Documentation("Shows additional options for editing more than one vehicle at a time")]
        private void GetBulkEditOptions()
        {
            List<ListOption> listOptions = new List<ListOption>();
            listOptions.Add(new ListOption("Edit Vehicles By Filter", EditCarsByFilter));
            listOptions.Add(new ListOption("Bulk Edit Vehicles", BulkEditVehicles));
            listOptions.Add(new ListOption("Update Names From Language Files", UpdateNamesFromLanguageFiles));
            listOptions.Add(new ListOption("Update Vehicle Prices From Web Search", GenerateVehiclePrices));
            listOptions.Add(new ListOption(GlobalConstants.SelectionOptions.ReturnToMenu, () => throw new Exception(GlobalConstants.Commands.CANCEL)));
            listOptions.Add(GetHelpOption());

            SelectionPrompt<ListOption> prompt = new SelectionPrompt<ListOption>();
            prompt.Title = "Select a method for editing:";
            prompt.AddChoices(listOptions);

            ListOption choice = AnsiConsole.Prompt(prompt);

            if (choice.IsHelpOption)
            {
                ((ListOption<List<ListOption>, bool>)choice).Function(listOptions);
                GetBulkEditOptions();
            }
            else
            {
                choice.Function();
            }
        }

        [Documentation("Allows you to edit a list of vehicles after choosing from a list of pre-defined filters.  After choosing the desired " +
        "filters, vehicles matching the filter criteria will be fed in one by one so that you can edit their properties.  While editing, if you leave " +
        "the input blank and press [blue]<enter>[/], the current property will be skipped.")]
        private void EditCarsByFilter()
        {
            EditOptions<PremiumDeluxeCar> options = EditOptions<PremiumDeluxeCar>.Prompt();
            List<PropertyInfo> propsToEdit = options.GetEditableProperties();

            PremiumDeluxeListFilter filter = new PremiumDeluxeListFilter(Cars.Values);

            foreach (PremiumDeluxeCar car in filter.FilterList())
            {
                AnsiConsole.MarkupLine(string.Format("\nNow Editing: [yellow]{0}[/]", car.GetDisplayName()));
                UpdateCarFields(car, propsToEdit);
            }
        }

        [Documentation("Allows you to edit a list of vehicles in bulk after choosing from a list of pre-defined filters.  This option is similar to the 'Edit by filter' " +
        "option, except that vehicles are not fed in one by one for editing.  You will be prompted for values for each of the fields you want to edit, and [red]ALL[/] vehicles matching " +
        "the filters you've chosen will have their properties updated to whatever values you entered.")]
        private void BulkEditVehicles()
        {
            SelectionPrompt<ListOption<PremiumDeluxeListFilter>> prompt = new SelectionPrompt<ListOption<PremiumDeluxeListFilter>>();
            prompt.Title = "Select the method you wish to use to filter down the list of vehicles that will be edited";
            prompt.AddChoice(new ListOption<PremiumDeluxeListFilter>("Filter By Class/Make", () => new PremiumDeluxeListFilter(Cars.Values)));
            prompt.AddChoice(new ListOption<PremiumDeluxeListFilter>("Partial Text Match", GetPartialMatchFiler));
            prompt.AddChoice(new ListOption<PremiumDeluxeListFilter>(GlobalConstants.SelectionOptions.ReturnToMenu, () => throw new Exception(GlobalConstants.Commands.CANCEL)));

            ListOption<PremiumDeluxeListFilter> selection = AnsiConsole.Prompt(prompt);
            PremiumDeluxeListFilter filter = selection.Function();

            AnsiConsole.WriteLine();
            IEnumerable<PremiumDeluxeCar> filteredCars = filter.FilterList();

            if (!filteredCars.Any()) 
            {
                AnsiConsole.WriteLine("No vehicles were found.");
                return;
            }

            TableDisplay.BuildDisplay<PremiumDeluxeCar>(filter);

            EditOptions<PremiumDeluxeCar> options =  EditOptions<PremiumDeluxeCar>.Prompt();
            List<PropertyInfo> propsToEdit = options.GetEditableProperties();

            Dictionary<string, object> enteredValues = Utilities.GetEditInput<PremiumDeluxeCar>(null, propsToEdit);

            if (!enteredValues.Any())
                return;

            AnsiConsole.MarkupLine(string.Format("\nThe [bold red]{0}[/] vehicles shown in the grid above will be updated as follows:", filteredCars.Count()));
            
            foreach(KeyValuePair<string, object> pair in enteredValues)
                AnsiConsole.MarkupLine(string.Format("[bold blue]{0}[/]: {1}", pair.Key, pair.Value));

            string confirmation = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                .Title("\nWould you like to proceed?")
                .AddChoices(GlobalConstants.SelectionOptions.Yes, GlobalConstants.SelectionOptions.No)
            );

            if (confirmation == GlobalConstants.SelectionOptions.No)
                return;

            foreach(PremiumDeluxeCar car in filteredCars)
            {
                foreach (PropertyInfo prop in propsToEdit)
                {
                    enteredValues.TryGetValue(prop.Name, out object value);

                    if (value != null)
                        prop.SetValue(car, value);
                }

                Cars[car.Model] = car;
            }
        }

        [Documentation("Looks at the list of supplied .gxt2 files (or any custom configuration you've performed in the Language Generator), and attempts to replace the vehicle " +
        "names currently assigned to the list of vehicles with their proper names that have been defined in the language files.  This is useful because the Premium Deluxe Motorsport " +
        "dealership manages the names of vehicles on its own.  So if you want the vehicle names at the dealership to match the name that appears when you enter/exit a vehicle, the names" +
        "must be correct in the Premium Deluxe mod's .ini files.")]
        private void UpdateNamesFromLanguageFiles()
        {
            Dictionary<string, LanguageEntry> languageDictionary = LanguageDictionary.GetEntries();

            bool proceed = Utilities.GetConfirmation("This action will attempt to map the name of all vehicles in " +
                "the current list to their corresponding entry in the available language files (if one exists).  Would you like to proceed?");

            if (!proceed)
                return;

            int successCount = 0;

            foreach(KeyValuePair<string, PremiumDeluxeCar> pair in Cars)
            {
                PremiumDeluxeCar car = pair.Value;
                string hash = Utilities.GetHash(car.Model);
                languageDictionary.TryGetValue(hash, out LanguageEntry entry);

                if (entry != null && !string.IsNullOrEmpty(entry.CurrentValue) && entry.CurrentValue != car.Name)
                {
                    AnsiConsole.MarkupLine("[yellow]Model:[/] {0}\n[blue]Previous Name:[/] {1}\n[green]New Name:[/] {2}\n", car.Model, car.Name, entry.CurrentValue);
                    car.Name = entry.CurrentValue;
                    successCount++;
                }
            }
        }

        [Documentation("Performs a Google search on your list of vehicles in an attempt to gather real pricing data for each car that can be automatically or manually assigned. " +
        "It's recommended that you do this AFTER you've assigned realistic names to all of the vehicles - either by manual entry or via the 'Update from Language Files' option. " +
        "Reason being, the search will be based on the MAKE and NAME fields, and if there is strange data in those fields then the results will be inaccurate.")]
        private void GenerateVehiclePrices()
        {
            StringBuilder sb = new StringBuilder();
            ConcurrentDictionary<string, List<int>> pricesByCar = new ConcurrentDictionary<string, List<int>>();

            ProgressColumn[] columns = new ProgressColumn[]
            {
                new TaskDescriptionColumn(), new ProgressBarColumn(),
                new PercentageColumn(), new RemainingTimeColumn()
            };

            AnsiConsole.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(columns)
            .Start(ctx =>
            {
                var task = ctx.AddTask("Gathering [blue]G[/][red]o[/][yellow]o[/][blue]g[/][green]l[/][red]e[/] Search Results", true, Cars.Count);

                while (!ctx.IsFinished)
                {
                    Object _lock = new Object();

                    Parallel.ForEach(Cars, pair =>
                    {
                        PremiumDeluxeCar car = pair.Value;

                        if (string.IsNullOrEmpty(car.Make) || string.IsNullOrEmpty(car.Name))
                        {
                            lock (_lock) 
                            {
                                task.Increment(1);
                                ctx.Refresh();
                            }

                            return;
                        }

                        string query = string.Format("How much is a {0} {1}", car.Make, car.Name);
                        List<string> results = WebSearch.GetResults(query, "$");

                        List<int> resultGroup = Utilities.ParseCurrencyFromText(results);

                        pricesByCar.TryAdd(car.Model, resultGroup);

                        lock (_lock)
                        {
                            sb.AppendLine(query);
                            results.ForEach(x => sb.AppendLine(x.ToString()));
                            sb.AppendLine();

                            task.Increment(1);
                            ctx.Refresh();
                        }
                    });

                    if (sb.Length > 0)
                        Utilities.WriteToFile(WorkingDirectory, PriceGeneratorOutputFileName, sb);

                    task.StopTask();
                }
            });

            if (!pricesByCar.Any())
            {
                AnsiConsole.MarkupLine("[orange1]No pricing information for the current list of vehicles was found during the search.[/]");
                return;
            }

            SelectionPrompt<ListOption<ConcurrentDictionary<string, List<int>>, bool>> prompt = new SelectionPrompt<ListOption<ConcurrentDictionary<string, List<int>>, bool>>();
            prompt.Title = string.Format("Pricing information was found for [pink1]{0}[/] vehicles.  " +
                "In most instances a vehicle will have multiple values to choose from.  How would you like to proceed?", pricesByCar.Count());

            prompt.AddChoice(new ListOption<ConcurrentDictionary<string, List<int>>, bool>("Auto Assign Prices From Results", AutoCalculateVehiclePrices));
            prompt.AddChoice(new ListOption<ConcurrentDictionary<string, List<int>>, bool>("Manually Assign Prices From Results", ManuallyAssignVehiclePrices));
            prompt.AddChoice(new ListOption<ConcurrentDictionary<string, List<int>>, bool>("Cancel", (s) => throw new Exception(GlobalConstants.Commands.CANCEL)));

            ListOption<ConcurrentDictionary<string, List<int>>, bool> option = AnsiConsole.Prompt(prompt);
            option.Function(pricesByCar);
        }

        [Documentation("Takes all of the vehicle edits you've made and generates .ini files that will be saved to your PremiumDeluxeAutoManager folder. " +
        "A separate .ini file will be generated for each unique 'class' you've assigned to each of your vehicles, which will represent how your vehicles " +
        "are grouped together once inside the Premium Deluxe dealership.  These .ini files will need to be added to your [orange1]Grand Theft Auto V/scripts/PremiumDeluxeMotorsport/Vehicles[/] " +
        "folder, and a reference to the name of the .ini file will need to be added to [orange1]Grand Theft Auto V/scripts/PremiumDeluxeMotorsport/Languages[/] in the " +
        "[orange1].cfg[/] language file of your choice.  Every time 'SaveChanges' is selected, any existing .ini files in the PremiumDeluxeManager folder will " +
        "be archived so that changes can easily be reversed.  [red bold]Don't forget to save changes before exiting the Premium Deluxe Auto Manager menu![/]")]
        private void SaveChanges()
        {
            Dictionary<string, List<PremiumDeluxeCar>> carsByClass = Cars.Values.GroupBy(x => x.Class)
                                                                     .ToDictionary(x => x.Key, x => x.OrderBy(y => y.Model).ToList());

            Utilities.ArchiveFiles(WorkingDirectory, "*" + Constants.Extentions.Ini, carsByClass.Keys.Select(x => x + Constants.Extentions.Ini).ToList());

            foreach (KeyValuePair<string, List<PremiumDeluxeCar>> pair in carsByClass)
            {
                StringBuilder content = new StringBuilder();

                pair.Value.ForEach(car =>
                {
                    content.AppendLine(car.Save());
                });

                Utilities.WriteToFile(WorkingDirectory, string.Format("{0}{1}", pair.Key, Constants.Extentions.Ini), content);
            };
        }

        #endregion

        #region Helper Classes

        public class PremiumDeluxeListFilter : ListFilter<PremiumDeluxeCar> 
        {
            public PremiumDeluxeListFilter(IEnumerable<PremiumDeluxeCar> list) : base(list)
            {
            }

            public PremiumDeluxeListFilter(IEnumerable<PremiumDeluxeCar> list, string textMatch) : base(list, textMatch)
            {
            }

            protected override IEnumerable<PremiumDeluxeCar> ApplyTextMatch(IEnumerable<PremiumDeluxeCar> list)
            {
                return list.Where(x => (!string.IsNullOrEmpty(x.Model) && x.Model.Contains(TextMatch)) || (!string.IsNullOrEmpty(x.Name) && x.Name.Contains(TextMatch)));
            }

            protected override void Prompt(IEnumerable<PremiumDeluxeCar> cars)
            {
                if (cars == null)
                    return;

                MultiSelectionPrompt<EditOptionChoice> prompt = new MultiSelectionPrompt<EditOptionChoice>();
                prompt.Title = "Select the options you wish to use to filter the list of vehicles";
                prompt.InstructionsText = "[grey](Press [blue]<space>[/] to toggle an option, [green]<enter>[/] to begin)[/]\n";
                prompt.Required = false;
                prompt.PageSize = 20;

                prompt.AddChoice(new EditOptionChoice(EditOptionChoice.ConfigureSorting, nameof(ListFilter.OrderBys)));

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
                AddFilters(choices);
            }
        }

        #endregion
    }
}
