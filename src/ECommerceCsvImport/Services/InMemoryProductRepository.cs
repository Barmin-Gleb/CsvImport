using ECommerceCsvImport.Models;

namespace ECommerceCsvImport.Services;

/// <summary>
/// Потокобезопасный репозиторий товаров в памяти. Регистрируется как singleton,
/// поэтому данные сохраняются между HTTP-запросами.
/// </summary>
public class InMemoryProductRepository : IProductRepository
{
    private readonly List<Product> _products = new();
    private readonly object _lock = new();

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _products.Count;
            }
        }
    }

    public void Add(Product product)
    {
        if (product is null)
            throw new ArgumentNullException(nameof(product));

        lock (_lock)
        {
            _products.Add(product);
        }
    }

    public IReadOnlyList<Product> GetAll()
    {
        lock (_lock)
        {
            // Возвращаем снапшот, чтобы внешнее изменение не влияло на хранилище.
            return _products.ToList();
        }
    }

    public Product? GetBySku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return null;

        lock (_lock)
        {
            return _products.FirstOrDefault(p =>
                string.Equals(p.Sku, sku.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _products.Clear();
        }
    }
}
