using System.Configuration;
using System.Xml.Linq;

namespace CustomSpectreConsole.Settings
{
    public static class AppSettings
    {
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
            DirectoryInfo dir = AppSettings.GetDirectory(node);
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
            if(settings == null)
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

        public static void Update<T>(T node, string value)
        where T : SettingsNode, ISettingsNode
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            if (node.Type == SettingsNodeType.DatabaseConnection)
            {
                ConnectionStringSettings settings = new ConnectionStringSettings() { Name = node.Name, ConnectionString = value };
                config.ConnectionStrings.ConnectionStrings.Remove(node.Name);
                config.ConnectionStrings.ConnectionStrings.Add(settings);

                if (ConfigurationManager.ConnectionStrings[node.Name] != null)
                    ConfigurationManager.ConnectionStrings[node.Name].ConnectionString = value;
                else
                    ConfigurationManager.ConnectionStrings.Add(settings);
            }
            else
            {
                config.AppSettings.Settings.Remove(node.Name);
                config.AppSettings.Settings.Add(node.Name, value);
                ConfigurationManager.AppSettings[node.Name] = value;
            }

            config.Save(); 
        }

        public static bool HasEntry<T>(T node)
        where T : SettingsNode, ISettingsNode
        {
            return !string.IsNullOrEmpty(GetValue(node));
        }

        public static string GetValue<T>(T node)
        where T : SettingsNode, ISettingsNode
        {
            if(node.Type == SettingsNodeType.DatabaseConnection) 
            {
                if (ConfigurationManager.ConnectionStrings[node.Name] != null)
                    return ConfigurationManager.ConnectionStrings[node.Name].ConnectionString;
                else
                    return null;
            }

            return ConfigurationManager.AppSettings.Get(node.Name);
        }

        #endregion
    }
}
