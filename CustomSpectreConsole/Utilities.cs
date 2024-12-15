using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Text.RegularExpressions;
using ZipArchive = SharpCompress.Archives.Zip.ZipArchive;

namespace CustomSpectreConsole
{
    public static class Utilities
    {
        #region Public API: File Management

        public static void StartProcess(string filePath)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
            {
                FileName = filePath,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        public static DirectoryInfo GetOrCreateDirectory(string filePath)
        {
            // If directory does not exist, create it
            if (!Directory.Exists(filePath))
                return Directory.CreateDirectory(filePath);
            else
                return new DirectoryInfo(filePath);
        }

        public static T GetFileSystemInfoFromInput<T>(string prompt, bool isRequired)
        where T : FileSystemInfo
        {
            T info = null;

            while (info == null || !info.Exists)
            {
                string path = GetInput(prompt, x => !string.IsNullOrEmpty(x) || !isRequired);

                if (string.IsNullOrEmpty(path) && !isRequired)
                {
                    info = null;
                    break;
                }

                if (!string.IsNullOrEmpty(path))
                    path = path.Replace("\"", string.Empty);

                info = (T)Activator.CreateInstance(typeof(T), new object[]{ path });

                if (!info.Exists)
                {
                    AnsiConsole.WriteLine("\nThe directory does not exist\n");
                }
            }

            AnsiConsole.WriteLine();

            return info;
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

                AnsiConsole.Write("Content was successfully written to the file: ");
                AnsiConsole.Write(path);
            }
            catch (Exception e)
            {
                if (e.GetType() == typeof(FileNotFoundException))
                    AnsiConsole.MarkupLine("The file [red]{0}[/] does not exist.", fileName);
                else
                    Console.WriteLine(e.ToString());
            }
        }

        public static List<string> ReadTextFromFile(DirectoryInfo dir, string fileName)
        {
            string filePath = Path.Combine(dir.FullName, fileName);
            List<string> textEntries = new List<string>();

            try
            {
                textEntries = File.ReadLines(filePath).ToList();
            }
            catch (Exception e)
            {
                if (e.GetType() == typeof(FileNotFoundException))
                    AnsiConsole.MarkupLine("The file [red]{0}[/] does not exist.", filePath);
                else
                    Console.WriteLine(e.ToString());
            }

            return textEntries;
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

            AnsiConsole.MarkupLine("The previous versions of all files in the directory [orange1]{0}[/]\nhave been archived to [teal]{1}[/]\n", sourceDir.FullName, archiveName);
        }

        public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            var dir = new DirectoryInfo(sourceDir);

            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            DirectoryInfo newDir = Directory.CreateDirectory(Path.Combine(destinationDir, dir.Name));

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(newDir.FullName, file.Name);
                file.CopyTo(targetFilePath);
            }

            if (recursive)
            {
                DirectoryInfo[] dirs = dir.GetDirectories();

                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(newDir.FullName, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }

        public static void CopyFilesToDirectory(DirectoryInfo sourceDir, DirectoryInfo destinationDir, 
        string searchPattern = null, bool allowOverWrite = false,  bool recursive = false, bool writeToConsole = true)
        {
            if (!sourceDir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {sourceDir.FullName}");

            foreach (FileInfo file in sourceDir.GetFiles(searchPattern))
            {
                string targetFilePath = Path.Combine(destinationDir.FullName, file.Name);
                file.CopyTo(targetFilePath, allowOverWrite);
            }

            if (recursive)
            {
                DirectoryInfo[] dirs = sourceDir.GetDirectories();

                foreach (DirectoryInfo subDir in dirs)
                {
                    CopyFilesToDirectory(subDir, destinationDir, searchPattern, allowOverWrite, recursive, false);
                }
            }

            if(!writeToConsole) { return; }

            AnsiConsole.MarkupLine("The [green]{0}[/] files from the source directory: [teal]{1}[/]\nHave been copied to the directory [orange1]{2}[/] successfully\n", 
            searchPattern, sourceDir.FullName, destinationDir.FullName);
        }

        public static void ExtractFile(FileInfo file, string destinationPath, string searchPattern = null)
        {
            switch(file.Extension)
            {
                case GlobalConstants.FileExtension.Zip:
                    ExtractZipFile(file, destinationPath, searchPattern);
                    break;

                case GlobalConstants.FileExtension.Rar:
                    ExtractRarFile(file, destinationPath, searchPattern);
                    break;

                case GlobalConstants.FileExtension.SevenZip:
                    Extract7zFile(file, destinationPath, searchPattern);
                    break;

                default:
                    throw new Exception(String.Format("Could not extract file [red]{0}[/].  Unknown extension '[red]{1}[/]'", file.FullName, file.Extension));
            }
        }

        #endregion

        #region Private API: File Management

        private static void ExtractZipFile(FileInfo file, string desintationPath, string searchPattern = null)
        {
            using (var archive = ZipArchive.Open(file.FullName))
            {
                foreach (var entry in archive.Entries)
                {
                    if (!string.IsNullOrEmpty(searchPattern) && !entry.Key.Contains(searchPattern))
                        continue;

                    entry.WriteToDirectory(desintationPath, new ExtractionOptions()
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });
                }
            }
        }
        private static void ExtractRarFile(FileInfo file, string desintationPath, string searchPattern = null)
        {
            using (var archive = RarArchive.Open(file.FullName))
            {
                foreach (var entry in archive.Entries)
                {
                    if (!string.IsNullOrEmpty(searchPattern) && !entry.Key.Contains(searchPattern))
                        continue;

                    entry.WriteToDirectory(desintationPath, new ExtractionOptions()
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });
                }
            }
        }

