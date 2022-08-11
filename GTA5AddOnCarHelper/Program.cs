﻿using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GTA5AddOnCarHelper
{
    class Program
    {
        static void Main(string[] args)
        {
            SelectionPrompt<ListOption> prompt = new SelectionPrompt<ListOption>();
            prompt.Title = "Select an option:";
            prompt.AddChoices(CreateListOptions());

            while(true)
            {
                Rule rule = new Rule("[green]GTA 5 AddOn Car Helper[/]\n").DoubleBorder<Rule>();
                AnsiConsole.Write(rule);

                Dictionary<PathDictionary.Node, string> paths = PathDictionary.GetPaths();

                if(paths.Any())
                {
                    string message = string.Format("The paths that will be used during execution of this program are defined below." +
                        "  To change any of the values, remove the corresponding entry in the [green]{0}[/] file\n\n", PathDictionary.FileName);

                    AnsiConsole.Markup(message);

                    Enum.GetValues<PathDictionary.Node>()
                    .ToList()
                    .ForEach(x =>
                    {
                        paths.TryGetValue(x, out string value);

                        if (!string.IsNullOrEmpty(value))
                            AnsiConsole.MarkupLine(string.Format("[green]{0}[/]: {1}", x, value));
                    });

                    rule = new Rule();
                    AnsiConsole.Write(rule);
                    AnsiConsole.Write("\n\n");
                }

                ListOption option = AnsiConsole.Prompt(prompt);

                if (option.Function != null)
                {
                    try
                    {
                        if(option is ProgramFunctionBase)
                            ((ProgramFunctionBase)option).WriteHeaderToConsole();

                        option.Function();
                        AnsiConsole.Clear();
                    }
                    catch (Exception e)
                    {
                        if (e.Message == Command.EXIT)
                            break;
                        else 

                        AnsiConsole.Clear();

                        if (e.Message != Command.MENU)
                            AnsiConsole.Write(string.Format("{0}\n\n", e.Message));
                    }
                }
                else
                {
                    break;
                }
            }
        }

        private static List<ListOption> CreateListOptions()
        {
            List<ListOption> listOptions = new List<ListOption>();

            listOptions.Add(DLCListGenerator.Instance);
            listOptions.Add(PremiumDeluxeAutoManager.Instance);
            listOptions.Add(new ListOption("Exit", null));

            return listOptions;
        }
    }
}
