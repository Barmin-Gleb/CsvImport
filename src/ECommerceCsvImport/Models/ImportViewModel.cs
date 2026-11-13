namespace ECommerceCsvImport.Models;

public class ImportViewModel
{
    public string FilePath { get; set; } = string.Empty;
    public string UploadedFileName { get; set; } = string.Empty;
    public ImportResult? Result { get; set; }
}
