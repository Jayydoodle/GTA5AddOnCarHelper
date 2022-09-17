using CustomSpectreConsole;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static GTA5AddOnCarHelper.Settings;

namespace GTA5AddOnCarHelper
{
    public class VehicleMeta : VehicleMetaBase, IMetaObject<VehicleMeta>
    {
        #region Constants

        private const string InitDatasNode = "InitDatas";
        private const string ItemNode = "Item";
        private const string ModelNode = "modelName";
        private const string TxdNode = "txdName";
        private const string HandlingIdNode = "handlingId";
        private const string GameNameNode = "gameName";
        private const string VehicleMakeNameNode = "vehicleMakeName";
        private const string VehicleClassNameNode = "vehicleClass";

        #endregion

        #region Properties

        public override string FileName => Constants.FileNames.VehicleMeta;

        [TableColumn]
        public string Make { get; set; }
        [TableColumn]
        public string Class { get; set; }
        [TableColumn]
        public string GameName { get; set; }
        [TableColumn]
        public string TxDName { get; set; }
        [TableColumn]
        public string HandlingId { get; set; }
        public string HashOfMake
        {
            get { return Utilities.GetHash(Make); }
        }

        public VehicleMetaColor Color { get; set; }
        public VehicleMetaHandling Handling { get; set; }
        public VehicleMetaVariation Variation { get; set; }
        public VehicleMetaLayout Layout { get; set; }

        #endregion

        #region Static API

        public static VehicleMeta Create(XMLFile xml)
        {
            XElement itemNode = xml.TryGetNode(ItemNode);
            return itemNode != null ? Create(xml, itemNode) : null;
        }

        public static List<VehicleMeta> CreateAll(XMLFile xml)
        {
            List<VehicleMeta> metaFiles = new List<VehicleMeta>();
            XElement baseNode = xml.TryGetNode(InitDatasNode);

            List<XElement> itemNodes = baseNode.Document.Descendants(ItemNode)
                                       .Where(x => x.Parent == baseNode).ToList();

            itemNodes.ForEach(x => metaFiles.Add(Create(xml, x)));
            return metaFiles;
        }

        private static VehicleMeta Create(XMLFile xml, XElement node)
        {
            XElement modelNode = node.Descendants(ModelNode).FirstOrDefault();

            if (modelNode == null)
                return GenerateMissingAttributeError<VehicleMeta>(ModelNode, xml);

            XElement txdNode = node.Descendants(TxdNode).FirstOrDefault();

            if (txdNode == null)
                return GenerateMissingAttributeError<VehicleMeta>(TxdNode, xml);

            XElement handlindIdNode = node.Descendants(HandlingIdNode).FirstOrDefault();

            if (handlindIdNode == null)
                return GenerateMissingAttributeError<VehicleMeta>(HandlingIdNode, xml);

            XElement gameNameNode = node.Descendants(GameNameNode).FirstOrDefault();

            if (gameNameNode == null)
                return GenerateMissingAttributeError<VehicleMeta>(GameNameNode, xml);

            XElement vehicleMakeNameNode = node.Descendants(VehicleMakeNameNode).FirstOrDefault();

            if (vehicleMakeNameNode == null)
                return GenerateMissingAttributeError<VehicleMeta>(VehicleMakeNameNode, xml);

            XElement vehicleClassNameNode = node.Descendants(VehicleClassNameNode).FirstOrDefault();

            if (vehicleClassNameNode == null)
                return GenerateMissingAttributeError<VehicleMeta>(VehicleClassNameNode, xml);

            return new VehicleMeta()
            {
                Model = modelNode.Value,
                TxDName = txdNode.Value,
                HandlingId = handlindIdNode.Value,
                GameName = gameNameNode.Value,
                Make = vehicleMakeNameNode.Value,
                Class = vehicleClassNameNode.Value,
                XML = xml
            };
        }

        #endregion
    }
}
