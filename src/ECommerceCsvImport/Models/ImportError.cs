namespace ECommerceCsvImport.Models;

/// <summary>
/// Ошибка импорта, привязанная к физическому номеру строки CSV-файла.
/// Для файловых ошибок используется RowNumber = 0.
/// </summary>
public class ImportError
{
    public int RowNumber { get; set; }
    public string Message { get; set; } = string.Empty;
}
