﻿using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Text;

namespace CustomSpectreConsole
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

        public static void ArchiveFiles(DirectoryInfo dir, string searchPattern, List<string> fileNames = null)
        {
            List<FileInfo> files = dir.GetFiles(searchPattern).ToList();

            if (fileNames != null)
                files = files.Where(x => fileNames.Any(y => x.Name == y)).ToList();

            if (!files.Any())
                return;

            DirectoryInfo archive = dir.CreateSubdirectory(string.Format("_Archive__{0}", DateTime.Now.ToString("yyyy-MM-dd__h-mm-ss-tt")));

            files.ForEach(file => file.MoveTo(Path.Combine(archive.FullName, file.Name)));

            AnsiConsole.MarkupLine("The previous versions of the [red]{0}[/] files in the directory [red]{1}[/]\nhave been archived to [teal]{2}[/]\n",
                                  searchPattern.Replace("*", string.Empty), dir.Name, archive.Name);
        }

        public static void ArchiveDirectory(DirectoryInfo sourceDir, DirectoryInfo destinationDir = null, bool zip = false)
        {
            List<DirectoryInfo> directories = sourceDir.GetDirectories().Where(x => !(x.Name.StartsWith("_Archive"))).ToList();
            List<FileInfo> files = sourceDir.GetFiles().Where(x => !(x.Name.StartsWith("_Archive"))).ToList(); 

            if(!directories.Any() && !files.Any())
                return;

            if (destinationDir == null)
                destinationDir = sourceDir;

            DirectoryInfo archive = destinationDir.CreateSubdirectory(string.Format("_Archive__{0}", DateTime.Now.ToString("yyyy-MM-dd__h-mm-ss-tt")));

            directories.ForEach(directory => directory.MoveTo(Path.Combine(archive.FullName, directory.Name)));
            files.ForEach(file => file.MoveTo(Path.Combine(archive.FullName, file.Name)));

            string archiveName = archive.FullName;

            if (zip)
            {
                ZipFile.CreateFromDirectory(archive.FullName, string.Format("{0}{1}", archive.FullName, ".zip"));
                archive.Delete(true);
                archiveName = string.Format("{0}{1}", archiveName, ".zip");
            }

            AnsiConsole.MarkupLine("The previous versions of all files in the directory [red]{0}[/]\nhave been archived to [teal]{1}[/]\n", sourceDir.FullName, archiveName);
        }

        #endregion

        #region Public API: Input

        public static Dictionary<string, object> GetEditInput<T>(T item, List<PropertyInfo> props = null)
        {
            Dictionary<string, object> enteredValues = new Dictionary<string, object>();

            if (props == null)
                props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();

            AnsiConsole.MarkupLine("Press [green]ENTER[/] to skip editing the current field.\n");

            foreach (PropertyInfo prop in props)
            {
                if (item != null)
                {
                    object currentValue = prop.GetValue(item);
                    AnsiConsole.WriteLine(string.Format("Current {0}: {1}", prop.Name, currentValue));
                }

                string input = Utilities.GetInput(string.Format("{0}:", prop.Name));

                bool isNumeric = int.TryParse(input, out int numValue);

                if (isNumeric && prop.PropertyType == typeof(string)
                    || !isNumeric && prop.PropertyType == typeof(int) && input != string.Empty)
                {
                    string message = prop.PropertyType == typeof(string) ? "Input cannot be a number" : "Input must be a number";
                    AnsiConsole.WriteLine(message);
                    continue;
                };

                if (isNumeric && numValue < 0)
                {
                    AnsiConsole.WriteLine("Number must be greater than 0");
                    continue;
                }

                object value = isNumeric ? numValue : !string.IsNullOrEmpty(input) ? input : null;

                if (value != null)
                    enteredValues.Add(prop.Name, value);
            }

            return enteredValues;
        }

        public static string GetInput(string message, Func<string, bool> validator = null)
        {
            string value = null;

            TextPrompt<string> prompt = new TextPrompt<string>(message);
            prompt.ValidationErrorMessage = "\nInvalid Input.\n";
            prompt.AllowEmpty = true;

            value = validator != null ? AnsiConsole.Prompt(prompt.Validate(validator))
                                      : AnsiConsole.Prompt(prompt);

            string command = value.ToUpper();

            if (command == Constants.Commands.MENU || command == Constants.Commands.EXIT || command == Constants.Commands.CANCEL)
                throw new Exception(command);

            return value.Trim();
        }

        public static T GetActionApprovalInput<T>(Func<T> function, string message = "Would you like to proceed?")
        {
            bool confirmed = false;
            T item = default(T);
            int iteration = 0;

            while (!confirmed)
            {
                iteration++;

                if (iteration % 2 == 0)
                    AnsiConsole.MarkupLine(string.Format("[red]Reminder[/]: You may enter [red bold]{0}[/] at any time to return to the menu\n", Constants.Commands.CANCEL));

                item = function();

                AnsiConsole.WriteLine();
                confirmed = GetConfirmation(message);

                AnsiConsole.WriteLine();
            }

            return item;
        }

        public static bool GetConfirmation(string message)
        {
            string input = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                    .Title(message)
                    .AddChoices("Yes", "No"));

            return input == "Yes";
        }

        #endregion

        #region Public API : Util

        public static string GetHash(string input)
        {
            uint hash = CreateHash(input.ToLower());
            return string.Format("0x{0}", hash.ToString("X8"));
        }

        public static uint CreateHash(string input)
        {
            uint num = 0;
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            for (int i = 0; i < bytes.Length; i++)
            {
                num += bytes[i];
                num += num << 10;
                num ^= num >> 6;
            }
            num += num << 3;
            num ^= num >> 11;
            return (num + (num << 15));
        }

        #endregion
    }
}
