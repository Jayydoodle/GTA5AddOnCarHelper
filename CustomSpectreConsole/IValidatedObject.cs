using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomSpectreConsole
{
    public interface IValidatedObject
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
    }
}
