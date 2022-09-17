using CustomSpectreConsole;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static GTA5AddOnCarHelper.LanguageDictionary;
using static GTA5AddOnCarHelper.PremiumDeluxeAutoManager;

namespace GTA5AddOnCarHelper
{
    public sealed class LanguageGenerator : AddOnCarHelperFunctionBase<LanguageGenerator>
    {
        #region Constants

        public const string OutputFileName = "GTA5_LanguageGenerator.txt";
        public const string MakeDictionaryFileName = "VehicleMakes.txt";
        private const string VehicleDownloadRegex = "(^[^-]*-)|((?i)by.*$)|(_)|([0-9]\\.[0-9])|((?i)v[0-9]\\.[0-9])|(\\[.*$)|([^\\x00-\\x7F]+)";

        private const string VehicleMakesWebsite = "https://listcarbrands.com/car-brands-with-a-z/";
        private const string VehicleMakesWebisteXPath = "//li";

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
            listOptions.Add(new ListOption("Auto Assign Model Display Names", AutoAssignModelDisplayNames));
            listOptions.Add(new ListOption("Auto Assign Make Display Names", AutoAssignMakeDisplayNames));
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

        private List<string> GetVehicleMakeDictionary()
        {
            List<string> makes = Utilities.ReadTextFromFile(WorkingDirectory, MakeDictionaryFileName);

            if(!makes.Any())
            {
                string message = string.Format("\nUnable to build a vehicle make database from the file [teal]{0}[/].  " +
                    "Would you like to rebuild the database from a web search?", MakeDictionaryFileName);

                bool confirmation = Utilities.GetConfirmation(message);

                if (!confirmation)
                    return makes;

                SelectionPrompt<string> prompt = new SelectionPrompt<string>();
                prompt.Title = string.Format("\nThe default website used to find vehicle makes is [orange1]{0}[/].  Would you like to use this site or enter your own?", VehicleMakesWebsite);

                string[] choices = new string[] { "Use Default Site", "Use Custom Site (Advanced)" };
                prompt.AddChoices(choices);

                string choice = AnsiConsole.Prompt(prompt);

                string site = VehicleMakesWebsite;
                string xPath = "//li";

                if (choice == choices[1])
                {
                    site = Utilities.GetInput("Enter the website to search for: ", x => !string.IsNullOrEmpty(x));
                    xPath = Utilities.GetInput("Enter the xPath: ", x => !string.IsNullOrEmpty(x));
                }

                List<string> searchResults = WebSearch.GetResults(site, xPath);
                string resultText = string.Join('\n', searchResults.Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)));

                Console.WriteLine();
                Utilities.WriteToFile(WorkingDirectory, MakeDictionaryFileName, new StringBuilder(resultText));
                Console.WriteLine();

                makes = Utilities.ReadTextFromFile(WorkingDirectory, MakeDictionaryFileName);
            }

            return makes;
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

