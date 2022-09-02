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
using static GTA5AddOnCarHelper.Settings;

namespace GTA5AddOnCarHelper
{
    public abstract class VehicleMetaBase : IValidatedObject
    {
        #region Properties

        [TableColumn]
        public string Model { get; set; }
        public XMLFile XML { get; set; }
        public abstract string FileName { get; }
        public string SourceFilePath { get; set; }
        public string HashOfModel
        {
            get { return Utilities.GetHash(Model); }
        }

        #endregion

        #region [IValidatedObject]

        public bool IsValid { get; set; } = true;
        public string ErrorMessage { get; set; }

        #endregion

        #region Static API

        public static XElement TryGetNode<T>(XMLFile xml, string node)
        {
            if (typeof(T) == typeof(VehicleMeta))
            {
                return xml.TryGetNode(node);
            }
            else 
            {
                return xml.TryGetNode(node, x => !string.IsNullOrEmpty(x.Value) && VehicleMetaFileManager.Instance.ModelNames.Any(y => x.Value.Contains(y, StringComparison.OrdinalIgnoreCase)));
            }
        }

        public static T GenerateMissingAttributeError<T>(string node, XMLFile xml)
        where T : VehicleMetaBase, new()
        {
            T item = new T();
            item.XML = xml;
            item.IsValid = false;
            item.ErrorMessage = string.Format("Could not parse the XML from file [red]{0}[/].  It is missing a [blue]<{1}>[/] attribute\n", xml.SourceFileName, node);

            return item;
        }

        #endregion
    }

    public interface IMetaObject<T>
    where T : VehicleMetaBase, new()
    {
        public string Model { get; }
        public string FileName { get; }
        public string SourceFilePath { get; set; }
        public static abstract T Create(XMLFile file);
    }
}
