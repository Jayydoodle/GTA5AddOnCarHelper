using CustomSpectreConsole;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GTA5AddOnCarHelper
{
    public static class PathDictionary
    {
        #region Constants

        public const string FileName = "paths.xml";

        private const string DefaultRequiredPrompt = "Enter the directory: ";
        private const string DefaultNotRequiredPrompt = "Enter the directory (if nothing is entered, the current directory will be used): ";
        private const string WorkingDirectoryPrompt = "Enter in the working directory that this program will use to output generated files (if nothing is entered, the current directory will be used): ";

        #endregion

        #region Public API

        public static DirectoryInfo GetDirectory(Node node, string prompt = null, bool isRequired = true)
        {
            DirectoryInfo dir = null;
            string path = GetPath(node);

            if (!string.IsNullOrEmpty(path))
                dir = new DirectoryInfo(path);

            if (dir == null || !dir.Exists)
            {
                if (string.IsNullOrEmpty(prompt))
                    prompt = isRequired ? DefaultRequiredPrompt : DefaultNotRequiredPrompt;

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

        public static string GetPath(Node node)
        {
            XDocument xml = GetDocument();
            XElement elem = xml.Root.Element(node.ToString());

            return elem != null ? elem.Value : null;
        }

        public static Dictionary<Node, string> GetPaths()
        {
            Dictionary<Node, string> paths = new Dictionary<Node, string>();
            XDocument xml = GetDocument();

            Enum.GetValues<Node>()
            .ToList()
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
            return !string.IsNullOrEmpty(GetPath(node));    
        }

        #endregion

        #region Private API

        private static XDocument GetDocument()
        {
            XDocument xml = null;

            try { xml = XDocument.Load(FileName); }
            catch (Exception)
            {
                xml = new XDocument(new XElement("Paths"));

                Enum.GetNames<Node>()
                .ToList()
                .ForEach(x =>
                {
                    xml.Root.Add(new XElement(x));
                });
            }

            return xml;
        }

        #endregion

        #region Helper Classes

        public enum Node
        {
            WorkingDirectoryPath,
            DLCListFilesPath,
            VehicleMetaFilesPath,
            LanguageFilesPath
        }

        #endregion
    }
}