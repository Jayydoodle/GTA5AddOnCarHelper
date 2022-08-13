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
    {
        #region Properties

        protected abstract string WorkingDirectoryName { get; }
        protected DirectoryInfo WorkingDirectory { get; private set; }

        #endregion

        #region Protected API

        protected void Initialize()
        {
            WorkingDirectory = EnsureWorkingDirectory();
        }

        #endregion

        #region Private API

        private DirectoryInfo EnsureWorkingDirectory()
        {
            if (string.IsNullOrEmpty(WorkingDirectoryName))
                return null;

            DirectoryInfo mainWorkingDirectory = PathDictionary.GetDirectory(PathDictionary.Node.WorkingDirectoryPath);
            string path = Path.Combine(mainWorkingDirectory.FullName, WorkingDirectoryName);

            DirectoryInfo workingDirectory = new DirectoryInfo(path);

            if (!workingDirectory.Exists)
                workingDirectory = mainWorkingDirectory.CreateSubdirectory(WorkingDirectoryName);

            return workingDirectory;
        }

        #endregion
    }
}
