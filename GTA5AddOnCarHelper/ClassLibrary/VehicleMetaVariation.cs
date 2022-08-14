using CustomSpectreConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GTA5AddOnCarHelper
{
    public class VehicleMetaVariation : VehicleMetaBase,  IMetaObject<VehicleMetaVariation>
    {
        #region Constants

        public const string ModelNode = "modelName";

        #endregion

        #region Properties

        public override string FileName => Constants.FileNames.VariationsMeta;

        #endregion

        #region Static API

        public static VehicleMetaVariation Create(XMLFile xml)
        {
            XElement modelNode = xml.TryGetNode(ModelNode);

            if (modelNode == null)
            {
                GenerateMissingAttributeError(ModelNode, xml.SourceFileName);
                return null;
            }

            return new VehicleMetaVariation()
            {
                Model = modelNode.Value,
                XML = xml
            };
        }

        #endregion
    }
}
