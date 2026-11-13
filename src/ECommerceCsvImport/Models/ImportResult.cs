namespace ECommerceCsvImport.Models;

/// <summary>
/// Результат импорта CSV-файла.
/// </summary>
public class ImportResult
{
    /// <summary>Количество непустых строк данных после заголовка.</summary>
    public int TotalRows { get; set; }

    /// <summary>Количество товаров, успешно добавленных в репозиторий.</summary>
    public int ImportedCount { get; set; }

    /// <summary>Список ошибок; одна строка может дать несколько ошибок.</summary>
    public List<ImportError> Errors { get; set; } = new();

    public int ErrorCount => Errors.Count;
}
