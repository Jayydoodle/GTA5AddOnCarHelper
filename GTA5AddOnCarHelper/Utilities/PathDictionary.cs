using CustomSpectreConsole;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GTA5AddOnCarHelper
{
    public static class Settings
    {
        #region Constants

        public const string FileName = "settings.xml";

        #endregion

        #region Public API

        public static T GetDirectoryOrFile<T>(Node node, string prompt = null, bool isRequired = true)
        where T : FileSystemInfo
        {
            T info = null;
            string path = GetValue(node);

            if (!string.IsNullOrEmpty(path))
                info  = (T)Activator.CreateInstance(typeof(T), new object[] { path });

            if (info == null || !info.Exists)
            {
                if (string.IsNullOrEmpty(prompt))
                    prompt = GetDefaultPromptByNode(node, isRequired);

                info = Utilities.GetFileSystemInfoFromInput<T>(prompt, isRequired);

                if (info != null && info.Exists)
                    Update(node, info.FullName);
            }

            return info;
        }

        public static DirectoryInfo GetDirectory(Node node, string prompt = null, bool isRequired = true)
        {
            DirectoryInfo dir = null;

            if (node == Node.WorkingDirectoryPath)
                isRequired = false;

            dir = GetDirectoryOrFile<DirectoryInfo>(node, prompt, isRequired);

            if (dir == null && node == Node.WorkingDirectoryPath)
            {
                dir = new DirectoryInfo(Directory.GetCurrentDirectory());
                Update(node, dir.FullName);
            }

            return dir;
        }

        public static FileInfo GetFile(Node node, string prompt = null, bool isRequired = true)
        {
            FileInfo file = GetDirectoryOrFile<FileInfo>(node, prompt, isRequired);
            return file;
        }

        public static DirectoryInfo GetDirectoryFromNode(Node node, string expectedPath)
        {
            DirectoryInfo dir = Settings.GetDirectory(node);
            string path = Path.Combine(dir.FullName, expectedPath);
            DirectoryInfo targetDir = new DirectoryInfo(path);

            while (!targetDir.Exists)
            {
                string message = string.Format("The target folder is expected at the path [red]{1}[/], but the directory does " +
                "not exist.  Please enter the correct path: ", path);

                path = Utilities.GetInput(message, x => !string.IsNullOrEmpty(x));
                targetDir = new DirectoryInfo(path);
            }

            return targetDir;
        }

        public static string GetSetting(Node node, string prompt = null)
        {
            string value = GetValue(node);
            bool exists = !string.IsNullOrEmpty(value) && (Directory.Exists(value) || File.Exists(value));

            if (exists)
                return value;

            if (string.IsNullOrEmpty(prompt))
                prompt = string.Format("Enter in a value for the {0}: ", node.ToString().SplitByCase());

            while (!exists)
            {
                value = Utilities.GetInput(prompt, x => !string.IsNullOrEmpty(x));
                exists = Directory.Exists(value) || File.Exists(value);

                if (!exists)
                    AnsiConsole.MarkupLine("The directory or file specified by the path [red]{0}[/] does not exist", value);
            }

            Update(node, value);

            return value;
        }

        public static Node GetNode(string nodeName)
        {
            return Enum.Parse<Node>(nodeName);
        }

        public static Dictionary<Node, string> GetValues(Func<Node, bool> predicate = null)
        {
            Dictionary<Node, string> paths = new Dictionary<Node, string>();
            XDocument xml = GetDocument();

            IEnumerable<Node> nodes = Enum.GetValues<Node>();

            if (predicate != null)
                nodes = nodes.Where(predicate);

            nodes.ToList()
            .ForEach(x =>
            {
                XElement elem = xml.Root.Element(x.ToString());

                if (elem != null)
                    paths.Add(x, elem.Value);
            });

            return paths;
        }

        public static void Update(Node node, string filePath)
        {
            XDocument xml = GetDocument();
            XElement elem = xml.Root.Element(node.ToString());

            if (elem == null) 
            {
                elem = new XElement(node.ToString());
                xml.Root.Add(elem);
            }

            elem.Value = filePath;
            xml.Save(FileName);
        }

        public static bool HasEntry(Node node)
        {
            return !string.IsNullOrEmpty(GetValue(node));    
        }

        #endregion

        #region Private API

        private static string GetValue(Node node)
        {
            XDocument xml = GetDocument();
            XElement elem = xml.Root.Element(node.ToString());

            return elem != null ? elem.Value : null;
        }

        private static XDocument GetDocument()
        {
            XDocument xml = null;

            try { xml = XDocument.Load(FileName); }
            catch (Exception)
            {
                xml = new XDocument(new XElement("Settings"));

                Enum.GetNames<Node>()
                .ToList()
                .ForEach(x =>
                {
                    xml.Root.Add(new XElement(x));
                });
            }

            return xml;
        }

        private static string GetDefaultPromptByNode(Node node, bool isRequired = true)
        {
            switch(node)
            {
                case Node.GTA5FolderPath:
                    return "Enter the path to your GTA 5 folder: ";

                case Node.LanguageFilesPath:
                    return "Enter the directory that contains the [green]" + Constants.Extentions.Gxt2 + "[/] files: ";

                case Node.OpenIVPath:
                    return "Enter the path to your [green]Open IV[/] application (press [blue]<enter>[/] to skip): ";

                case Node.VehicleDownloadsPath:
                    return "\nEnter the path to the directory containing all of your vehicle downloads: ";

                case Node.VehicleMetaFilesPath:
                    return "Enter the directory that contains the [green]" + Constants.Extentions.Meta + "[/] files: ";

                case Node.WorkingDirectoryPath:
                    return "Enter in the working directory that this program will use to output generated files (if nothing is entered, the current directory will be used): ";

                default:
                    return isRequired ? "Enter the directory: " : "Enter the directory (if nothing is entered, the current directory will be used): ";
            }
        }

        #endregion

        #region Helper Classes

        public enum Node
        {
            APIKey,
            WorkingDirectoryPath,
            GTA5FolderPath,
            OpenIVPath,
            VehicleMetaFilesPath,
            VehicleDownloadsPath,
            LanguageFilesPath,
        }

        #endregion
    }
}