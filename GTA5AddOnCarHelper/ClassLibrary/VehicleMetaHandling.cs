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

        public static VehicleMetaHandling Create(string filePath)
        {
            XDocument xml = null;

            try { xml = XDocument.Load(filePath); }
            catch (Exception) { return null; }

            XElement modelNode = TryGetNode(xml, ModelNode);

            if (modelNode == null)
            {
                GenerateError(ModelNode, filePath);
                return null;
            }

            return new VehicleMetaHandling()
            {
                Model = modelNode.Value,
                XML = xml
            };
        }

        #endregion
    }
}
