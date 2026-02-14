using System.IO;

public static class ExportImportUtils
{
    public static readonly string[] SupportedFormats = { "csv", "xlsx" };

    public static bool IsSupportedFormat(string fileName)
    {
        var ext = Path.GetExtension(fileName)?.TrimStart('.').ToLower();
        foreach (var f in SupportedFormats)
            if (f == ext) return true;
        return false;
    }

    public static string GetFormatFilter()
    {
        return "CSV (*.csv)|*.csv|Excel (*.xlsx)|*.xlsx";
    }
} 