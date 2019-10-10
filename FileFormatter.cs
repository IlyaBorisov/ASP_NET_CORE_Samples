using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace compleadapi.Common
{
    public enum FileType : byte
    {
        xml,
        xlsx
    }
    public static class FileFormatter
    {
        public static async Task<byte[]> GetFile(FileType format, List<Dictionary<string, object>> data, string[] keys = null)
        {
            if (format == FileType.xlsx)
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Sheet1");
                int i = 1, j = 2;
                if (keys == null)
                    keys = data[0].Keys.ToArray();
                foreach (var header in keys)
                {
                    worksheet.Cells[1, i++].Value = header;
                }
                foreach (var item in data)
                {
                    i = 1;
                    foreach (var header in keys)
                    {
                        worksheet.Cells[j, i++].Value = $"{item[header]}";
                    }
                    j++;
                }
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                return package.GetAsByteArray();
            }
            else if (format == FileType.xml)
            {
                var xdoc = new XDocument(new XDeclaration("1.0", "", "yes"));
                var root = new XElement("VFPData");
                foreach (var item in data)
                {
                    var temp = new XElement("tempxml");
                    foreach (var header in keys)
                    {
                        temp.Add(new XElement(header)
                        {
                            Value = $"{item[header]}".Trim()
                        });
                    }
                    root.Add(temp);
                }
                if (data.Count == 0)
                    root.Value = "";
                xdoc.Add(root);
                using var ms = new MemoryStream();
                using var wr = new StreamWriter(ms, Encoding.UTF8);
                xdoc.Save(wr);
                var fileContents = new byte[ms.Length];
                ms.Position = 0;
                await ms.ReadAsync(fileContents, 0, (int)ms.Length).ConfigureAwait(false);
                return fileContents;
            }
            else
                return null;
        }
    }
}
