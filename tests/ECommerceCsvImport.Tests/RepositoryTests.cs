using ECommerceCsvImport.Models;
using ECommerceCsvImport.Services;

namespace ECommerceCsvImport.Tests;

public class RepositoryTests
{
    [Fact]
    public void Add_AddsProduct_GetAllContainsIt()
    {
        var repository = new InMemoryProductRepository();
        var product = new Product
        {
            Sku = "1001",
            Name = "Ноутбук Lenovo IdeaPad 3",
            Price = 45990m,
            Brand = "Lenovo"
        };

        repository.Add(product);

        var all = repository.GetAll();
        Assert.Single(all);
        Assert.Equal("1001", all[0].Sku);
        Assert.Equal(1, repository.Count);
    }

    [Fact]
    public void GetBySku_ReturnsProduct()
    {
        var repository = new InMemoryProductRepository();
        repository.Add(new Product { Sku = "1001", Name = "Ноутбук" });

        var found = repository.GetBySku("1001");

        Assert.NotNull(found);
        Assert.Equal("Ноутбук", found!.Name);
    }

    [Fact]
    public void GetBySku_IgnoresCaseAndWhitespace()
    {
        var repository = new InMemoryProductRepository();
        repository.Add(new Product { Sku = "1001", Name = "Ноутбук" });

        var found = repository.GetBySku(" 1001 ");

        Assert.NotNull(found);
        Assert.Equal("1001", found!.Sku);
    }

    [Fact]
    public void EmptyRepository_ReturnsEmptySnapshotAndNull()
    {
        var repository = new InMemoryProductRepository();

        Assert.Equal(0, repository.Count);
        Assert.Empty(repository.GetAll());
        Assert.Null(repository.GetBySku("1001"));
    }

    [Fact]
    public void Clear_RemovesAllProducts()
    {
        var repository = new InMemoryProductRepository();
        repository.Add(new Product { Sku = "1001" });

        repository.Clear();

        Assert.Equal(0, repository.Count);
        Assert.Empty(repository.GetAll());
    }
}
