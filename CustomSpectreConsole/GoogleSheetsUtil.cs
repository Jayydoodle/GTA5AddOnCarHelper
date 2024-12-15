using Google.Apis.Sheets.v4;
using Newtonsoft.Json;
using OfficeOpenXml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CustomSpectreConsole
{
    public static class GoogleSheetsUtil
    {
        private const string BaseUrl = "https://docs.google.com/spreadsheets/d/{0}/export?format=xlsx&amp;usp=sharing";

        public static void ProcessSheet(string sheetId, Action<ExcelWorksheet> processFunction)
        {
            var url = string.Format(BaseUrl, sheetId);

            using var client = new WebClient();
            client.Headers.Add("accept", "*/*");
            byte[] outputData = client.DownloadData(url);

            Stream stream = new MemoryStream(outputData);
            ExcelWorksheet sheet = null;

            using (var package = new ExcelPackage(stream))
            {
                sheet = package.Workbook.Worksheets[0];
                processFunction(sheet);
            }
        }
    }
}
