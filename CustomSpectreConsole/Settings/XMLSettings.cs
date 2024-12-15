using System.Linq;
using System.Xml.Linq;

namespace CustomSpectreConsole.Settings
{
    public static class XMLSettings
    {
        #region Constants

        public const string FileName = "settings.xml";

        #endregion

        #region Public API

        public static U GetDirectoryOrFile<T, U>(T node, string prompt = null, bool isRequired = true)
        where T : SettingsNode, ISettingsNode
        where U : FileSystemInfo
        {
            U info = null;
            string path = GetValue(node);

            if (!string.IsNullOrEmpty(path))
                info = (U)Activator.CreateInstance(typeof(U), new object[] { path });

            if (info == null || !info.Exists)
            {
                if (string.IsNullOrEmpty(prompt))
                    prompt = node.GetPrompt();

                info = Utilities.GetFileSystemInfoFromInput<U>(prompt, isRequired);

                if (info != null && info.Exists)
                    Update(node, info.FullName);
            }

            return info;
        }

        public static DirectoryInfo GetDirectory<T>(T node, string prompt = null, bool isRequired = true)
        where T : SettingsNode, ISettingsNode
        {
            DirectoryInfo dir = GetDirectoryOrFile<T, DirectoryInfo>(node, prompt, isRequired);

            return dir;
        }

        public static FileInfo GetFile<T>(T node, string prompt = null, bool isRequired = true)
        where T : SettingsNode, ISettingsNode
        {
            FileInfo file = GetDirectoryOrFile<T, FileInfo>(node, prompt, isRequired);
            return file;
        }

        public static DirectoryInfo GetDirectoryFromNode<T>(T node, string expectedPath)
        where T : SettingsNode, ISettingsNode
        {
            DirectoryInfo dir = XMLSettings.GetDirectory(node);
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

        public static string GetSetting<T>(T node, Func<T, string, bool> ValidationFunction = null, PromptSettings settings = null)
        where T : SettingsNode, ISettingsNode
        {
            if (settings == null)
                settings = new PromptSettings();

            string value = GetValue(node);
            bool validated = false;

            if (!string.IsNullOrEmpty(value))
            {
                if (ValidationFunction == null || !settings.ValidateAlways)
                    validated = true;
                else
                    validated = ValidationFunction(node, value);
            }

            if (validated)
                return value;

            while (!validated)
            {
                if (string.IsNullOrEmpty(settings.Prompt))
                    settings.Prompt = node.GetPrompt();

                settings.Validator = x => !string.IsNullOrEmpty(x);
                value = Utilities.GetInput(settings);

                if (ValidationFunction != null)
                    validated = ValidationFunction(node, value);
                else
                    validated = true;
            }

            Update(node, value);

            return value;
        }

        public static Dictionary<T, string> GetValues<T>(Func<T, bool> predicate = null)
        where T : SettingsNode, ISettingsNode
        {
            Dictionary<T, string> paths = new Dictionary<T, string>();
            XDocument xml = GetDocument<T>();

            IEnumerable<T> nodes = T.GetAll<T>();

            if (predicate != null)
                nodes = nodes.Where(predicate);

            nodes.ToList()
            .ForEach(x =>
            {
                XElement elem = xml.Root.Element(x.Name);

                if (elem != null)
                    paths.Add(x, elem.Value);
            });

            return paths;
        }

        public static void Update<T>(T node, string value)
        where T : SettingsNode, ISettingsNode
        {
            XDocument xml = GetDocument<T>();
            XElement elem = xml.Root.Element(node.Name);

            if (elem == null)
            {
                elem = new XElement(node.Name);
                xml.Root.Add(elem);
            }

            elem.Value = value;
            xml.Save(FileName);
        }

        public static bool HasEntry<T>(T node)
        where T : SettingsNode, ISettingsNode
        {
            return !string.IsNullOrEmpty(GetValue(node));
        }

        public static string GetValue<T>(T node)
        where T : SettingsNode, ISettingsNode
        {
            XDocument xml = GetDocument<T>();
            XElement elem = xml.Root.Element(node.Name);

            return elem != null ? elem.Value : null;
        }

        #endregion

        #region Private API

        private static XDocument GetDocument<T>()
        where T : SettingsNode, ISettingsNode
        {
            XDocument xml = null;

            try { xml = XDocument.Load(FileName); }
            catch (Exception)
            {
                xml = new XDocument(new XElement("Settings"));

                T.GetAll<T>()
                .ForEach(x =>
                {
                    xml.Root.Add(new XElement(x.Name));
                });
            }

            return xml;
        }

        #endregion
    }
}
