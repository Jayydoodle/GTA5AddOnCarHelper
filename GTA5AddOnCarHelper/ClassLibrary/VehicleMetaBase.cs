using CustomSpectreConsole;
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
        public XMLFile XML { get; set; }
        public abstract string FileName { get; }
        public string SourceFilePath { get; set; }
        public string HashOfModel
        {
            get { return Utilities.GetHash(Model); }
        }

        #endregion

        #region Static API

        public static int ErrorCount = 0;
        protected static List<string> ModelNames { get; set; }

        public static  void GenerateError(string message, string fileName)
        {
            AnsiConsole.MarkupLine(message);
            ErrorCount++;
        }

        public static void GenerateMissingAttributeError(string node, string path)
        {
            AnsiConsole.MarkupLine("Could not parse the XML from file [red]{0}[/].  It is missing a [blue]<{1}>[/] attribute\n", path, node);
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
                XMLFile file = XMLFile.Load(x.FullName);
                T meta = default(T);

                if(file != null)
                    meta = T.Create(file);
                else
                    GenerateError(string.Format("The file [red]{0}[/] could not be processed. An error occurred while trying to read it's XML content.\n", x.FullName), x.FullName);

                if (meta != null && !metaFiles.ContainsKey(meta.Model.ToLower()))
                {
                    meta.SourceFilePath = x.FullName;
                    metaFiles.Add(meta.Model.ToLower(), meta);
                }
                else if(meta != null && metaFiles.ContainsKey(meta.Model.ToLower()))
                {
                    GenerateError(string.Format("The file [red]{0}[/] could not be processed.  A file with a duplicate identifier " +
                        "[orange1]{1}[/] has already been added to the list.\n", x.FullName, meta.Model.ToLower()), x.FullName);
                }
            });

            if (!metaFiles.Any())
                AnsiConsole.MarkupLine("No [green]" + fileName + Constants.Extentions.Meta + "[/] files were found in the specified directory");

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
        public static abstract T Create(XMLFile file);
    }
}
