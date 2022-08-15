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
using static GTA5AddOnCarHelper.PremiumDeluxeAutoManager;

namespace GTA5AddOnCarHelper
{
    public sealed class VehicleMetaFileManager : AddOnCarHelperFunctionBase<VehicleMetaFileManager>
    {
        #region Constants

        private const string ErrorDirName = "_ErrorFiles";
        private const string ArchiveDirName = "_Archive";

        #endregion

        #region Properties

        private List<string> ErrorFiles { get; set; }
        private List<string> ErrorMessages { get; set; }
        private List<VehicleMeta> MetaFiles { get; set; }
        public List<string> ModelNames { get; set; } = new List<string>();

        #endregion

        #region Public API

        public override void Run()
        {
            Initialize();
            MetaFiles = null;
            MetaFiles = GetMetaFiles(false);
            RunProgramLoop();
        }

        public List<VehicleMeta> GetMetaFiles(bool vehicleFilesOnly = true)
        {
            if (MetaFiles != null && MetaFiles.Any())
                return MetaFiles;

            ModelNames = new List<string>();
            ErrorMessages = new List<string>();
            ErrorFiles = new List<string>();

            AnsiConsole.MarkupLine("Importing [green]" + Constants.Extentions.Meta + "[/] files\n");

            Dictionary<string, VehicleMeta> metaObjects = GetFiles<VehicleMeta>();

            if (vehicleFilesOnly)
            {
                ViewErrors();
                AnsiConsole.WriteLine();
                return metaObjects.Values.ToList();
            }

            ModelNames = metaObjects.Select(x => x.Value.Model).ToList();

            Dictionary<string, VehicleMetaColor> colorObjects = GetFiles<VehicleMetaColor>();
            Dictionary<string, VehicleMetaHandling> handlingObjects = GetFiles<VehicleMetaHandling>();
            Dictionary<string, VehicleMetaVariation> variationObjects = GetFiles<VehicleMetaVariation>();
            Dictionary<string, VehicleMetaLayout> layoutObjects = GetFiles<VehicleMetaLayout>();

            foreach (KeyValuePair<string, VehicleMeta> meta in metaObjects)
            {
                VehicleMeta v = meta.Value;

                if (colorObjects.ContainsKey(meta.Key))
                {
                    v.Color = colorObjects[meta.Key];
                    colorObjects.Remove(meta.Key);
                }

                if (handlingObjects.ContainsKey(meta.Key))
                {
                    v.Handling = handlingObjects[meta.Key];
                    handlingObjects.Remove(meta.Key);
                }
                else if (handlingObjects.ContainsKey(meta.Value.HandlingId))
                {
                    v.Handling = handlingObjects[meta.Value.HandlingId];
                    handlingObjects.Remove(meta.Value.HandlingId);
                }

                if (variationObjects.ContainsKey(meta.Key))
                {
                    v.Variation = variationObjects[meta.Key];
                    variationObjects.Remove(meta.Key);
                }

                if (layoutObjects.ContainsKey(meta.Key))
                {
                    v.Layout = layoutObjects[meta.Key];
                    layoutObjects.Remove(meta.Key);
                }
            }

            AnsiConsole.WriteLine();

            GenerateUnresolvedModelError(colorObjects.Values, VehicleMetaColor.ModelNode);
            GenerateUnresolvedModelError(handlingObjects.Values, VehicleMetaHandling.ModelNode);
            GenerateUnresolvedModelError(variationObjects.Values, VehicleMetaVariation.ModelNode);
            GenerateUnresolvedModelError(layoutObjects.Values, VehicleMetaLayout.ModelNode);

            ViewErrors();
            AnsiConsole.WriteLine();

            MetaFiles = metaObjects.Values.ToList();
            return MetaFiles;
        }

        #endregion

        #region Private API

        protected override List<ListOption> GetListOptions()
        {
            List<ListOption> listOptions = new List<ListOption>();
            listOptions.Add(new ListOption("Show Meta Files", ShowMetaFiles));
            listOptions.Add(new ListOption("Create Meta File Directory", BuildMetaDirectory));
            listOptions.AddRange(base.GetListOptions());

            return listOptions;
        }

