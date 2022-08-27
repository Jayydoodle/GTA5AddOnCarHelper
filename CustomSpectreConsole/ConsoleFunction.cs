using CustomSpectreConsole;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomSpectreConsole
{
    public abstract class ConsoleFunction : ListOption
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

            AnsiConsole.MarkupLine(string.Format("Enter [bold red]{0}[/] at any time to return to the main menu.", Constants.Commands.MENU));
            AnsiConsole.MarkupLine(string.Format("Enter [bold red]{0}[/] at any time to end the current operation and return to the {1} menu.", Constants.Commands.CANCEL, DisplayName));
            AnsiConsole.MarkupLine(string.Format("Enter [bold red]{0}[/] at any time to quit.", Constants.Commands.EXIT));
            AnsiConsole.Write("\n\n");
        }

        #endregion

        #region Protected API

        protected void RunProgramLoop()
        {
            SelectionPrompt<ListOption> prompt = new SelectionPrompt<ListOption>();
            prompt.Title = "Select an option:";
            prompt.AddChoices(GetListOptions());

            while (true)
            {
                ListOption option = AnsiConsole.Prompt(prompt);

                if (option.Function != null)
                {
                    try
                    {
                        option.Function();
                    }
                    catch (Exception e)
                    {
                        if (e.Message != Constants.Commands.CANCEL)
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

        protected virtual List<ListOption> GetListOptions()
        {
            List<ListOption> listOptions = new List<ListOption>();
            listOptions.Add(new ListOption(Constants.SelectionOptions.ReturnToMainMenu, () => throw new Exception(CustomSpectreConsole.Constants.Commands.MENU)));

            return listOptions;
        }

        #endregion
    }
}
