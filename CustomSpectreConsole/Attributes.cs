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

    public class TableColumn : Attribute
    {
        public int DisplayOrder { get; set; }

        public TableColumn() { }

        public TableColumn(int displayOrder)
        {
            DisplayOrder = displayOrder;
        }
    }
}
