﻿using CustomSpectreConsole;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static GTA5AddOnCarHelper.PremiumDeluxeAutoManager;

namespace GTA5AddOnCarHelper
{
    public sealed class DLCExtractor : AddOnCarHelperFunctionBase<DLCExtractor>
    {
        #region Constants

        public const string DLCFileName = "dlc.rpf";
        private const string DLCListInsertFormatString = "<Item>dlcpacks:{0}/{1}/</Item>";
        private const string DLCListOutputFileName = "GTA5_DLCListGenerator.txt";
        private const string VehicleDirectoryName = "Vehicles";
        private const string OpenIVInstallersDirectoryName = "OpenIVInstallers";
        private const string OpenIVInstallerSearchPattern = "*.oiv";
        private const string TempDirectoryName = "_temp";

        #endregion

        #region Public API

        [Documentation(Summary)]
        public override void Run()
        {
            Initialize();
            RunProgramLoop();
        }

        #endregion

        #region Private API

        protected override List<MenuOption> GetMenuOptions()
        {
            List<MenuOption> menuOptions = new List<MenuOption>();
            menuOptions.Add(new MenuOption("Extract Vehicles From Downloads", ExtractDLCFiles));
            menuOptions.Add(new MenuOption("Generate DLC List Inserts", GenerateDLCList));
            menuOptions.AddRange(base.GetMenuOptions());

            return menuOptions;
        }

        private string GetFilePrefix()
        {
            string prefix = Utilities.GetInput("Enter in the sub-folder path that you want your vehicles folder to have.  Ex. /cars: ");

            if (!string.IsNullOrEmpty(prefix) && !prefix.StartsWith("/"))
                prefix = "/" + prefix;

            if (prefix.EndsWith("/"))
                prefix = prefix.Substring(0, prefix.Length - 1);

            AnsiConsole.MarkupLine("\nYour inserts will be printed in the format: " + string.Format(DLCListInsertFormatString, string.Format("[red]{0}[/]", prefix), "VEHICLE_NAME_HERE"));

            return prefix;
        }

        public List<string> ExtractFiles()
        {
            List<string> errorMessages = new List<string>();

            DirectoryInfo sourceDir = Settings.GetDirectory(Settings.Node.VehicleDownloadsPath);
            DirectoryInfo tempDir = new DirectoryInfo(Path.Combine(WorkingDirectory.FullName, TempDirectoryName));

            FileInfo[] sourceFiles = sourceDir.GetFiles();
            int fileCount = sourceFiles.Count();

            if (fileCount == 0)
            {
                errorMessages.Add(string.Format("No files were found in the directory [orange1]{0}[/]", sourceDir.FullName));
                return errorMessages;
            }

            ProgressColumn[] columns = new ProgressColumn[]
            {
                new TaskDescriptionColumn(), new ProgressBarColumn(),
                new PercentageColumn(), new RemainingTimeColumn()
            };

            AnsiConsole.MarkupLine("[pink1]Please Be Advised...[/]\nWhen performing this type of extraction, " +
                "files that do not contain [green]{0}[/] files are automatically filtered for faster extraction times.  If the " +
                "total number of extracted vehicles at the end of the process does not match your expected total, there are " +
                "likely some 'replace' vehicles in your list.  You can try extracting the files on your own and use the" +
                "[blue]'Extract from already unarchived folders'[/] option to get " +
                "a more verbose list of vehicles that have been filtered out.", DLCFileName);

            AnsiConsole.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(columns)
            .Start(ctx =>
            {
                var task = ctx.AddTask("[green]Extracting Files[/]", true, fileCount);

                while (!ctx.IsFinished)
                {
                    foreach (FileInfo file in sourceFiles)
                    {
                        try { Utilities.ExtractFile(file, tempDir.FullName, DLCFileName); }
                        catch (Exception e) { errorMessages.Add(e.Message); }

                        task.Increment(1);
                        ctx.Refresh();
                    }

                    task.StopTask();
                }
            });

            return errorMessages;
        }

