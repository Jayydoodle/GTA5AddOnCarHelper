using CustomSpectreConsole;
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

        private const string DLCFileName = "dlc.rpf";
        private const string DLCListInsertFormatString = "<Item>dlcpacks:{0}/{1}/</Item>";
        private const string DLCListOutputFileName = "GTA5_DLCListGenerator.txt";
        private const string VehicleDirectoryName = "Vehicles";
        private const string TempDirectoryName = "_temp";

        #endregion


        #region Public API

        public override void Run()
        {
            Initialize();
            RunProgramLoop();
        }

        #endregion

        #region Private API

        protected override List<ListOption> GetListOptions()
        {
            List<ListOption> listOptions = new List<ListOption>();
            listOptions.Add(new ListOption("Extract Vehicles From Downloads", ExtractDLCFiles));
            listOptions.Add(new ListOption("Generate DLC List Inserts", GenerateDLCList));
            listOptions.AddRange(base.GetListOptions());

            return listOptions;
        }

        private string GetFilePrefix()
        {
            string prefix = Utilities.GetInput("Enter in the sub-folder path that you want your cars folder to have.  Ex. /cars: ");

            if (!string.IsNullOrEmpty(prefix) && !prefix.StartsWith("/"))
                prefix = "/" + prefix;

            if (prefix.EndsWith("/"))
                prefix = prefix.Substring(0, prefix.Length - 1);

            AnsiConsole.MarkupLine("\nYour inserts will be printed in the format: " + string.Format(DLCListInsertFormatString, string.Format("[red]{0}[/]", prefix), "CAR_NAME_HERE"));

            return prefix;
        }

        private List<string> ExtractFiles()
        {
            List<string> errorMessages = new List<string>();

            DirectoryInfo sourceDir = PathDictionary.GetDirectory(PathDictionary.Node.VehicleDownloadsPath);
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
                "likely some 'replace' cars in your list.  You can try extracting the files on your own and use the" +
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

        private List<string> ExtractDirectories()
        {
            List<string> errorMessages = new List<string>();

            DirectoryInfo sourceDir = PathDictionary.GetDirectory(PathDictionary.Node.VehicleDownloadsPath);
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

        #endregion

        #region Private API: Prompt Functions

        private void ExtractDLCFiles()
        {
            string sourceDirPrompt = "\nEnter the path to the directory containing all of your vehicle downloads: ";

            DirectoryInfo sourceDir = PathDictionary.GetDirectory(PathDictionary.Node.VehicleDownloadsPath, sourceDirPrompt);
            DirectoryInfo vehicledir = new DirectoryInfo(Path.Combine(WorkingDirectory.FullName, VehicleDirectoryName));

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

            List<ListOption<List<string>>> options = new List<ListOption<List<string>>>();
            options.Add(new ListOption<List<string>>("Extract from .zip/.rar/.7z files", ExtractFiles));
            options.Add(new ListOption<List<string>>("Extract from already unarchived folders", ExtractDirectories));
            options.Add(new ListOption<List<string>>(CustomSpectreConsole.Constants.SelectionOptions.ReturnToMenu, () => throw new Exception(CustomSpectreConsole.Constants.Commands.CANCEL)));

            SelectionPrompt<ListOption<List<string>>> prompt = new SelectionPrompt<ListOption<List<string>>>();
            prompt.Title = "Select the type of extraction you want to perform:";
            prompt.AddChoices(options);

            ListOption<List<string>> choice = AnsiConsole.Prompt<ListOption<List<string>>>(prompt);

            DirectoryInfo tempDir = new DirectoryInfo(Path.Combine(WorkingDirectory.FullName, TempDirectoryName));

            if (!tempDir.Exists)
                tempDir = WorkingDirectory.CreateSubdirectory(TempDirectoryName);

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

                if (!files.Any())
                {
                    errorMessages.Add(string.Format("No dlc.rpf files found in the directory [red]{0}[/]\n", Markup.Escape(dir.FullName)));
                    continue;
                }

                foreach (FileInfo file in files)
                {
                    string path = Path.Combine(vehicledir.FullName, file.Directory.Name);
                    DirectoryInfo newDir = new DirectoryInfo(path);

                    if (newDir.Exists)
                        newDir.Delete(true);

                    file.Directory.MoveTo(path);
                }
            }

            tempDir.Delete(true);

            int total = vehicledir.GetDirectories().Count();

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

        private void GenerateDLCList()
        {
            string path = Path.Combine(WorkingDirectory.FullName, VehicleDirectoryName);
            DirectoryInfo vehicledir = new DirectoryInfo(path);

            if (!vehicledir.Exists || !vehicledir.GetDirectories().Any())
            {
                AnsiConsole.MarkupLine("No cars exist in the directory [orange1]{0}[/].  Please run the vehicle extraction tool to continue.", path);
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

            Utilities.WriteToFile(destDir, DLCListOutputFileName, dlcList);
        }

        #endregion
    }
}
