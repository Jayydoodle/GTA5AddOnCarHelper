using CustomSpectreConsole;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static GTA5AddOnCarHelper.LanguageDictionary;
using static GTA5AddOnCarHelper.PremiumDeluxeAutoManager;

namespace GTA5AddOnCarHelper
{
    public sealed class LanguageGenerator : AddOnCarHelperFunctionBase<LanguageGenerator>
    {
        #region Constants

        public const string OutputFileName = "GTA5_LanguageGenerator.txt";

        #endregion

        #region Properties
        
        private Dictionary<string, LanguageMapping> Mappings { get; set; }

        #endregion

        #region Public API

        public override void Run()
        {
            Initialize();
            Mappings = LanguageMapping.BuildMappings();
            RunProgramLoop();
        }

        #endregion

        #region Private API

        protected override List<ListOption> GetListOptions()
        {
            List<ListOption> listOptions = new List<ListOption>();

            listOptions.Add(new ListOption("Show Mappings", ShowMappings));
            listOptions.Add(new ListOption("Edit Mapping", EditMapping));
            listOptions.Add(new ListOption("Edit Mappings By Filter", EditMappingsByFilter));
            listOptions.Add(new ListOption("Save Changes", SaveChanges));
            listOptions.AddRange(base.GetListOptions());

            return listOptions;
        }

        private void UpdateMappingFields(LanguageMapping mapping, List<PropertyInfo> props)
        {
            Dictionary<string, object> enteredValues = Utilities.GetEditInput(mapping, props);

            foreach (KeyValuePair<string, object> pair in enteredValues)
            {
                PropertyInfo prop = props.FirstOrDefault(x => x.Name == pair.Key);

                if (prop != null)
                    prop.SetValue(mapping, pair.Value);
            }

            Mappings[mapping.Hash] = mapping;
        }

        #endregion

        #region Program Functions

        private void ShowMappings()
        {
            LanguageGeneratorListFilter filter = new LanguageGeneratorListFilter(Mappings.Values.OrderBy(x => x.Identifier));
            TableDisplay.BuildDisplay(filter);
        }

        private void EditMapping()
        {
            string hash = Utilities.GetInput("Enter the hash of the mapping you wish to edit: ", x => !string.IsNullOrEmpty(x));
            Mappings.TryGetValue(hash, out LanguageMapping mapping);

            if (mapping == null)
            {
                AnsiConsole.MarkupLine(string.Format("A mapping with the hash [green]{0}[/] was not found", hash));
                return;
            }

            EditOptions<LanguageMapping> options = new EditOptions<LanguageMapping>();
            UpdateMappingFields(mapping, options.GetEditableProperties());
        }

        private void EditMappingsByFilter()
        {
            EditOptions<LanguageMapping> options = new EditOptions<LanguageMapping>();
            List<PropertyInfo> propsToEdit = options.GetEditableProperties();

            LanguageGeneratorListFilter filter = new LanguageGeneratorListFilter(Mappings.Values.OrderBy(x => x.Identifier));

            foreach (LanguageMapping mapping in filter.FilterList())
            {
                AnsiConsole.MarkupLine(string.Format("\nNow Editing: [yellow]{0} - {1}[/]", mapping.Hash, mapping.Identifier));
                UpdateMappingFields(mapping, propsToEdit);
            }
        }

        private void SaveChanges()
        {
            Utilities.ArchiveFiles(WorkingDirectory, "*.txt", new List<string>() { OutputFileName });

            StringBuilder content = new StringBuilder();

            foreach (KeyValuePair<string, LanguageMapping> pair in Mappings)
            {
                string entry = pair.Value.Save();

                if(!string.IsNullOrEmpty(entry))
                    content.AppendLine(entry);
            };

            Utilities.WriteToFile(WorkingDirectory, OutputFileName, content);
        }

        #endregion

        #region Helper Classes

        private class LanguageGeneratorListFilter : ListFilter<LanguageMapping>
        {
            public LanguageGeneratorListFilter(IEnumerable<LanguageMapping> list) : base(list)
            {
            }

            protected override void Prompt(IEnumerable<LanguageMapping> list)
            {
                MultiSelectionPrompt<EditOptionChoice> prompt = new MultiSelectionPrompt<EditOptionChoice>();
                prompt.Title = "Select the options you wish to use to filter the list of mappings";
                prompt.InstructionsText = "[grey](Press [blue]<space>[/] to toggle an option, [green]<enter>[/] to begin)[/]\n";
                prompt.Required = false;
                prompt.PageSize = 20;

                prompt.AddChoice(new EditOptionChoice("Configure Ordering", nameof(ListFilter.OrderBys)));

                List<string> mappingTypes = list.Where(x => !string.IsNullOrEmpty(x.MappingType)).Select(x => x.MappingType)
                                           .OrderBy(x => x).Distinct().ToList();
                if (mappingTypes.Any())
                {
                    EditOptionChoice filterMappingTypes = new EditOptionChoice("Mapping Type", nameof(LanguageMapping.MappingType));

                    mappingTypes.ForEach(x => filterMappingTypes.AddChild(x, x));
                    prompt.AddChoiceGroup(filterMappingTypes, filterMappingTypes.Children);
                }

                List<EditOptionChoice> choices = AnsiConsole.Prompt(prompt);
                AddFilters(choices);   
            }
        }

        private class LanguageMapping
        {
            #region Properties

            [TableColumn]
            [Protected]
            public string MappingType { get; set; }
            [TableColumn]
            [Protected]
            public string Identifier { get; set; }
            [TableColumn]
            [Protected]
            public string Hash { get; set; }
            [TableColumn]
            [Protected]
            public string GameName { get; set; }
            [TableColumn]
            public string DisplayName { get; set; }

            #endregion

            #region Public API

            public string Save()
            {
                return !string.IsNullOrEmpty(DisplayName) ? string.Format("{0} = {1}", Hash, DisplayName) : null;
            }

            #endregion

            #region Static API

            public static Dictionary<string, LanguageMapping> BuildMappings()
            {
                var mappings = new Dictionary<string, LanguageMapping>();
                Dictionary<string, LanguageEntry> languageDictionary = LanguageDictionary.GetEntries();
                List<VehicleMeta> vehicles = VehicleMeta.GetMetaFiles();

                vehicles.ForEach(x =>
                {
                    string modelHash = x.HashOfModel;

                    if (!mappings.ContainsKey(modelHash))
                    {
                        LanguageMapping modelMapping = new LanguageMapping();
                        modelMapping.MappingType = nameof(VehicleMeta.Model);
                        modelMapping.Identifier = x.Model;
                        modelMapping.GameName = x.GameName;
                        modelMapping.Hash = modelHash;

                        languageDictionary.TryGetValue(modelHash, out LanguageEntry modelEntry);

                        if (modelEntry != null)
                            modelMapping.DisplayName = modelEntry.CurrentValue;

                        mappings.Add(modelHash, modelMapping);
                    }

                    string makeHash = x.HashOfMake;

                    if (!mappings.ContainsKey(makeHash))
                    {
                        LanguageMapping makeMapping = new LanguageMapping();
                        makeMapping.MappingType = nameof(VehicleMeta.Make);
                        makeMapping.Identifier = x.Make;
                        makeMapping.Hash = makeHash;

                        languageDictionary.TryGetValue(makeHash, out LanguageEntry makeEntry);

                        if (makeEntry != null)
                            makeMapping.DisplayName = makeEntry.CurrentValue;

                        mappings.Add(makeHash, makeMapping);

                    }
                });

                return mappings;
            }

            #endregion
        }

        #endregion
    }
}