        public List<string> ExtractDirectories()
        {
            List<string> errorMessages = new List<string>();

            DirectoryInfo sourceDir = Settings.GetDirectory(Settings.Node.VehicleDownloadsPath);
            DirectoryInfo tempDir = new DirectoryInfo(Path.Combine(WorkingDirectory.FullName, TempDirectoryName));

            DirectoryInfo[] sourceDirs = sourceDir.GetDirectories();
            int dirCount = sourceDirs.Count();

            if (dirCount == 0)
            {
                errorMessages.Add(string.Format("No directories were found in the directory [orange1]{0}[/]", sourceDir.FullName));
                return errorMessages;
            }

            ProgressColumn[] columns = new ProgressColumn[]
            {
                new TaskDescriptionColumn(), new ProgressBarColumn(),
                new PercentageColumn(), new RemainingTimeColumn()
            };

            AnsiConsole.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(columns)
            .Start(ctx =>
            {
                var task = ctx.AddTask("[green]Extracting Directories[/]", true, dirCount);

                while (!ctx.IsFinished)
                {
                    foreach (DirectoryInfo dir in sourceDirs)
                    {
                        try { Utilities.CopyDirectory(dir.FullName, tempDir.FullName, true); }
                        catch (Exception e) { errorMessages.Add(e.Message); }

                        task.Increment(1);
                        ctx.Refresh();
                    }

                    task.StopTask();
                }
            });

            return errorMessages;
        }

        public DirectoryInfo EnsureTempDirectory()
        {
            DirectoryInfo tempDir = new DirectoryInfo(Path.Combine(WorkingDirectory.FullName, TempDirectoryName));

            if (tempDir.Exists)
                tempDir.Delete(true);

            tempDir = WorkingDirectory.CreateSubdirectory(TempDirectoryName);

            return tempDir;
        }

        #endregion

        #region Private API: Prompt Functions

        [Documentation(ExtractDLCFilesSummary)]
        private void ExtractDLCFiles()
        {
            DirectoryInfo sourceDir = Settings.GetDirectory(Settings.Node.VehicleDownloadsPath);
            DirectoryInfo vehicledir = new DirectoryInfo(Path.Combine(WorkingDirectory.FullName, VehicleDirectoryName));
            DirectoryInfo openIVDir = new DirectoryInfo(Path.Combine(WorkingDirectory.FullName, OpenIVInstallersDirectoryName));

            if (vehicledir.Exists && vehicledir.GetDirectories().Any()) 
            {
                string message = string.Format("[orange1]WARNING:[/] This process will overwrite any folders in the [blue]{0}[/] folder " +
                    "that match any data found during the extraction.  Would you like to create a backup of the existing files before extracting new files?", vehicledir.Name);

                bool shouldArchive = Utilities.GetConfirmation(message);

                if (shouldArchive)
                    Utilities.ArchiveDirectory(WorkingDirectory);
            }

            if (!vehicledir.Exists)
                vehicledir = WorkingDirectory.CreateSubdirectory(VehicleDirectoryName);

            List<MenuOption<List<string>>> options = new List<MenuOption<List<string>>>();
            options.Add(new MenuOption<List<string>>("Extract from .zip/.rar/.7z files", ExtractFiles));
            options.Add(new MenuOption<List<string>>("Extract from already unarchived folders", ExtractDirectories));
            options.Add(MenuOption<List<string>>.CancelOption(GlobalConstants.SelectionOptions.ReturnToMenu));

            SelectionPrompt<MenuOption<List<string>>> prompt = new SelectionPrompt<MenuOption<List<string>>>();
            prompt.Title = "Select the type of extraction you want to perform:";
            prompt.AddChoices(options);

            MenuOption<List<string>> choice = AnsiConsole.Prompt<MenuOption<List<string>>>(prompt);

            DirectoryInfo tempDir = EnsureTempDirectory();

            List<string> errorMessages = choice.Function();

            if (!tempDir.GetDirectories().Any())
            {
                tempDir.Delete(true);
                errorMessages.ForEach(x => AnsiConsole.MarkupLine(x));
                return;
            }

            foreach (DirectoryInfo dir in tempDir.GetDirectories())
            {
                FileInfo[] files = dir.GetFiles(DLCFileName, SearchOption.AllDirectories);
                bool foundOpenIVFiles = false;

                if (!files.Any())
                {
                    files = dir.GetFiles(OpenIVInstallerSearchPattern, SearchOption.AllDirectories);

                    if (files.Any())
                    {
                        if (!openIVDir.Exists)
                            openIVDir = WorkingDirectory.CreateSubdirectory(OpenIVInstallersDirectoryName);

                        foundOpenIVFiles = true;
                    }
                    else
                    {
                        errorMessages.Add(string.Format("No dlc.rpf files found in the directory [red]{0}[/]\n", Markup.Escape(dir.FullName)));
                        continue;
                    }
                }

                string destDir = foundOpenIVFiles ? openIVDir.FullName : vehicledir.FullName;

                foreach (FileInfo file in files)
                {
                    string path = Path.Combine(destDir, file.Directory.Name);
                    DirectoryInfo newDir = new DirectoryInfo(path);

                    if (newDir.Exists)
                        newDir.Delete(true);

                    file.Directory.MoveTo(path);
                }
            }

            tempDir.Delete(true);

            int total = vehicledir.GetDirectories().Count();
            int openIVInstallerTotal = openIVDir.Exists ? openIVDir.GetDirectories().Count() : 0;

            if (total > 0)
                AnsiConsole.MarkupLine("Extraction complete. [green]{0}[/] vehicles have been extracted to the folder\n[blue]{1}[/]", total, vehicledir.FullName);
            else
                AnsiConsole.WriteLine("Extraction was unsuccessful.");

            if (errorMessages.Any())
            {
                bool confirmation = Utilities.GetConfirmation(string.Format("\nA total of [red]{0}[/] errors occurred while processing.  Would you like to view them?", errorMessages.Count));

                if (confirmation)
                    errorMessages.ForEach(x => AnsiConsole.MarkupLine(x));
            }
        }

