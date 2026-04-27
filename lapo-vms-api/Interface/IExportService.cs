namespace lapo_vms_api.Interface;

public interface IExportService
{
    byte[] ExportToCsv<T>(List<T> data);
    byte[] ExportToExcel<T>(List<T> data, string sheetName = "Sheet1");
}
