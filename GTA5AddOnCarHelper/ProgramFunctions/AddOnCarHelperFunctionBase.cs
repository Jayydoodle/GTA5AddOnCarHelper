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
    public abstract class AddOnCarHelperFunctionBase : ConsoleFunction
    { }

    public abstract class AddOnCarHelperFunctionBase<T> : AddOnCarHelperFunctionBase
     where T : new()
    {
        #region Properties

        public override string DisplayName => typeof(T).Name.SplitByCase();
        protected string WorkingDirectoryName => typeof(T).Name;

        private static readonly Lazy<T> _instance = new Lazy<T>(() => new T());
        public static T Instance => _instance.Value;

        private DirectoryInfo _workingDirectory;
        public DirectoryInfo WorkingDirectory
        {
            get
            {
                if (_workingDirectory == null)
                    _workingDirectory = EnsureWorkingDirectory();

                return _workingDirectory;
            }
            private set
            {
                _workingDirectory = value;
            }
        }

        #endregion

        #region Protected API

        protected void Initialize()
        {
            WorkingDirectory = EnsureWorkingDirectory();
        }

        protected override List<MenuOption> GetMenuOptions()
        {
            List<MenuOption> menuOptions = new List<MenuOption>();
            menuOptions.Add(new MenuOption("Open Directory", OpenDirectory));

            return menuOptions;
        }

        #endregion

        #region Private API

        [Documentation("Opens the selected directory in the file system.")]
        private void OpenDirectory()
        {
            Dictionary<Settings.Node, string> paths = Settings.GetValues(x => x.ToString().Contains("Path") && x != Settings.Node.WorkingDirectoryPath);
           
            List<string> choices = new List<string>();
            choices.Add(WorkingDirectoryName);
            choices.AddRange(paths.Where(x => Directory.Exists(x.Value) || File.Exists(x.Value)).Select(x => x.Key.ToString()));

            if (!paths.Any())
            {
                Utilities.StartProcess(Environment.CurrentDirectory);
                return;
            }

            SelectionPrompt<string> prompt = new SelectionPrompt<string>();
            prompt.Title = "Select the directory you want to open:";
            prompt.AddChoices(choices);

            string choice = AnsiConsole.Prompt(prompt);

            string path = choice == WorkingDirectoryName 
                        ? WorkingDirectory.FullName 
                        : Settings.GetSetting(Settings.GetNode(choice));

            Utilities.StartProcess(path);
        }

        private DirectoryInfo EnsureWorkingDirectory()
        {
            DirectoryInfo mainWorkingDirectory = Settings.GetDirectory(Settings.Node.WorkingDirectoryPath);

            if (string.IsNullOrEmpty(WorkingDirectoryName))
                return null;

            string path = Path.Combine(mainWorkingDirectory.FullName, WorkingDirectoryName);

            DirectoryInfo workingDirectory = new DirectoryInfo(path);

            if (!workingDirectory.Exists)
                workingDirectory = mainWorkingDirectory.CreateSubdirectory(WorkingDirectoryName);

            return workingDirectory;
        }

        #endregion
    }
}