        [Documentation(GenerateDLCListSummary)]
        private void GenerateDLCList()
        {
            string path = Path.Combine(WorkingDirectory.FullName, VehicleDirectoryName);
            DirectoryInfo vehicledir = new DirectoryInfo(path);

            if (!vehicledir.Exists || !vehicledir.GetDirectories().Any())
            {
                AnsiConsole.MarkupLine("No vehicles exist in the directory [orange1]{0}[/].  Please run the vehicle extraction tool to continue.", path);
                return;
            }

            DirectoryInfo destDir = WorkingDirectory;

            string filePrefix = Utilities.GetActionApprovalInput<string>(GetFilePrefix);

            List<DirectoryInfo> carDirs = vehicledir.GetDirectories().OrderBy(x => x.Name).ToList();
            StringBuilder dlcList = new StringBuilder();

            carDirs.ForEach(x =>
            {
                string insert = string.Format(DLCListInsertFormatString, filePrefix, x.Name);
                dlcList.AppendLine(insert);
                AnsiConsole.WriteLine(insert);
            });

            Console.WriteLine();
            Utilities.WriteToFile(destDir, DLCListOutputFileName, dlcList);
        }

        #endregion

        #region Documentation

        private const string Summary = "A utility that extracts dlc.rpf files from compressed (.zip/.rar/.7zip) vehicle download files, formats them " +
        "into subfolders for easy insertion into the [orange1]mods/update/x64/dlcpacks[/] folder, and generates inserts for the [orange1]mods/update/update.rpf/common/data/dlclist.xml[/] file";

        private const string ExtractDLCFilesSummary = "Takes a folder containing vehicles downloaded from the gta5-mods site and extracts all of the folders containing the dlc.rpf file. " +
        "Extraction can be performed either directly on the compressed (.zip/.rar/.7zip) files, or you can pre-unzip all the files yourself and have the extraction " +
        "be performed on all of the unarchived folders.  These folders can then be copied into the [orange1]mods/update/x64/dlcpacks[/] folder";

        private const string GenerateDLCListSummary = "Takes all of the folders generated from 'Extract Vehicles From Downloads' option and generates a list of inserts for them " +
        "that can be pasted into the [orange1]mods/update/update.rpf/common/data/dlclist.xml[/] file";

        #endregion
    }
}
