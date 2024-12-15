using CustomSpectreConsole;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CustomSpectreConsole
{
    public abstract class ConsoleFunction : MenuOption
    {
        #region Base Class Overrides

        public abstract override string DisplayName { get; }
        public override Action Function { get => Run; }

        #endregion

        #region Public API

        public abstract void Run();

        public void WriteHeaderToConsole()
        {
            AnsiConsole.Clear();

            Rule rule = new Rule(string.Format("[red]{0}[/]", DisplayName));
            rule.Alignment = Justify.Left;
            AnsiConsole.Write(rule);
            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine(string.Format("Enter [bold red]{0}[/] at any time to return to the main menu.", GlobalConstants.Commands.MENU));
            AnsiConsole.MarkupLine(string.Format("Enter [bold red]{0}[/] at any time to end the current operation and return to the {1} menu.", GlobalConstants.Commands.CANCEL, DisplayName));
            AnsiConsole.MarkupLine(string.Format("Enter [bold red]{0}[/] at any time to quit.", GlobalConstants.Commands.EXIT));
            AnsiConsole.Write("\n\n");
        }

        #endregion

        #region Protected API

        protected void RunProgramLoop()
        {
            SelectionPrompt<MenuOption> prompt = new SelectionPrompt<MenuOption>();
            prompt.Title = "Select an option:";
            List<MenuOption> options = GetMenuOptions();

            if (options.Any(x => x.Function != null && x.Function.Method.HasAttribute<DocumentationAttribute>()))
                options.Add(GetHelpOption());

            options.Add(new MenuOption(GlobalConstants.SelectionOptions.ReturnToMainMenu, () => throw new Exception(GlobalConstants.Commands.MENU)));

            prompt.AddChoices(options);

            while (true)
            {
                MenuOption option = AnsiConsole.Prompt(prompt);

                if (option.Function != null || option.IsHelpOption)
                {
                    try
                    {
                        if (option.IsHelpOption)
                            ((MenuOption<List<MenuOption>, bool>)option).Function(options);
                        else
                            option.Function();
                    }
                    catch (Exception e)
                    {
                        if (e.Message != GlobalConstants.Commands.CANCEL)
                            throw;
                        else
                            WriteHeaderToConsole();
                    }
                }
                else
                {
                    break;
                }

                AnsiConsole.Write("\n\n");
            }
        }

        protected abstract List<MenuOption> GetMenuOptions();
        
        public static MenuOption GetHelpOption()
        {
            return new MenuOption<List<MenuOption>, bool>(GlobalConstants.SelectionOptions.Help, PrintHelpText);
        }

        #endregion

        #region Private API

        private static bool PrintHelpText(List<MenuOption> options)
        {
            Rule rule = new Rule("[pink1]Help[/]");
            rule.RuleStyle("blue");
            AnsiConsole.Write(rule);
            AnsiConsole.WriteLine();

            options.ForEach(x =>
            {
                if (x.Function != null && x.Function.Method.HasAttribute<DocumentationAttribute>())
                {
                    AnsiConsole.MarkupLine("[green]{0}[/]", x.DisplayName);

                    DocumentationAttribute attr = x.Function.Method.GetCustomAttribute(typeof(DocumentationAttribute)) as DocumentationAttribute;
                    AnsiConsole.MarkupLine(attr.Summary);
                    AnsiConsole.WriteLine();
                }
            });

            rule = new Rule();
            rule.RuleStyle("blue");
            AnsiConsole.Write(rule);
            AnsiConsole.WriteLine();

            return true;
        }

        #endregion
    }
}
