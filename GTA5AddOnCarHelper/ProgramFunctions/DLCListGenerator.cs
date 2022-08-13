using CustomSpectreConsole;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTA5AddOnCarHelper
{
    public sealed class DLCListGenerator : AddOnCarHelperFunctionBase
    {
        #region Constants

        private const string OutputFileName = "GTA5_DLCListGenerator.txt";
        private const string InsertFormatString = "<Item>dlcpacks:{0}/{1}/</Item>";

        #endregion

        #region Properties

        public override string DisplayName => nameof(DLCListGenerator).SplitByCase();
        protected override string WorkingDirectoryName => null;

        private static readonly Lazy<DLCListGenerator> _instance = new Lazy<DLCListGenerator>(() => new DLCListGenerator());

        public static DLCListGenerator Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        #endregion

        #region Constructor

        private DLCListGenerator()
        {
        }

        #endregion

        #region Public API

        public override void Run()
        {
            Initialize();

            string sourceDirPrompt = "\nEnter the path to the source directory.  This is the directory containing your cars in " +
                "individual folders as they would appear in the mods\\update\\x64\\dlcpacks folder: ";

            DirectoryInfo sourceDir = PathDictionary.GetDirectory(PathDictionary.Node.DLCListFilesPath, sourceDirPrompt);
            DirectoryInfo destDir = PathDictionary.GetDirectory(PathDictionary.Node.WorkingDirectoryPath);

            string filePrefix = Utilities.GetActionApprovalInput<string>(GetFilePrefix);

            List<DirectoryInfo> carDirs = sourceDir.GetDirectories().OrderBy(x => x.Name).ToList();
            StringBuilder dlcList = new StringBuilder();

            carDirs.ForEach(x =>
            {
                string insert = string.Format(InsertFormatString, filePrefix, x.Name);
                dlcList.AppendLine(insert);
                Console.WriteLine(insert);
            });

            Utilities.WriteToFile(destDir, OutputFileName, dlcList);
            Utilities.GetInput(string.Format("Press enter to return to the main menu, or enter [bold red]{0}[/] to exit", CustomSpectreConsole.Constants.Commands.EXIT));
        }

        #endregion

        #region Private API

        private string GetFilePrefix()
        {
            string prefix = Utilities.GetInput("Enter in the sub-folder path that you want your cars folder to have.  Ex. /cars: ");

            if (!string.IsNullOrEmpty(prefix) && !prefix.StartsWith("/"))
                prefix = "/" + prefix;

            if (prefix.EndsWith("/"))
                prefix = prefix.Substring(0, prefix.Length - 1);

            AnsiConsole.MarkupLine("\nYour inserts will be printed in the format: " + string.Format(InsertFormatString, string.Format("[red]{0}[/]", prefix), "CAR_NAME_HERE"));

            return prefix;
        }

        #endregion
    }
}
