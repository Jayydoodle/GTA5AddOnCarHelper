using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomSpectreConsole
{
    public class ProtectedAttribute : Attribute
    {
    }

    public class TableColumnAttribute : Attribute
    {
        public int DisplayOrder { get; set; }

        public TableColumnAttribute() { }

        public TableColumnAttribute(int displayOrder)
        {
            DisplayOrder = displayOrder;
        }
    }
}
