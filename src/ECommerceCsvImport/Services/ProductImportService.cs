using System.Globalization;
using ECommerceCsvImport.Models;

namespace ECommerceCsvImport.Services;

/// <summary>
/// Сервис импорта товаров из CSV-файла с валидацией данных
/// для варианта 1 «Электроника».
/// </summary>
public class ProductImportService : IProductImportService
{
    private const string DefaultCurrency = "RUB";
    private const string DefaultCategory = "Без категории";
    private const int MinWarrantyMonths = 0;
    private const int MaxWarrantyMonths = 60;

    private static readonly string[] RequiredColumns =
    {
        "sku",
        "name",
        "description",
        "price",
        "currency",
        "category_name",
        "stock",
        "image_url",
        "is_active",
        "brand",
        "warranty_months",
        "model_number"
    };

    private readonly IProductRepository _repository;

    public ProductImportService(IProductRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public ImportResult ImportFromCsv(string filePath)
    {
        var result = new ImportResult();

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            result.Errors.Add(new ImportError
            {
                RowNumber = 0,
                Message = "Файл не найден"
            });
            return result;
        }

        string[] lines;
        try
        {
            lines = File.ReadAllLines(filePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            result.Errors.Add(new ImportError
            {
                RowNumber = 0,
                Message = "Не удалось прочитать файл"
            });
            return result;
        }

        return ImportFromLines(lines);
    }

    public ImportResult ImportFromLines(IEnumerable<string> lines)
    {
        var result = new ImportResult();
        var sourceLines = lines?.ToList() ?? new List<string>();

        // Ищем первую непустую строку — это заголовок.
        var headerIndex = sourceLines.FindIndex(l => !string.IsNullOrWhiteSpace(l));
        if (headerIndex < 0)
        {
            result.Errors.Add(new ImportError
            {
                RowNumber = 0,
                Message = "Данные для импорта отсутствуют"
            });
            return result;
        }

        var headerRowNumber = headerIndex + 1;
        var headerValues = CsvLineParser.Parse(sourceLines[headerIndex]);

        // Словарь: имя колонки -> индекс. Первое вхождение имеет приоритет,
        // сравнение без учёта регистра, имена обрезаются по краям.
        var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headerValues.Count; i++)
        {
            var columnName = headerValues[i].Trim();
            if (columnName.Length > 0 && !columnMap.ContainsKey(columnName))
                columnMap[columnName] = i;
        }

        var missingColumns = RequiredColumns
            .Where(required => !columnMap.ContainsKey(required))
            .ToList();

        if (missingColumns.Count > 0)
        {
            result.Errors.Add(new ImportError
            {
                RowNumber = headerRowNumber,
                Message = "Неверные заголовки CSV"
            });
            return result;
        }

        var expectedFieldCount = headerValues.Count;
        var importedSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = headerIndex + 1; index < sourceLines.Count; index++)
        {
            var line = sourceLines[index];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            result.TotalRows++;
            var rowNumber = index + 1; // физический номер строки в файле

            var values = CsvLineParser.Parse(line);
            if (values.Count != expectedFieldCount)
            {
                result.Errors.Add(new ImportError
                {
                    RowNumber = rowNumber,
                    Message = "Неверное количество полей в строке"
                });
                continue;
            }

            string GetValue(string columnName)
            {
                return columnMap.TryGetValue(columnName, out var fieldIndex) && fieldIndex < values.Count
                    ? values[fieldIndex]
                    : string.Empty;
            }

            var rowErrors = new List<ImportError>();

            var sku = GetValue("sku").Trim();
            if (string.IsNullOrWhiteSpace(sku))
            {
                rowErrors.Add(new ImportError
                {
                    RowNumber = rowNumber,
                    Message = "Поле sku не может быть пустым"
                });
            }

            var name = GetValue("name").Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                rowErrors.Add(new ImportError
                {
                    RowNumber = rowNumber,
                    Message = "Поле name не может быть пустым"
                });
            }

            var priceValid = TryParseDecimal(GetValue("price"), out var price) && price > 0;
            if (!priceValid)
            {
                rowErrors.Add(new ImportError
                {
                    RowNumber = rowNumber,
                    Message = "Поле price должно быть числом больше 0"
                });
            }

            var stockValid = TryParseInt(GetValue("stock"), out var stock) && stock >= 0;
            if (!stockValid)
            {
                rowErrors.Add(new ImportError
                {
                    RowNumber = rowNumber,
                    Message = "Остаток на складе должен быть целым числом не меньше 0"
                });
            }

            var activeValid = TryParseBool(GetValue("is_active"), out var isActive);
            if (!activeValid)
            {
                rowErrors.Add(new ImportError
                {
                    RowNumber = rowNumber,
                    Message = "Поле is_active должно иметь значение true, false, 1 или 0"
                });
            }

            var brand = GetValue("brand").Trim();
            if (string.IsNullOrWhiteSpace(brand))
            {
                rowErrors.Add(new ImportError
                {
                    RowNumber = rowNumber,
                    Message = "Поле brand обязательно для варианта «Электроника»"
                });
            }

            var warrantyValid = TryParseInt(GetValue("warranty_months"), out var warrantyMonths)
                                && warrantyMonths >= MinWarrantyMonths
                                && warrantyMonths <= MaxWarrantyMonths;
            if (!warrantyValid)
            {
                rowErrors.Add(new ImportError
                {
                    RowNumber = rowNumber,
                    Message = "Гарантия должна быть целым числом от 0 до 60 месяцев"
                });
            }

            if (rowErrors.Count > 0)
            {
                result.Errors.AddRange(rowErrors);
                continue;
            }

            var isDuplicate = _repository.GetBySku(sku) is not null || importedSkus.Contains(sku);
            if (isDuplicate)
            {
                result.Errors.Add(new ImportError
                {
                    RowNumber = rowNumber,
                    Message = "Артикул товара уже существует"
                });
                continue;
            }

            var product = new Product
            {
                Sku = sku,
                Name = name,
                Description = GetValue("description").Trim(),
                Price = price,
                Currency = NormalizeCurrency(GetValue("currency")),
                CategoryName = NormalizeCategory(GetValue("category_name")),
                StockQuantity = stock,
                ImageUrl = GetValue("image_url").Trim(),
                IsActive = isActive,
                Brand = brand,
                WarrantyMonths = warrantyMonths,
                ModelNumber = GetValue("model_number").Trim()
            };

            _repository.Add(product);
            importedSkus.Add(sku);
            result.ImportedCount++;
        }

        if (result.TotalRows == 0 && result.Errors.Count == 0)
        {
            result.Errors.Add(new ImportError
            {
                RowNumber = 0,
                Message = "Данные для импорта отсутствуют"
            });
        }

        return result;
    }

    private static string NormalizeCurrency(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? DefaultCurrency
            : value.Trim().ToUpperInvariant();
    }

    private static string NormalizeCategory(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? DefaultCategory
            : value.Trim();
    }

    private static bool TryParseDecimal(string value, out decimal result)
    {
        return decimal.TryParse(
            value?.Trim(),
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out result);
    }

    private static bool TryParseInt(string value, out int result)
    {
        return int.TryParse(
            value?.Trim(),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out result);
    }

    private static bool TryParseBool(string value, out bool result)
    {
        var trimmed = value?.Trim();
        if (bool.TryParse(trimmed, out result))
            return true;

        if (trimmed == "1")
        {
            result = true;
            return true;
        }

        if (trimmed == "0")
        {
            result = false;
            return true;
        }

        return false;
    }
}