        public Dictionary<string, T> GetFiles<T>()
        where T : VehicleMetaBase, IMetaObject<T>, new()
        {
            Dictionary<string, T> metaFiles = new Dictionary<string, T>();

            string prompt = "Enter the directory that contains the [green]" + Constants.Extentions.Meta + "[/] files: ";
            DirectoryInfo dir = PathDictionary.GetDirectory(PathDictionary.Node.VehicleMetaFilesPath, prompt);
            string fileName = new T().FileName;

            string regexFormat = string.Format("{0}{1}|((:?^|\\s){0}[\\(])", fileName, Constants.Extentions.Meta);

            List<FileInfo> files = dir.GetFiles("*" + Constants.Extentions.Meta, SearchOption.AllDirectories)
                                      .Where(x => Regex.Match(x.Name, regexFormat).Success)
                                      .ToList();
            foreach(FileInfo x in files)
            {
                XMLFile file = XMLFile.Load(x.FullName);
                T meta = default(T);

                if (file != null)
                {
                    meta = T.Create(file);
                }
                else
                {
                    GenerateError(string.Format("The file [red]{0}[/] could not be processed. An error occurred while trying to read it's XML content.\n", x.FullName), x.FullName);
                    continue;
                }

                if(!meta.IsValid)
                {
                    GenerateError(meta.ErrorMessage, meta.XML.SourceFileName);
                }
                else if (metaFiles.ContainsKey(meta.Model.ToLower()))
                {
                    GenerateError(string.Format("The file [red]{0}[/] could not be processed.  A file with a duplicate identifier " +
                        "[orange1]{1}[/] has already been added to the list.\n", x.FullName, meta.Model.ToLower()), x.FullName);
                }
                else
                {
                    meta.SourceFilePath = x.FullName;
                    metaFiles.Add(meta.Model.ToLower(), meta);
                }
            };

            if (!metaFiles.Any())
                AnsiConsole.MarkupLine("No [orange1]" + fileName + Constants.Extentions.Meta + "[/] files were found in the specified directory");
            else
                AnsiConsole.MarkupLine("[green]" + fileName + Constants.Extentions.Meta + "[/] files were imported successfully!");

            return metaFiles;
        }

        #endregion

        #region Private API: Prompt Functions

        private void ShowMetaFiles()
        {
            ListFilter<VehicleMeta> filter = new ListFilter<VehicleMeta>(MetaFiles.OrderBy(x => x.Model));
            TableDisplay.BuildDisplay<VehicleMeta>(filter);
        }

        private void BuildMetaDirectory()
        {
            DirectoryInfo archiveDir = WorkingDirectory.CreateSubdirectory(ArchiveDirName);
            Utilities.ArchiveDirectory(WorkingDirectory, archiveDir, true);

            List<PropertyInfo> props = typeof(VehicleMeta).GetProperties()
                                        .Where(x => x.PropertyType.IsSubclassOf(typeof(VehicleMetaBase)))
                                        .ToList();
            int dirCount = 0;

            MetaFiles.ForEach(x =>
            {
                DirectoryInfo dir = WorkingDirectory.CreateSubdirectory(x.Model);
                x.XML.Save(Path.Combine(dir.FullName, string.Format("{0}{1}", x.FileName, Constants.Extentions.Meta)));

                props.ForEach(prop =>
                {
                    if (prop.GetValue(x) is VehicleMetaBase obj)
                        obj.XML.Save(Path.Combine(dir.FullName, string.Format("{0}{1}", obj.FileName, Constants.Extentions.Meta)));
                });

                dirCount++;
            });

            DirectoryInfo metaFilesDir = PathDictionary.GetDirectory(PathDictionary.Node.VehicleMetaFilesPath);
            AnsiConsole.MarkupLine("A total of [green]{0}[/] directories have been created for each car found in the source directory [green]{1}[/]", dirCount, metaFilesDir.FullName);

            if (!ErrorFiles.Any())
                return;
            List<FileInfo> errorFiles = metaFilesDir.GetFiles().Where(x => ErrorFiles.Contains(x.FullName)).ToList();

            if (!errorFiles.Any())
                return;

            DirectoryInfo errorFileDir = WorkingDirectory.CreateSubdirectory(ErrorDirName);

            errorFiles.ForEach(x =>
            {
                string fileName = x.Name.Replace("(", string.Empty).Replace(")", string.Empty);
                x.CopyTo(Path.Combine(errorFileDir.FullName, fileName));
            });

            AnsiConsole.MarkupLine("A total of [red]{0}[/] files could not matched to a specific vehicle and have been moved to [red]{1}[/]", errorFiles.Count(), errorFileDir.FullName);

            bool showErrors = Utilities.GetConfirmation("\nWould you like to view the list of errors?");

            if (showErrors)
            {
                AnsiConsole.WriteLine();
                ErrorMessages.ForEach(x => AnsiConsole.MarkupLine(x));
            }
        }

        #endregion

        #region Private API: Error Handling

        private void GenerateUnresolvedModelError(IEnumerable<VehicleMetaBase> objs, string nodeName)
        {
            foreach (VehicleMetaBase obj in objs)
            {
                GenerateError(string.Format("Could not parse the XML from file [red]{0}[/]. It's [blue]<{1}>[/] attribute has a " +
                    "value of [green]{2}[/] which could not be resolved against a vehicle.meta model\n", obj.SourceFilePath, nodeName, obj.Model), obj.SourceFilePath);
            }
        }

        private void GenerateError(string message, string fileName)
        {
            ErrorFiles.Add(fileName);
            ErrorMessages.Add(message);
        }

        private void ViewErrors()
        {
            if (ErrorMessages.Any())
            {
                bool confirmation = Utilities.GetConfirmation("Some errors occurred during the import.  Would you like to view them?");

                if(confirmation)
                    ErrorMessages.ForEach(x => AnsiConsole.MarkupLine(x));
            }
        }

        #endregion
    }
}
