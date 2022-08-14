using CustomSpectreConsole;
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
        public string HashOfMake
        {
            get { return Utilities.GetHash(Make); }
        }

        public VehicleMetaColor Color { get; set; }
        public VehicleMetaHandling Handling { get; set; }
        public VehicleMetaVariation Variation { get; set; }

        #endregion

        #region Static API

        public static List<VehicleMeta> GetMetaFiles(bool vehicleFilesOnly = true)
        {
            Dictionary<string, VehicleMeta> metaObjects = GetFiles<VehicleMeta>();

            if (vehicleFilesOnly)
                return metaObjects.Values.ToList();

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
            AnsiConsole.WriteLine();

            return metaObjects.Values.ToList();
        }

        private static void PrintError(IEnumerable<VehicleMetaBase> objs, string nodeName)
        {
            foreach (VehicleMetaBase obj in objs)
            {
                GenerateError(string.Format("Could not parse the XML from file [red]{0}[/]. It's [blue]<{1}>[/] attribute has a " +
                    "value of [green]{2}[/] which could not be resolved against a vehicle.meta model\n", obj.SourceFilePath, nodeName, obj.Model), obj.SourceFilePath);
            }
        }

        public static VehicleMeta Create(XMLFile xml)
        {
            XElement modelNode = xml.TryGetNode(ModelNode);

            if (modelNode == null)
                return null;

            XElement txdNode = xml.TryGetNode(TxdNode);

            if (txdNode == null)
                return null;

            XElement gameNameNode = xml.TryGetNode(GameNameNode);

            if (gameNameNode == null)
                return null;

            XElement vehicleMakeNameNode = xml.TryGetNode(VehicleMakeNameNode);

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
