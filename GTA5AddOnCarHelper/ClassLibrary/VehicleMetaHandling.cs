using CustomSpectreConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GTA5AddOnCarHelper
{
    public class VehicleMetaHandling : VehicleMetaBase, IMetaObject<VehicleMetaHandling>
    {
        #region Constants

        public const string ModelNode = "handlingName";

        #endregion

        #region Properties

        public override string FileName => Constants.FileNames.HandlingMeta;

        #endregion

        #region Static API

        public static VehicleMetaHandling Create(XMLFile xml)
        {
            XElement modelNode = TryGetNode<VehicleMetaHandling>(xml, ModelNode);

            if (modelNode == null)
                return GenerateMissingAttributeError<VehicleMetaHandling>(ModelNode, xml);

            return new VehicleMetaHandling()
            {
                Model = modelNode.Value,
                XML = xml
            };
        }

        #endregion
    }
}
