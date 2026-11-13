using ECommerceCsvImport.Models;
using ECommerceCsvImport.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceCsvImport.Controllers;

/// <summary>
/// Тонкий контроллер: бизнес-логика импорта находится в сервисе.
/// </summary>
public class ProductsController : Controller
{
    private readonly IProductRepository _repository;
    private readonly IProductImportService _importService;
    private readonly IWebHostEnvironment _environment;

    public ProductsController(
        IProductRepository repository,
        IProductImportService importService,
        IWebHostEnvironment environment)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _importService = importService ?? throw new ArgumentNullException(nameof(importService));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    [HttpGet]
    public IActionResult Index()
    {
        var viewModel = new ProductsIndexViewModel
        {
            Products = _repository.GetAll()
        };
        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Import()
    {
        return View(new ImportViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public IActionResult Import(string? filePath, IFormFile? uploadedFile)
    {
        // Приоритет у файла, выбранного в браузере: путь к файлу на сервере
        // нужен только для демонстрации и локальных сценариев.
        if (uploadedFile is { Length: > 0 })
        {
            return ImportUploadedFile(uploadedFile, filePath);
        }

        var resolvedPath = ResolveFilePath(filePath);
        if (string.IsNullOrWhiteSpace(resolvedPath))
        {
            return View("Import", new ImportViewModel
            {
                FilePath = filePath ?? string.Empty,
                Result = CreateErrorResult("Укажите путь к CSV-файлу или выберите файл")
            });
        }

        var result = _importService.ImportFromCsv(resolvedPath);
        return View("Import", new ImportViewModel
        {
            FilePath = filePath ?? string.Empty,
            Result = result
        });
    }

    private IActionResult ImportUploadedFile(IFormFile uploadedFile, string? filePath)
    {
        var extension = Path.GetExtension(uploadedFile.FileName);
        if (!string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
        {
            return View("Import", new ImportViewModel
            {
                FilePath = filePath ?? string.Empty,
                Result = CreateErrorResult("Выберите файл с расширением .csv")
            });
        }

        var tempPath = Path.Combine(
            Path.GetTempPath(),
            $"ecommerce-import-{Guid.NewGuid():N}.csv");

        try
        {
            using (var stream = System.IO.File.Create(tempPath))
            {
                uploadedFile.CopyTo(stream);
            }

            var result = _importService.ImportFromCsv(tempPath);
            return View("Import", new ImportViewModel
            {
                FilePath = filePath ?? string.Empty,
                UploadedFileName = uploadedFile.FileName,
                Result = result
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return View("Import", new ImportViewModel
            {
                FilePath = filePath ?? string.Empty,
                Result = CreateErrorResult("Не удалось сохранить загруженный файл")
            });
        }
        finally
        {
            if (System.IO.File.Exists(tempPath))
                System.IO.File.Delete(tempPath);
        }
    }

    private static ImportResult CreateErrorResult(string message)
    {
        return new ImportResult
        {
            Errors =
            {
                new ImportError
                {
                    RowNumber = 0,
                    Message = message
                }
            }
        };
    }

    [HttpGet]
    public IActionResult Error()
    {
        return View();
    }

    /// <summary>
    /// Поддерживает как относительные пути от wwwroot (/csv/sample_products.csv),
    /// так и полные пути к файлу (C:\data\products.csv).
    /// </summary>
    private string? ResolveFilePath(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var trimmed = input.Trim();

        // Путь вида /csv/sample_products.csv считается относительным от wwwroot.
        // Проверяем его раньше Path.IsPathRooted: в Windows путь, начинающийся
        // с "/", тоже считается укоренённым (от корня текущего диска).
        if (trimmed.StartsWith('/') || trimmed.StartsWith('\\'))
        {
            trimmed = trimmed.TrimStart('/', '\\');
            var webRoot = _environment.WebRootPath
                          ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            return Path.Combine(webRoot, trimmed);
        }

        if (Path.IsPathRooted(trimmed))
            return trimmed;

        var fallbackWebRoot = _environment.WebRootPath
                              ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        return Path.Combine(fallbackWebRoot, trimmed);
    }
}
