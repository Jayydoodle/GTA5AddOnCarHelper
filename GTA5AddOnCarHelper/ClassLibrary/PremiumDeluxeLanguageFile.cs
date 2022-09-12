using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTA5AddOnCarHelper
{
    public class PremiumDeluxeLanguageFile
    {
        #region Constants

        private const string CategoryIdentifier = "//";

        #endregion

        #region Properties

        public Dictionary<string, string> VehicleClasses { get; set; }
        public string SourceFileName { get; set; }
        public string SourceFilePath { get; set; }
        public string DisplayName { get { return SourceFileName.Replace(Constants.Extentions.Cfg, string.Empty); } }
        private string CategoryName { get; set; }
        private string OtherText { get; set; }

        #endregion

        #region Constructor

        public PremiumDeluxeLanguageFile()
        {
            VehicleClasses = new Dictionary<string, string>();
        }

        #endregion

        #region Public API

        public StringBuilder Save()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(CategoryName);

            foreach(KeyValuePair<string, string> pair in VehicleClasses)
            {
                string text = string.Format("{0} {1}", pair.Key, pair.Value);
                sb.AppendLine(text);
            }

            sb.AppendLine();
            sb.Append(OtherText);

            return sb;
        }

        public static List<PremiumDeluxeLanguageFile> GetAll(DirectoryInfo dir)
        {
            FileInfo[] files = dir.GetFiles("*" + Constants.Extentions.Cfg);
            List<PremiumDeluxeLanguageFile> langFiles = new List<PremiumDeluxeLanguageFile>();

            foreach (FileInfo file in files)
            {
                PremiumDeluxeLanguageFile languageFile = new PremiumDeluxeLanguageFile();
                IEnumerable<string> lines = File.ReadLines(file.FullName);
                int iteration = 0;
                bool success = true, complete = false;
                StringBuilder sb = new StringBuilder();

                foreach(string line in lines)
                {
                    iteration++;

                    if (iteration == 1 && line.Contains(CategoryIdentifier)) {
                        languageFile.CategoryName = line;
                        continue;
                    }
                    else if (iteration == 1) { success = false; break; }

                    if (line.Contains(CategoryIdentifier)) { complete = true; }
                    if (complete) { sb.AppendLine(line); continue; }

                    string[] pieces = line.Split('"', StringSplitOptions.RemoveEmptyEntries)
                                          .Select(x => x.Trim())
                                          .ToArray();

                    if (pieces.Length != 2) { continue; }

                    if(!languageFile.VehicleClasses.ContainsKey(pieces[0]))
                        languageFile.VehicleClasses.Add(pieces[0], string.Format("\"{0}\"", pieces[1]));
                }

                if (success)
                {
                    languageFile.SourceFileName = file.Name;
                    languageFile.SourceFilePath = file.FullName;
                    languageFile.OtherText = sb.ToString(); 
                    langFiles.Add(languageFile);
                }
            }

            return langFiles;
        }

        #endregion
    }
}
