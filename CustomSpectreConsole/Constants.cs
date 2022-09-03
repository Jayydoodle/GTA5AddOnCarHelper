using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomSpectreConsole
{
    public class GlobalConstants
    {
        public static TextInfo TEXTINFO = new CultureInfo("en-us", false).TextInfo;

        public static class Commands
        {
            public const string CANCEL = "CANCEL";
            public const string MENU = "MENU";
            public const string EXIT = "EXIT";
        }

        public static class SelectionOptions
        {
            public const string Yes = "Yes";
            public const string No = "No";
            public const string Exit = "Exit";
            public const string Continue = "Continue";
            public const string Help = "Help";
            public const string ReturnToMenu = "Return To Menu";
            public const string ReturnToMainMenu = "Return To Main Menu";
        }

        public static class FileExtension
        {
            public const string Zip = ".zip";
            public const string Rar = ".rar";
            public const string SevenZip = ".7z";
        }

        public static class RegexPattern
        {
            public const string AmericanCurrency = "\\$[\\s]?([\\d\\.\\,]+)[\\s]*(thousand|million|billion|trillion)?";
        }
    }
}
