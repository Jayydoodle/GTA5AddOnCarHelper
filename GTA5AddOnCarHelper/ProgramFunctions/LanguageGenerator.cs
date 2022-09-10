using CustomSpectreConsole;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
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

        [Documentation(Summary)]
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
            listOptions.Add(GetHelpOption());

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

        [Documentation(ShowMappingsSummary)]
        private void ShowMappings()
        {
            LanguageGeneratorListFilter filter = new LanguageGeneratorListFilter(Mappings.Values.OrderBy(x => x.Identifier));
            TableDisplay.BuildDisplay(filter);
        }

        [Documentation(EditMappingSummary)]
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

        [Documentation(EditMappingsByFilterSummary)]
        private void EditMappingsByFilter()
        {
            EditOptions<LanguageMapping> options = new EditOptions<LanguageMapping>();
            List<PropertyInfo> propsToEdit = options.GetEditableProperties();

            LanguageGeneratorListFilter filter = new LanguageGeneratorListFilter(Mappings.Values.OrderBy(x => x.Identifier));

            foreach (LanguageMapping mapping in filter.FilterList())
            {
                string message = !string.IsNullOrEmpty(mapping.GameName)
                    ? string.Format("\nNow Editing: [yellow]{0} - {1}[/]: [teal]GAME NAME[/] - [pink1]{2}[/]", mapping.Hash, mapping.Identifier, mapping.GameName)
                    : string.Format("\nNow Editing: [yellow]{0} - {1}[/]", mapping.Hash, mapping.Identifier);

                AnsiConsole.MarkupLine(message);
                UpdateMappingFields(mapping, propsToEdit);
            }
        }

        [Documentation(SaveChangesSummary)]
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
                MultiSelectionPrompt<EditOptionChoice<LanguageMapping>> prompt = new MultiSelectionPrompt<EditOptionChoice<LanguageMapping>>();
                prompt.Title = "Select the options you wish to use to filter the list of mappings";
                prompt.InstructionsText = "[grey](Press [blue]<space>[/] to toggle an option, [green]<enter>[/] to begin)[/]\n";
                prompt.Required = false;
                prompt.PageSize = 20;

                prompt.AddChoice(EditOptionChoice<LanguageMapping>.OrderByOption());
                prompt.AddChoice(new EditOptionChoice<LanguageMapping>("Show Missing Display Names Only", x => string.IsNullOrEmpty(x.DisplayName)));

                List<string> mappingTypes = list.Where(x => !string.IsNullOrEmpty(x.MappingType)).Select(x => x.MappingType)
                                           .OrderBy(x => x).Distinct().ToList();
                if (mappingTypes.Any())
                {
                    EditOptionChoice<LanguageMapping> filterMappingTypes = new EditOptionChoice<LanguageMapping>("Mapping Type", nameof(LanguageMapping.MappingType));

                    mappingTypes.ForEach(x => filterMappingTypes.AddChild(x, x));
                    prompt.AddChoiceGroup(filterMappingTypes, filterMappingTypes.Children);
                }

                List<EditOptionChoice<LanguageMapping>> choices = AnsiConsole.Prompt(prompt);
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
            [Protected]
            public string GameName { get; set; }
            [TableColumn]
            public string DisplayName { get; set; }
            [TableColumn]
            [Protected]
            public string SourceMetaFile { get; set; }

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
                List<VehicleMeta> vehicles = VehicleMetaFileManager.Instance.GetMetaFiles();

                DirectoryInfo dir = Settings.GetDirectory(Settings.Node.VehicleMetaFilesPath);

                if (!dir.Exists)
                    throw new Exception(string.Format("Unable to find the [red]{0}[/].  Please update the path in the {1} file", Settings.Node.VehicleMetaFilesPath.ToString(), Settings.FileName));

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
                        modelMapping.SourceMetaFile = x.XML.SourceFileName.Replace(dir.FullName, string.Empty);

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
                        makeMapping.SourceMetaFile = x.XML.SourceFileName.Replace(dir.FullName, string.Empty);

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

        #region Documentation

        private const string Summary = "A utility that uses extracted .gxt2 language files to pull in all of the currently configured MAKE and MODEL display names " +
        "for vehicles loaded into the program.  These names can be updated (or added for any vehicles/makes that are missing a display name), and a file will be generated " +
        "containing text inserts which can be pasted into the language files located in [orange1]mods/update/update.rpf/x64/patch/data/lang[/] to fix the display names in game";

        private const string ShowMappingsSummary = "Shows a grid of the currently configured language mappings pulled from the source .gxt2 files " +
        "and the existing " + OutputFileName + " file, if one exists";

        private const string EditMappingSummary = "Edit a single language mapping based on the entered hash value, which can be found from the grid " +
        "that displays when selecting the 'Show Mappings' option";

        private const string EditMappingsByFilterSummary = "Allows you to edit a list of language mappings after choosing from a list of pre-defined filters.  After choosing the desired " +
        "filters, mappings matching the filter criteria will be fed in one by one so that you can edit their display names.  While editing, if you leave " +
        "the input blank and press [blue]<enter>[/], the current mapping will be skipped.";

        private const string SaveChangesSummary = "Takes all of the mapping edits you've made and generates a file named " + OutputFileName + " that will be saved to the LanguageGenerator folder. " +
        "The inserts in this file can be copied to the [orange1]mods/update/update.rpf/x64/patch/data/lang[/] folder into the [orange1].cfg[/] file for your language to fix the display names " +
        "of vehicles in game. Every time 'SaveChanges' is selected, any previous " + OutputFileName + " file will be archived so that changes can easily be reversed.  " +
        "[red bold]Don't forget to save changes before exiting the Language Generator menu![/]";

        #endregion
    }
}
