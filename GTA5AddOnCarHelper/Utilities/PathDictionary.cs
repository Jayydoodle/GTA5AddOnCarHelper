using CustomSpectreConsole;
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

        private const string DefaultNotRequiredPrompt = "Enter the directory (if nothing is entered, the current directory will be used): ";
        private const string WorkingDirectoryPrompt = "Enter in the working directory that this program will use to output generated files (if nothing is entered, the current directory will be used): ";

        #endregion

        #region Public API

        public static DirectoryInfo GetDirectory(Node node, string prompt = null, bool isRequired = true)
        {
            DirectoryInfo dir = null;
            string path = GetValue(node);

            if (!string.IsNullOrEmpty(path))
                dir = new DirectoryInfo(path);

            if (dir == null || !dir.Exists)
            {
                if (string.IsNullOrEmpty(prompt))
                    prompt = isRequired ? GetDefaultPromptByNode(node) : DefaultNotRequiredPrompt;

                if (node == Node.WorkingDirectoryPath)
                {
                    isRequired = false;
                    prompt = WorkingDirectoryPrompt;
                }

                dir = Utilities.GetDirectoryFromInput(prompt, isRequired);

                if(dir == null && node == Node.WorkingDirectoryPath)
                    dir = new DirectoryInfo(Directory.GetCurrentDirectory());

                if (dir != null && dir.Exists)
                    Update(node, dir.FullName);
            }

            return dir;
        }

        public static string GetSetting(Node node, string prompt = null)
        {
            string value = GetValue(node);

            if (!string.IsNullOrEmpty(value))
                return value;

            if (string.IsNullOrEmpty(prompt))
                prompt = string.Format("Enter in a value for the {0}: ", node.ToString().SplitByCase());

            value = Utilities.GetInput(prompt, x => !string.IsNullOrEmpty(x));
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

        private static string GetDefaultPromptByNode(Node node)
        {
            switch(node)
            {
                case Node.GTA5FolderPath:
                    return "Enter the path to your GTA 5 folder: ";

                case Node.LanguageFilesPath:
                    return "Enter the directory that contains the [green]" + Constants.Extentions.Gxt2 + "[/] files: ";

                case Node.VehicleDownloadsPath:
                    return "\nEnter the path to the directory containing all of your vehicle downloads: ";

                case Node.VehicleMetaFilesPath:
                    return "Enter the directory that contains the [green]" + Constants.Extentions.Meta + "[/] files: ";

                default:
                    return "Enter the directory: ";
            }
        }

        #endregion

        #region Helper Classes

        public enum Node
        {
            APIKey,
            GTA5FolderPath,
            WorkingDirectoryPath,
            VehicleMetaFilesPath,
            VehicleDownloadsPath,
            LanguageFilesPath
        }

        #endregion
    }
}