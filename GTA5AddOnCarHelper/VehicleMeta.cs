using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GTA5AddOnCarHelper
{
    public class VehicleMeta
    {
        #region Constants

        private const string FileName = "vehicles.meta";

        private const string ModelNode = "modelName";
        private const string TxdNode = "txdName";
        private const string GameNameNode = "gameName";
        private const string VehicleMakeNameNode = "vehicleMakeName";

        #endregion

        #region Properties

        public string Model{ get; set; }
        public string Make { get; set; }
        public string GameName { get; set; }
        public string TxDName { get; set; }
        private XElement XML { get; set; }

        #endregion

        #region Public API

        public static VehicleMeta Create(string filePath)
        {
            XElement xml = null;

            try { xml = XElement.Load(filePath); }
            catch (Exception) { return null; }

            XElement modelNode = xml.Descendants(ModelNode).FirstOrDefault();

            if (modelNode == null)
                return null;

            XElement txdNode = xml.Descendants(TxdNode).FirstOrDefault();

            if (txdNode == null)
                return null;

            XElement gameNameNode = xml.Descendants(GameNameNode).FirstOrDefault();

            if (gameNameNode == null)
                return null;

            XElement vehicleMakeNameNode = xml.Descendants(VehicleMakeNameNode).FirstOrDefault();

            if (vehicleMakeNameNode == null)
                return null;

            return new VehicleMeta()
            { 
                Model = modelNode.Value,
                TxDName = txdNode.Value,
                GameName = gameNameNode.Value,
                Make = vehicleMakeNameNode.Value,
                XML = xml
            };
        }

        #endregion

        #region Static API

        private static List<VehicleMeta> _metaFiles;
        public static List<VehicleMeta> MetaFiles
        {
            get
            {
                if (_metaFiles == null || !_metaFiles.Any())
                    _metaFiles = GetMetaFiles();

                return _metaFiles;
            }
        }

        private static List<VehicleMeta> GetMetaFiles()
        {
            List<VehicleMeta> metaFiles = new List<VehicleMeta>();

            string prompt = "Enter the directory that contains the [green]vehicle.meta[/] files: ";
            DirectoryInfo dir = PathDictionary.GetDirectory(PathDictionary.Node.VehicleMetaFilesPath, prompt);

            List<FileInfo> files = dir.GetFiles("*.meta", SearchOption.AllDirectories).ToList();

            files.ForEach(x =>
            {
                VehicleMeta meta = VehicleMeta.Create(x.FullName);

                if (meta != null)
                    metaFiles.Add(meta);
            });

            if(!metaFiles.Any())
                AnsiConsole.WriteLine("No vehicle.meta files were found in the specified directory");

            return metaFiles.OrderBy(x => x.Model).ToList();
        }

        #endregion
    }
}
