public class ExportImportTableParams
{
    public string TableName { get; set; }
    public string Format { get; set; } // csv или xlsx
    public string FilePath { get; set; } // путь к файлу для импорта/экспорта
} 