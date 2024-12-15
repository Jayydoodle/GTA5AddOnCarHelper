using CustomSpectreConsole;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GTA5AddOnCarHelper
{
    class Program
    {
        private const string VersionNumber = "6.3";

        static void Main(string[] args)
        {
            Console.SetWindowSize(Console.LargestWindowWidth / 2, Console.LargestWindowHeight / 2);
            System.Console.OutputEncoding = Encoding.UTF8;

            Settings.GetDirectory(Settings.Node.GTA5FolderPath);
            Settings.GetDirectory(Settings.Node.WorkingDirectoryPath);
            Settings.GetFile(Settings.Node.OpenIVPath, null, false);
            Console.Clear();

            SelectionPrompt<MenuOption> prompt = new SelectionPrompt<MenuOption>();
            prompt.Title = "Select an option:";
            List<MenuOption> options = CreateMenuOptions();
            prompt.AddChoices(options);

            bool printMenuHeading = true;

            while(true)
            {
                if (printMenuHeading)
                    PrintMenuHeading();

                MenuOption option = AnsiConsole.Prompt(prompt);

                if (option.Function != null || option.IsHelpOption)
                {
                    try
                    {
                        if(option is AddOnCarHelperFunctionBase)
                            ((AddOnCarHelperFunctionBase)option).WriteHeaderToConsole();

                        printMenuHeading = true;

                        if (option.IsHelpOption)
                        {
                            printMenuHeading = false;
                            AnsiConsole.Clear();
                            ((MenuOption<List<MenuOption>, bool>)option).Function(options);
                        }
                        else
                        {
                            option.Function();
                            AnsiConsole.Clear();
                        }
                    }
                    catch (Exception e)
                    {
                        if (e.Message == GlobalConstants.Commands.EXIT)
                            break;
                        else 

                        AnsiConsole.Clear();

                        if (e.Message != GlobalConstants.Commands.MENU)
                            AnsiConsole.Write(string.Format("{0}\n\n", e.Message));
                    }
                }
                else
                {
                    break;
                }
            }
        }

        private static void PrintMenuHeading()
        {
            Rule rule = new Rule(string.Format("[green]GTA 5 AddOn Car Helper v{0}[/]\n", VersionNumber)).DoubleBorder<Rule>();
            AnsiConsole.Write(rule);

            Dictionary<Settings.Node, string> paths = Settings.GetValues(x => x.ToString().Contains("Path"));

            if (paths.Any(x => !string.IsNullOrEmpty(x.Value)))
            {
                string message = string.Format("The paths that will be used during execution of this program are defined below." +
                    "  To change any of the values, remove the corresponding entry in the [green]{0}[/] file\n\n", Settings.FileName);

                AnsiConsole.Markup(message);

                Enum.GetValues<Settings.Node>()
                .ToList()
                .ForEach(x =>
                {
                    paths.TryGetValue(x, out string value);

                    if (!string.IsNullOrEmpty(value))
                    {
                        AnsiConsole.Markup(string.Format("[green]{0}[/]: {1}", x, value));

                        if (!Directory.Exists(value) && !File.Exists(value))
                            AnsiConsole.Markup(" [red bold]INVALID PATH[/]");

                        AnsiConsole.WriteLine();
                    }
                });

                rule = new Rule();
                AnsiConsole.Write(rule);
                AnsiConsole.Write("\n\n");
            }
        }

        private static List<MenuOption> CreateMenuOptions()
        {
            List<MenuOption> menuOptions = new List<MenuOption>();

            menuOptions.Add(DLCExtractor.Instance);
            menuOptions.Add(VehicleMetaFileManager.Instance);
            menuOptions.Add(LanguageGenerator.Instance);
            menuOptions.Add(PremiumDeluxeAutoManager.Instance);
            menuOptions.Add(new MenuOption("Open Directory", OpenDirectory));
            menuOptions.Add(ConsoleFunction.GetHelpOption());
            menuOptions.Add(new MenuOption(GlobalConstants.SelectionOptions.Exit, null));

            return menuOptions;
        }

        [Documentation("Opens the selected directory in the file system.")]
        private static void OpenDirectory()
        {
            Dictionary<Settings.Node, string> paths = Settings.GetValues(x => x.ToString().Contains("Path"));
            paths = paths.Where(x => Directory.Exists(x.Value) || File.Exists(x.Value)).ToDictionary(x => x.Key, x => x.Value);

            if(!paths.Any())
            {
                Utilities.StartProcess(Environment.CurrentDirectory);
                return;
            }

            SelectionPrompt<Settings.Node> prompt = new SelectionPrompt<Settings.Node>();
            prompt.Title = "Select the directory you want to open:";
            prompt.AddChoices(paths.Keys);

            Settings.Node choice = AnsiConsole.Prompt(prompt);
            string path = Settings.GetSetting(choice);
            Utilities.StartProcess(path);
        }
    }
}