        private static void Extract7zFile(FileInfo file, string desintationPath, string searchPattern = null)
        {

            using (var archive = SevenZipArchive.Open(file.FullName))
            {
                foreach (var entry in archive.Entries)
                {
                    if (!string.IsNullOrEmpty(searchPattern) && !entry.Key.Contains(searchPattern))
                        continue;

                    entry.WriteToDirectory(desintationPath, new ExtractionOptions()
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });
                }
            }
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

        public static string GetInput(string message, Func<string, bool> validator = null, string errorMessage = null)
        {
            PromptSettings settings = new PromptSettings();
            settings.Prompt = message;
            settings.Validator = validator;
            settings.ValidationErrorMessage = errorMessage;
            return GetInput(settings);
        }

        public static string GetInput(PromptSettings settings)
        {
            string value = null;

            if(settings == null)
                settings = new PromptSettings();

            TextPrompt<string> prompt = new TextPrompt<string>(settings.Prompt);
            prompt.ValidationErrorMessage = settings.ValidationErrorMessage ?? "\nInvalid Input.\n";
            prompt.AllowEmpty = true;

            if(settings.IsSecret)
                prompt.Secret();

            value = settings.Validator != null ? AnsiConsole.Prompt(prompt.Validate(settings.Validator))
                                      : AnsiConsole.Prompt(prompt);

            string command = value.ToUpper();

            if (command == GlobalConstants.Commands.MENU || command == GlobalConstants.Commands.EXIT || command == GlobalConstants.Commands.CANCEL)
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
                    AnsiConsole.MarkupLine(string.Format("[red]Reminder[/]: You may enter [red bold]{0}[/] at any time to return to the menu\n", GlobalConstants.Commands.CANCEL));

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
            return hash > 0 ? string.Format("0x{0}", hash.ToString("X8")) : string.Empty;
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

        public static List<int> ParseCurrencyFromText(List<string> results)
        {
            IEnumerable<MatchCollection> matches = results.Select(x => Regex.Matches(x, GlobalConstants.RegexPattern.AmericanCurrency, RegexOptions.IgnoreCase));

            List<int> resultGroup = new List<int>();

            foreach (MatchCollection x in matches)
            {
                foreach (Match m in x)
                {
                    string[] values = m.Value.Split(" ");

                    string parsedAmount = new string(values[0].Where(c => char.IsDigit(c) || c == '.').ToArray());

                    if (parsedAmount.EndsWith('.'))
                        parsedAmount = parsedAmount + "0";

                    bool couldParse = double.TryParse(parsedAmount, out double parsedValue);

                    if (!couldParse)
                        continue;

                    if (values.Length == 2)
                    {
                        string modifier = new string(values[1].Where(c => char.IsLetter(c)).ToArray());

                        switch (modifier.ToLower())
                        {
                            case "thousand":
                                parsedValue = parsedValue * 1000;
                                break;

                            case "million":
                                parsedValue = parsedValue * 1000000;
                                break;

                            case "billion":
                                parsedValue = parsedValue * 1000000000;
                                break;
                        }
                    }

                    resultGroup.Add(Convert.ToInt32(parsedValue));
                }
            }

            return resultGroup;
        }

        #endregion
    }
}
