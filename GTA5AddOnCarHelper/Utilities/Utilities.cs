using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GTA5AddOnCarHelper
{
    public static class Utilities
    {
        #region Public API: File Management

        public static DirectoryInfo GetDirectoryFromInput(string prompt, bool isRequired)
        {
            DirectoryInfo dir = null;

            while (dir == null || !dir.Exists)
            {
                string path = GetInput(prompt, x => !string.IsNullOrEmpty(x) || !isRequired);

                if (string.IsNullOrEmpty(path) && !isRequired)
                {
                    dir = null;
                    break;
                }

                dir = new DirectoryInfo(path);

                if (!dir.Exists)
                {
                    AnsiConsole.WriteLine("\nThe directory does not exist\n");
                }
            }

            AnsiConsole.WriteLine();

            return dir;
        }

        public static void WriteToFile(DirectoryInfo dir, string outputFileName, StringBuilder content)
        {
            if (dir == null)
                return;

            string fileName = Path.Combine(dir.FullName, outputFileName);

            try
            {
                File.WriteAllText(fileName, content.ToString());
                TextPath path = new TextPath(fileName);
                path.LeafColor(Color.Green);

                AnsiConsole.Write("\nContent was successfully written to the file: ");
                AnsiConsole.Write(path);
                AnsiConsole.WriteLine();
            }
            catch (Exception Ex)
            {
                Console.WriteLine(Ex.ToString());
            }
        }

        public static void ArchiveFiles(DirectoryInfo dir, string searchPattern)
        {
            List<FileInfo> files = dir.GetFiles(searchPattern).ToList();

            if (!files.Any())
                return;

            DirectoryInfo archive = dir.CreateSubdirectory(string.Format("Archive__{0}", DateTime.Now.ToString("yyyy-MM-dd__h-mm-ss-tt")));
            files.ForEach(file =>
            {
                file.MoveTo(Path.Combine(archive.FullName, file.Name));
            });

            AnsiConsole.MarkupLine("The previous versions of the [red]{0}[/] files in the directory [red]{1}[/] inside the working directory\nhave been archived to [teal]{2}[/]",
                                  searchPattern.Replace("*", string.Empty), dir.Name, archive.Name);
        }

        #endregion

        #region Public API: Input

        public static string GetInput(string message, Func<string, bool> validator = null)
        {
            string value = null;

            TextPrompt<string> prompt = new TextPrompt<string>(message);
            prompt.ValidationErrorMessage = "\nInvalid Input.\n";
            prompt.AllowEmpty = true;

            value = validator != null ? AnsiConsole.Prompt(prompt.Validate(validator))
                                      : AnsiConsole.Prompt(prompt);

            string command = value.ToUpper();

            if (command == Command.MENU || command == Command.EXIT || command == Command.CANCEL)
                throw new Exception(command);

            return value;
        }

        public static T GetActionApprovalInput<T>(Func<T> function, string message = "Would you like to proceed?")
        {
            string input = string.Empty;
            T item = default(T);
            int iteration = 0;

            while (!string.Equals(input, "Yes"))
            {
                iteration++;

                if (iteration % 2 == 0)
                    AnsiConsole.MarkupLine(string.Format("[red]Reminder[/]: You may enter [red bold]{0}[/] at any time to return to the menu\n", Command.CANCEL));

                item = function();

                AnsiConsole.WriteLine();

                input = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                    .Title(message)
                    .AddChoices("Yes", "No")
                );

                AnsiConsole.WriteLine();
            }

            return item;
        }

        #endregion
    }

    public class Command
    {
        public const string CANCEL = "CANCEL";
        public const string MENU = "MENU";
        public const string EXIT = "EXIT";
    }
}
