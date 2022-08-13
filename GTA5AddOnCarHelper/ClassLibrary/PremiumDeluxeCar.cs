using CustomSpectreConsole;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GTA5AddOnCarHelper
{
    public class PremiumDeluxeCar
    {
        #region Properties

        [TableColumn]
        [Protected]
        public string Name { get; set; }

        [TableColumn]
        public string Make { get; set; }

        [TableColumn]
        [Protected]
        public string Model { get; set; }

        [TableColumn]
        public string Class { get; set; }

        [Protected]
        public string GXT { get; set; }

        [TableColumn]
        public int Price { get; set; }

        #endregion

        #region Public API

        public string GetDisplayName()
        {
            return !string.IsNullOrEmpty(Name) ? Name : Model;
        }

        public string Save()
        {
            StringBuilder sb = new StringBuilder();

            typeof(PremiumDeluxeCar)
            .GetProperties(BindingFlags.Public|BindingFlags.Instance)
            .ToList()
            .ForEach(prop => 
            {
                object value = prop.GetValue(this);
                sb.Append(string.Format("[{0}]{1}", prop.Name.ToLower(), value));
            });

            return sb.ToString();
        }

        #endregion

        #region Static API

        public static Dictionary<string, PremiumDeluxeCar> GetFromMetaFiles()
        {
            Dictionary<string, PremiumDeluxeCar> cars = new Dictionary<string, PremiumDeluxeCar>();

            VehicleMeta.MetaFiles.ForEach(x => {

                PremiumDeluxeCar car = CreateFromMeta(x);

                if (!cars.ContainsKey(x.Model))
                    cars.Add(car.Model, car);
            });

            return cars;
        }

        public static Dictionary<string, PremiumDeluxeCar> GetFromIniDirectory(DirectoryInfo directory)
        {
            Dictionary<string, PremiumDeluxeCar> list = new Dictionary<string, PremiumDeluxeCar>();
            List<FileInfo> files = directory.GetFiles("*.ini").ToList();

            files.ForEach(file =>
            {
                List<string> entries = File.ReadLines(file.FullName).ToList();

                entries.ForEach(entry =>
                {
                    PremiumDeluxeCar car = PremiumDeluxeCar.CreateFromIniEntry(entry, file.Name.Replace(file.Extension, string.Empty));

                    if (car != null && !list.ContainsKey(car.Model))
                        list.Add(car.Model, car);
                });
            });

            return list;
        }

        public static PremiumDeluxeCar CreateFromMeta(VehicleMeta meta)
        {
            PremiumDeluxeCar car = new PremiumDeluxeCar();
            car.Name = meta.GameName;
            car.Make = meta.Make;
            car.Model = meta.Model;
            car.GXT = meta.Model;
            car.Price = 0;
            car.Class = "none";

            return car;
        }

        public static PremiumDeluxeCar CreateFromIniEntry(string entry, string vehicleClass)
        {
            PremiumDeluxeCar car = new PremiumDeluxeCar();
            car.Class = vehicleClass;

            string regexFormat = "(?<=\\[{0}\\])(.+?)(?=\\[|$)";

            foreach (PropertyInfo prop in car.GetType().GetProperties())
            {
                string regexMatch = string.Format(regexFormat, prop.Name.ToLower());
                Match match = Regex.Match(entry, regexMatch);

                if (!match.Success)
                    continue;

                bool isNumeric = int.TryParse(match.Value, out int numValue) && prop.PropertyType == typeof(int);
                object value = isNumeric ? numValue : match.Value;

                prop.SetValue(car, value);
            }

            return car;
        }

        #endregion
    }
}
