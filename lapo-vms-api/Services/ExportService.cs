using System.Globalization;
using System.Reflection;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using lapo_vms_api.Interface;

namespace lapo_vms_api.Services;

public class ExportService : IExportService
{
    public byte[] ExportToCsv<T>(List<T> data)
    {
        using var memoryStream = new MemoryStream();
        using var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8);
        var config = new CsvConfiguration
        {
            CultureInfo = CultureInfo.InvariantCulture,
            HasHeaderRecord = true
        };

        using var csv = new CsvWriter(streamWriter, config);
        csv.WriteRecords(data);
        streamWriter.Flush();

        return memoryStream.ToArray();
    }

    public byte[] ExportToExcel<T>(List<T> data, string sheetName = "Sheet1")
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        for (int i = 0; i < properties.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = properties[i].Name;
        }

        for (int row = 0; row < data.Count; row++)
        {
            for (int col = 0; col < properties.Length; col++)
            {
                var value = properties[col].GetValue(data[row]);
                worksheet.Cell(row + 2, col + 1).Value = value?.ToString();
            }
        }

        worksheet.Columns().AdjustToContents();

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);

        return memoryStream.ToArray();
    }
}
