using CustomSpectreConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GTA5AddOnCarHelper
{
    public class VehicleMetaLayout: VehicleMetaBase, IMetaObject<VehicleMetaLayout>
    {
        #region Constants

        public const string ModelNode = "Name";

        #endregion

        #region Properties

        public override string FileName => Constants.FileNames.LayoutsMeta;

        #endregion

        #region Static API

        public static VehicleMetaLayout Create(XMLFile xml)
        {
            XElement modelNode = TryGetNode<VehicleMetaLayout>(xml, ModelNode);

            if (modelNode == null)
                return GenerateMissingAttributeError<VehicleMetaLayout>(ModelNode, xml);

            return new VehicleMetaLayout()
            {
                Model = VehicleMetaFileManager.Instance.ModelNames.Where(x => modelNode.Value.Contains(x, StringComparison.OrdinalIgnoreCase)).FirstOrDefault(),
                XML = xml
            };
        }

        #endregion
    }
}
