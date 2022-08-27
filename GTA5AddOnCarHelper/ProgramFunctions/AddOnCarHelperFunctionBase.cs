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

        protected override List<ListOption> GetListOptions()
        {
            List<ListOption> listOptions = new List<ListOption>();
            listOptions.Add(new ListOption("Open Working Directory", OpenWorkingDirectory));
            listOptions.AddRange(base.GetListOptions());

            return listOptions;
        }

        #endregion

        #region Private API

        private void OpenWorkingDirectory()
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
            {
                FileName = WorkingDirectory.FullName,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        private DirectoryInfo EnsureWorkingDirectory()
        {
            DirectoryInfo mainWorkingDirectory = PathDictionary.GetDirectory(PathDictionary.Node.WorkingDirectoryPath);

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
