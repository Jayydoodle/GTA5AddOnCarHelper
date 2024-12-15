using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CustomSpectreConsole
{
    public static class ExcelUtil
    {
        public static List<T> ImportData<T>(ExcelWorksheet sheet)
        where T : new()
        {
            List<T> list = new List<T>();

            int startRow = sheet.Dimension.Start.Row;
            int endRow = sheet.Dimension.End.Row;
            int startColumn = sheet.Dimension.Start.Column;
            int endColumn = sheet.Dimension.End.Column;

            Dictionary<int, string> columnNames = new Dictionary<int, string>();

            for (int i = startColumn; i <= endColumn; i++)
            {
                ExcelRange cell = sheet.Cells[startRow, i];
                string columnName = new string(cell.Text.Where(x => char.IsAsciiLetter(x)).ToArray());

                if (!columnNames.ContainsKey(i))
                    columnNames.Add(i, columnName);
            }

            List<PropertyInfo> props = typeof(T).GetProperties().ToList();

            for (int row = startRow + 1; row <= endRow; row++)
            {
                T entry = new T();

                for (int col = startColumn; col <= endColumn; col++)
                {
                    columnNames.TryGetValue(col, out string columnName);
                    ExcelRange cell = sheet.Cells[row, col];

                    PropertyInfo prop = props.FirstOrDefault(x => x.Name == columnName);

                    if (prop != null)
                        prop.SetValue(entry, cell.Value);
                }

                list.Add(entry);
            }

            return list;
        }

        public static void ExportData<T>(List<T> data, string exportFileName)
        {
            using (var package = new ExcelPackage(exportFileName))
            {
                ExcelWorksheet sheet = package.Workbook.Worksheets.Add("Sheet 1");
                List<PropertyInfo> props = typeof(T).GetProperties().ToList();
                Dictionary<int, string> columnNames = new Dictionary<int, string>();

                for (int i = 0; i < props.Count; i++)
                {
                    int currentColumnIndex = i + 1;
                    PropertyInfo prop = props[i];
                    ExcelRange range = sheet.Cells[1, currentColumnIndex];
                    range.Value = prop.Name.SplitByCase();
                    columnNames.Add(currentColumnIndex, prop.Name);
                }

                for (int i = 0; i < data.Count; i++)
                {
                    T entry = data[i];
                    int currentRowIndex = i + 2;

                    for (int j = 0; j <= props.Count; j++)
                    {
                        int currentColumnIndex = j + 1;
                        columnNames.TryGetValue(currentColumnIndex, out string columnName);

                        PropertyInfo prop = props.FirstOrDefault(x => x.Name == columnName);

                        if (prop != null)
                        {
                            ExcelRange cell = sheet.Cells[currentRowIndex, currentColumnIndex];
                            cell.Value = prop.GetValue(entry);
                        }
                    }
                }

                package.Save();
            }
        }
    }
}