        [Documentation(AutoAssignModelNamesSummary)]
        private void AutoAssignModelDisplayNames()
        {
            DirectoryInfo downloadsDir = Settings.GetDirectory(Settings.Node.VehicleDownloadsPath);

            string message = string.Format("This action will attempt update all of the [orange1]Model[/] mappings with " +
                "data pulled from the names of downloads in the [violet]{0}[/] directory.  How would you like to proceed?", downloadsDir.FullName);

            string[] choices = new string[] { "Update All Models", "Update Models With Empty Display Names Only" };

            SelectionPrompt<string> prompt = new SelectionPrompt<string>();
            prompt.Title = message;
            prompt.AddChoices(choices);

            bool updateEmptyOnly = AnsiConsole.Prompt(prompt) == choices[1];

            DirectoryInfo tempDir = DLCExtractor.Instance.EnsureTempDirectory();
            DLCExtractor.Instance.ExtractDirectories();

            if (!tempDir.GetDirectories().Any())
            {
                AnsiConsole.MarkupLine("No valid files were found in the directory [red]{0}[/].  Please extract your vehicle downloads from the " +
                "gta-5 mods site into this directory to proceed.", downloadsDir.FullName);
                return;
            }

            Dictionary<string, LanguageMapping> modelMappings = Mappings.Where(x => x.Value.MappingType == nameof(VehicleMeta.Model))
                                                                        .ToDictionary(x => x.Key, x => x.Value);
            List<string> makes = GetVehicleMakeDictionary();

            int successCount = 0;
            HashSet<string> updatedHashes = new HashSet<string>();

            foreach (DirectoryInfo dir in tempDir.GetDirectories())
            {
                FileInfo[] files = dir.GetFiles("*", SearchOption.AllDirectories);

                foreach (FileInfo file in files)
                {
                    string directoryName = file.Directory.Name.Trim();
                    string hash = Utilities.GetHash(file.Directory.Name.Trim());

                    if (updatedHashes.Contains(hash))
                        continue;

                    modelMappings.TryGetValue(hash, out LanguageMapping mapping);

                    if (mapping == null)
                        mapping = modelMappings.Values.FirstOrDefault(x => directoryName.Contains(x.Identifier));

                    if (mapping != null)
                    {
                        if (updatedHashes.Contains(mapping.Hash))
                            continue;

                        if (updateEmptyOnly && !string.IsNullOrEmpty(mapping.DisplayName))
                            continue;

                        string text = Regex.Replace(dir.Name, VehicleDownloadRegex, string.Empty);

                        List<string> pieces = text.SplitByCase().Split(new char[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries).Where(x => x.Count() > 1).ToList();
                        List<string> makeNames = makes.Where(x => pieces.Any(y => x.Contains(y, StringComparison.OrdinalIgnoreCase))).ToList();

                        if (makeNames.Count() > 1)
                            makeNames = makeNames.Where(x => x.SplitByCase().Split(new char[] { ' ', '-' }).ToList().All(y => pieces.Contains(y))).ToList();

                        string makeToRemove = string.Empty;

                        if (makeNames.Count() > 1)
                        {
                            message = string.Format("Multiple make names were found in the text: [teal]{0}[/]\nFor the model: " +
                                "[yellow]{1}[/].\nWhich make name should be removed from the text?", text, mapping.Identifier);
                            prompt = new SelectionPrompt<string>();
                            prompt.Title = message;
                            prompt.AddChoices(makeNames);

                            makeToRemove = AnsiConsole.Prompt(prompt);
                        }
                        else { makeToRemove = makeNames.FirstOrDefault(); }

                        if (!string.IsNullOrEmpty(makeToRemove))
                        {
                            if (text.Contains(makeToRemove.Replace("-", " ")))
                                makeToRemove = makeToRemove.Replace("-", " ");

                            if (!text.Contains(makeToRemove))
                            {
                                makeToRemove = makeToRemove.Split(new char[]{ ' ', '-' }, StringSplitOptions.RemoveEmptyEntries)
                                                           .FirstOrDefault(x => text.Contains(x, StringComparison.OrdinalIgnoreCase));
                            }

                            if (!string.IsNullOrEmpty(makeToRemove) && text.Contains(makeToRemove) && !string.Equals(text, makeToRemove, StringComparison.OrdinalIgnoreCase))
                            {
                                text = text.Replace(string.Format("{0} ", makeToRemove), string.Empty, StringComparison.OrdinalIgnoreCase);
                                text = text.Replace(makeToRemove, string.Empty, StringComparison.OrdinalIgnoreCase);
                            }
                        }

                        text = text.SplitByCase();
                        text = string.Format("{0}{1}", char.ToUpper(text[0]), text.Substring(1));

                        mapping.DisplayName = text;
                        successCount++;
                        updatedHashes.Add(mapping.Hash);
                    }
                }
            }

            tempDir.Delete(true);

            AnsiConsole.MarkupLine("A total of [pink1]{0}[/] models have had their display names updated successfully!", successCount);
        }

        [Documentation(AutoAssignMakeNamesSummary)]
        private void AutoAssignMakeDisplayNames()
        {
            string message = string.Format("This action will attempt update all of the [orange1]Make[/] mappings with " +
                "data pulled from the file [violet]{0}[/].  How would you like to proceed?", MakeDictionaryFileName);

            string[] choices = new string[] { "Update All Makes", "Update Makes With Empty Display Names Only" };

            SelectionPrompt<string> prompt = new SelectionPrompt<string>();
            prompt.Title = message;
            prompt.AddChoices(choices);

            bool updateEmptyOnly = AnsiConsole.Prompt(prompt) == choices[1];

            List<string> makes = GetVehicleMakeDictionary();
            List<LanguageMapping> makeMappings = Mappings.Values.Where(x => x.MappingType == nameof(VehicleMeta.Make)).ToList();
            int successCount = 0;

            foreach (LanguageMapping mapping in makeMappings)
            {
                if (updateEmptyOnly && !string.IsNullOrEmpty(mapping.DisplayName))
                    continue;

                List<string> matches = makes.Where(x 
                    => mapping.Identifier.Contains(x, StringComparison.OrdinalIgnoreCase)
                    || !string.IsNullOrEmpty(mapping.DisplayName) && mapping.DisplayName.Contains(x, StringComparison.OrdinalIgnoreCase)
                    || mapping.Identifier.Contains(new string(x.Where(y => char.IsLetterOrDigit(y)).ToArray()), StringComparison.OrdinalIgnoreCase)
                    || mapping.Identifier.Replace(" ", string.Empty).Contains(new string(x.Where(y => char.IsLetterOrDigit(y)).ToArray()), StringComparison.OrdinalIgnoreCase)
                ).ToList();

                if (!matches.Any() || matches.Any(x => string.Equals(x, mapping.DisplayName)))
                    continue;

                string displayName = string.Empty;

                if(matches.Count > 1)
                {
                    message = string.Format("Multiple possible display names were found for the Make with identifier: [teal]{0}[/], which is correct?", mapping.Identifier);
                    prompt = new SelectionPrompt<string>();
                    prompt.Title = message;
                    prompt.AddChoices(matches);

                    displayName = AnsiConsole.Prompt(prompt);

                }
                else { displayName = matches.First(); }

                mapping.DisplayName = displayName;
                successCount++;
            }

            AnsiConsole.MarkupLine("A total of [pink1]{0}[/] makes have had their display names updated successfully!", successCount);
        }

        [Documentation(SaveChangesSummary)]
        private void SaveChanges()
        {
            Utilities.ArchiveFiles(WorkingDirectory, "*.txt", new List<string>() { OutputFileName });

            StringBuilder content = new StringBuilder();
            HashSet<string> additionalMappings = new HashSet<string>();

            foreach (KeyValuePair<string, LanguageMapping> pair in Mappings)
            {
                string entry1 = pair.Value.Save();
                string entry2 = pair.Value.SaveAdditionalMappings(additionalMappings);

                if(!string.IsNullOrEmpty(entry1))
                    content.AppendLine(entry1);

                if (!string.IsNullOrEmpty(entry2))
                    content.AppendLine(entry2);
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
                prompt.InstructionsText = "[grey](Press [teal]<space>[/] to toggle an option, [green]<enter>[/] to begin)[/]\n";
                prompt.Required = false;
                prompt.PageSize = 20;

                prompt.AddChoice(EditOptionChoice<LanguageMapping>.OrderByOption());
                prompt.AddChoice(EditOptionChoice<LanguageMapping>.PartialTextMatchOption());
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

            public string SaveAdditionalMappings(HashSet<string> mappings)
            {
                StringBuilder sb = new StringBuilder();

                if (!string.IsNullOrEmpty(GameName) && !string.IsNullOrEmpty(DisplayName) && !string.Equals(DisplayName, GameName, StringComparison.Ordinal))
                {
                    string gameNameHash = Utilities.GetHash(GameName);

                    if (!(string.Equals(Hash, gameNameHash)) && !mappings.Contains(gameNameHash))
                    {
                        mappings.Add(gameNameHash);
                        sb.Append(string.Format("{0} = {1}", gameNameHash, DisplayName));
                    }
                }

                return sb.ToString();
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

                    if (!string.IsNullOrEmpty(modelHash) && !mappings.ContainsKey(modelHash))
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

                    if (!string.IsNullOrEmpty(makeHash) && !mappings.ContainsKey(makeHash))
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
        "the input blank and press [teal]<enter>[/], the current mapping will be skipped.";

        private const string AutoAssignModelNamesSummary = "Attempts to automatically assign display names to models by trying to extract the vehicle name that's normally included in the name " +
        "of the downloaded file/folder from the gta5-mods site.  It tries to remove any unnecessary text using the Regex pattern [teal](^[[^-]]*-)[/][orange1]|[/][teal]((?i)by.*$)[/][orange1]|[/][teal](_)[/]" +
        "[orange1]|[/][teal]([[0-9]]\\.[[0-9]])[/][orange1]|[/][teal]((?i)v[[0-9]]\\.[[0-9]])[/][orange1]|[/][teal](\\[[.*$)[/][orange1]|[/][teal]([[^\\x00-\\x7F]]+)[/] against each of the file names, and " +
        "then attempts to remove the make name if present.  Each download file is associated to a particular model via a match on the extracted donwload folder containing a folder matching the " +
        "model name (normally the folder containing dlc.rpf).";

        private const string AutoAssignMakeNamesSummary = "Attempts to automatically assign display names to makes by doing a partial text match on the make's identifier or current display name " +
        "against the list of vehicle makes defined in the file [violet]VehicleMakes.txt[/].  If this file does not exist or has been deleted, it can be regenerated via a web search against " +
        "the default web page, or via a user-defined web scrape";

        private const string SaveChangesSummary = "Takes all of the mapping edits you've made and generates a file named " + OutputFileName + " that will be saved to the LanguageGenerator folder. " +
        "The inserts in this file can be copied to the [orange1]mods/update/update.rpf/x64/patch/data/lang[/] folder into the [orange1].cfg[/] file for your language to fix the display names " +
        "of vehicles in game. Every time 'SaveChanges' is selected, any previous " + OutputFileName + " file will be archived so that changes can easily be reversed.  " +
        "[red bold]Don't forget to save changes before exiting the Language Generator menu![/]";

        #endregion
    }
}
