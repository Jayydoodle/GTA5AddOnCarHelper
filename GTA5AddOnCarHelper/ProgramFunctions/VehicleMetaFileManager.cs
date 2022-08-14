using CustomSpectreConsole;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GTA5AddOnCarHelper
{
    public sealed class VehicleMetaFileManager : AddOnCarHelperFunctionBase<VehicleMetaFileManager>    
    {
        #region Public API

        public override void Run()
        {
            Initialize();
            RunProgramLoop();
        }

        #endregion

        #region Private API

        protected override List<ListOption> GetListOptions()
        {
            List<ListOption> listOptions = new List<ListOption>();
            listOptions.Add(new ListOption("Create Meta File Directory", BuildMetaDirectory));
            listOptions.AddRange(base.GetListOptions());

            return listOptions;
        }

        #endregion

        #region Private API: Prompt Functions

        private void BuildMetaDirectory()
        {
            List<PropertyInfo> props = typeof(VehicleMeta).GetProperties()
                                        .Where(x => x.PropertyType.IsSubclassOf(typeof(VehicleMetaBase)))
                                        .ToList();

            VehicleMeta.GetMetaFiles(false).ForEach(x =>
            {
                DirectoryInfo dir = WorkingDirectory.CreateSubdirectory(x.Model);
                x.XML.Save(Path.Combine(dir.FullName, string.Format("{0}{1}", x.FileName, Constants.Extentions.Meta)));

                props.ForEach(prop =>
                {
                    if (prop.GetValue(x) is VehicleMetaBase obj)
                        obj.XML.Save(Path.Combine(dir.FullName, string.Format("{0}{1}", obj.FileName, Constants.Extentions.Meta)));
                });
            });
        }

        #endregion
    }
}
