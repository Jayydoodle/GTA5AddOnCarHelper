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
        private const string HandlingIdNode = "handlingId";
        private const string GameNameNode = "gameName";
        private const string VehicleMakeNameNode = "vehicleMakeName";

        #endregion

        #region Properties

        public override string FileName => Constants.FileNames.VehicleMeta;

        [TableColumn]
        public string Make { get; set; }
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
            XElement modelNode = TryGetNode<VehicleMeta>(xml, ModelNode);

            if (modelNode == null)
                return GenerateMissingAttributeError<VehicleMeta>(ModelNode, xml);

            XElement txdNode = TryGetNode<VehicleMeta>(xml, TxdNode);

            if (txdNode == null)
                return GenerateMissingAttributeError<VehicleMeta>(TxdNode, xml);

            XElement handlindIdNode = TryGetNode<VehicleMeta>(xml, HandlingIdNode);

            if (handlindIdNode == null)
                return GenerateMissingAttributeError<VehicleMeta>(HandlingIdNode, xml);

            XElement gameNameNode = TryGetNode<VehicleMeta>(xml, GameNameNode);

            if (gameNameNode == null)
                return GenerateMissingAttributeError<VehicleMeta>(GameNameNode, xml);

            XElement vehicleMakeNameNode = TryGetNode<VehicleMeta>(xml, VehicleMakeNameNode);

            if (vehicleMakeNameNode == null)
                return GenerateMissingAttributeError<VehicleMeta>(VehicleMakeNameNode, xml);

            return new VehicleMeta()
            {
                Model = modelNode.Value,
                TxDName = txdNode.Value,
                HandlingId = handlindIdNode.Value,  
                GameName = gameNameNode.Value,
                Make = vehicleMakeNameNode.Value,
                XML = xml
            };
        }

        #endregion
    }
}
