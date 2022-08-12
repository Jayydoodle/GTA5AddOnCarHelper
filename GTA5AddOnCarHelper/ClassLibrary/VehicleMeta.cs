using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static GTA5AddOnCarHelper.PathDictionary;

namespace GTA5AddOnCarHelper
{
    public class VehicleMeta : VehicleMetaBase, IMetaObject<VehicleMeta>
    {
        #region Constants

        private const string ModelNode = "modelName";
        private const string TxdNode = "txdName";
        private const string GameNameNode = "gameName";
        private const string VehicleMakeNameNode = "vehicleMakeName";

        #endregion

        #region Properties

        public override string FileName => Constants.FileNames.VehicleMeta;

        public string Make { get; set; }
        public string GameName { get; set; }
        public string TxDName { get; set; }

        public VehicleMetaColor Color { get; set; }
        public VehicleMetaHandling Handling { get; set; }
        public VehicleMetaVariation Variation { get; set; }

        #endregion

        #region Static API

        private static List<VehicleMeta> _metaFiles;
        public static List<VehicleMeta> MetaFiles
        {
            get
            {
                if (_metaFiles == null || !_metaFiles.Any())
                    _metaFiles = BuildMetaFiles();

                return _metaFiles;
            }
        }

        private static List<VehicleMeta> BuildMetaFiles()
        {
            Dictionary<string, VehicleMeta> metaObjects = GetFiles<VehicleMeta>();

            ModelNames = metaObjects.Keys.ToList();

            Dictionary<string, VehicleMetaColor> colorObjects = GetFiles<VehicleMetaColor>();
            Dictionary<string, VehicleMetaHandling> handlingObjects = GetFiles<VehicleMetaHandling>();
            Dictionary<string, VehicleMetaVariation> variationObjects = GetFiles<VehicleMetaVariation>();

            foreach(KeyValuePair<string, VehicleMeta> meta in metaObjects)
            {
                VehicleMeta v = meta.Value;

                if (colorObjects.ContainsKey(meta.Key))
                {
                    v.Color = colorObjects[meta.Key];
                    colorObjects.Remove(meta.Key);
                }

                if (handlingObjects.ContainsKey(meta.Key))
                {
                    v.Handling = handlingObjects[meta.Key];
                    handlingObjects.Remove(meta.Key);
                }

                if (variationObjects.ContainsKey(meta.Key))
                {
                    v.Variation = variationObjects[meta.Key];
                    variationObjects.Remove(meta.Key);
                }
            }

            PrintError(colorObjects.Values, VehicleMetaColor.ModelNode);
            PrintError(handlingObjects.Values, VehicleMetaHandling.ModelNode);
            PrintError(variationObjects.Values, VehicleMetaVariation.ModelNode);

            AnsiConsole.MarkupLine("\nError Count: [yellow]{0}[/]", ErrorCount);

            return metaObjects.Values.ToList();
        }

        private static void PrintError(IEnumerable<VehicleMetaBase> objs, string nodeName)
        {
            foreach (VehicleMetaBase obj in objs)
            {
                AnsiConsole.MarkupLine("Could not parse the XML from file [red]{0}[/]. It's [red]<{1}>[/] field has a value of [red]{2}[/] which could not be resolved against a vehicle.meta model", obj.SourceFilePath, nodeName, obj.Model);
                ErrorCount++;
            }
        }

        public static VehicleMeta Create(string filePath)
        {
            XDocument xml = null;

            try { xml = XDocument.Load(filePath); }
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
    }
}
