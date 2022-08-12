using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using static GTA5AddOnCarHelper.PathDictionary;

namespace GTA5AddOnCarHelper
{
    public abstract class VehicleMetaBase
    {
        #region Properties

        public string Model { get; set; }
        public XDocument XML { get; set; }
        public abstract string FileName { get; }
        public string SourceFilePath { get; set; }

        #endregion

        #region Static API

        public static int ErrorCount = 0;

        public static XElement TryGetNode(XDocument xml, string node)
        {
            return xml.Descendants(node).FirstOrDefault(x => ModelNames.Any(y => x.Value == y));
        }

        protected static List<string> ModelNames { get; set; }

        public static void GenerateError(string node, string path)
        {
            AnsiConsole.MarkupLine("Could not parse the XML from file [red]{0}[/].  It is missing a [red]<{1}>[/] attribute", path, node);
            ErrorCount++;
        }

        public static Dictionary<string, T> GetFiles<T>()
        where T : IMetaObject<T>, new()
        {
            Dictionary<string, T> metaFiles = new Dictionary<string, T>();

            string prompt = "Enter the directory that contains the [green]"+ Constants.Extentions.Meta +"[/] files: ";
            DirectoryInfo dir = PathDictionary.GetDirectory(PathDictionary.Node.VehicleMetaFilesPath, prompt);
            string fileName = new T().FileName;

            string regexFormat = string.Format("{0}{1}|((:?^|\\s){0}[\\(])", fileName, Constants.Extentions.Meta);

            List<FileInfo> files = dir.GetFiles("*" + Constants.Extentions.Meta, SearchOption.AllDirectories)
                                      .Where(x => Regex.Match(x.Name, regexFormat).Success)
                                      .ToList();
            files.ForEach(x =>
            {
                T meta = T.Create(x.FullName);

                if (meta != null && !metaFiles.ContainsKey(meta.Model.ToLower()))
                {
                    meta.SourceFilePath = x.FullName;
                    metaFiles.Add(meta.Model.ToLower(), meta);
                }
                else if(meta != null && metaFiles.ContainsKey(meta.Model.ToLower()))
                {
                    AnsiConsole.MarkupLine("The file [red]{0}[/] could not be processed.  A file with a duplicate identifier [red]{1}[/] has already been added to the list.", x.FullName, meta.Model.ToLower());
                    ErrorCount++;
                }
            });

            if (!metaFiles.Any())
            {
                AnsiConsole.MarkupLine("No [green]" + Constants.Extentions.Meta + "[/] files were found in the specified directory");
                ErrorCount++;
            }

            return metaFiles;
        }

        #endregion
    }

    public interface IMetaObject<T>
    where T : new()
    {
        public string Model { get; }
        public string FileName { get; }
        public string SourceFilePath { get; set; }
        public static abstract T Create(string path);
    }
}
