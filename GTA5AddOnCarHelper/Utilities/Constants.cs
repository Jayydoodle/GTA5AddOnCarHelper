using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTA5AddOnCarHelper
{
    public static class Constants
    {
        public static class Commands
        {
            public const string CANCEL = "CANCEL";
            public const string MENU = "MENU";
            public const string EXIT = "EXIT";
        }

        public static class Extentions
        {
            public const string Ini = ".ini";
            public const string Meta = ".meta";
        }

        public static class FileNames
        {
            public const string ColorsMeta = "carcols";
            public const string HandlingMeta = "handling";
            public const string VehicleMeta = "vehicles";
            public const string VariationsMeta = "carvariations";
        }

        public static class SelectionOptions
        {
            public const string Yes = "Yes";
            public const string No = "No";
            public const string Exit = "Exit";
            public const string Continue = "Continue";
            public const string ReturnToMenu = "Return To Menu";
            public const string ReturnToMainMenu = "Return To Main Menu";
        }
    }
}
