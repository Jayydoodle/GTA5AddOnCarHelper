using CustomSpectreConsole;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GTA5AddOnCarHelper
{
    public class VehicleMetaColor : VehicleMetaBase, IMetaObject<VehicleMetaColor>
    {
        #region Constants

        public const string ModelNode = "name";

        #endregion

        #region Properties

        public override string FileName => Constants.FileNames.ColorsMeta;

        #endregion

        #region Static API

        public static VehicleMetaColor Create(XMLFile xml)
        {
            XElement modelNode = xml.TryGetNode(ModelNode);

            if (modelNode == null)
            {
                GenerateMissingAttributeError(ModelNode, xml.SourceFileName);
                return null;
            }

            return new VehicleMetaColor()
            {
                Model = modelNode.Value,
                XML = xml
            };
        }

        #endregion
    }
}
