using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomSpectreConsole
{
    public class PromptSettings
    {
        public string Prompt { get; set; }
        public bool IsSecret { get; set; }
        public bool ValidateAlways { get; set; } = true;
        public string ValidationErrorMessage { get; set; }
        public Func<string, bool> Validator { get; set; }

        public PromptSettings() { }

        public PromptSettings(string prompt)
        {
            Prompt = prompt;
        }
    }
}
