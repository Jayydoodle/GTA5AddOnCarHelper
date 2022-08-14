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
using static GTA5AddOnCarHelper.LanguageDictionary;
using static System.Net.WebRequestMethods;

namespace GTA5AddOnCarHelper
{
    public sealed class PremiumDeluxeAutoManager : AddOnCarHelperFunctionBase<PremiumDeluxeAutoManager>
    {
        #region Properties

        private Dictionary<string, PremiumDeluxeCar> Cars { get; set; }

        #endregion

        #region Public API

        public override void Run()
        {
            Initialize();
            Cars = PremiumDeluxeCar.GetFromIniDirectory(WorkingDirectory);
            bool importMeta = false;

            if (Cars.Any())
            {
                AnsiConsole.MarkupLine("Imported [blue]{0}[/] cars from the directory [blue]{1}[/]\n", Cars.Count(), WorkingDirectoryName);
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

                    AnsiConsole.MarkupLine("Imported [blue]{0}[/] new cars\n", importCount);
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

            listOptions.Add(new ListOption("Show All Cars", ShowAllCars));
            listOptions.Add(new ListOption("Edit A Car", EditCar));
            listOptions.Add(new ListOption("Edit Cars By Filter", EditCarsByFilter));
            listOptions.Add(new ListOption("Bulk Edit Cars", BulkEditCars));
            listOptions.Add(new ListOption("Update Names From Language Files", UpdateNamesFromLanguageFiles));
            listOptions.Add(new ListOption("Save Changes", SaveChanges));
            listOptions.AddRange(base.GetListOptions());

            return listOptions;
        }

        private PremiumDeluxeListFilter GetPartialMatchFiler()
        {
            string prompt = string.Format("Enter in the text you would like to search for.  The list of cars " +
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

            EditOptions<PremiumDeluxeCar> options = new EditOptions<PremiumDeluxeCar>();
            UpdateCarFields(car, options.GetEditableProperties());
        }

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

        private void BulkEditCars()
        {
            SelectionPrompt<ListOption<PremiumDeluxeListFilter>> prompt = new SelectionPrompt<ListOption<PremiumDeluxeListFilter>>();
            prompt.Title = "Select the method you wish to use to filter down the list of cars that will be edited";
            prompt.AddChoice(new ListOption<PremiumDeluxeListFilter>("Filter By Class/Make", () => new PremiumDeluxeListFilter(Cars.Values)));
            prompt.AddChoice(new ListOption<PremiumDeluxeListFilter>("Partial Text Match", GetPartialMatchFiler));
            prompt.AddChoices(new ListOption<PremiumDeluxeListFilter>(CustomSpectreConsole.Constants.SelectionOptions.ReturnToMenu, () => throw new Exception(CustomSpectreConsole.Constants.Commands.CANCEL)));

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

            EditOptions<PremiumDeluxeCar> options =  EditOptions<PremiumDeluxeCar>.Prompt();
            List<PropertyInfo> propsToEdit = options.GetEditableProperties();

            Dictionary<string, object> enteredValues = Utilities.GetEditInput<PremiumDeluxeCar>(null, propsToEdit);

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

        private void UpdateNamesFromLanguageFiles()
        {
            Dictionary<string, LanguageEntry> languageDictionary = LanguageDictionary.GetEntries();

            bool proceed = Utilities.GetConfirmation("This action will attempt to map the name of all cars in " +
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
                prompt.Title = "Select the options you wish to use to filter the list of cars";
                prompt.InstructionsText = "[grey](Press [blue]<space>[/] to toggle an option, [green]<enter>[/] to begin)[/]\n";
                prompt.Required = false;
                prompt.PageSize = 20;

                prompt.AddChoice(new EditOptionChoice("Configure Ordering", nameof(ListFilter.OrderBys)));

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
