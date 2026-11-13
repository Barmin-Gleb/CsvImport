using ECommerceCsvImport.Services;

// При запуске через "dotnet run --project ..." из корня решения текущая папка
// может не совпадать с папкой проекта. Находим content root по наличию wwwroot,
// чтобы статические файлы и CSV-фикстуры были доступны в любом случае.
var contentRoot = Directory.GetCurrentDirectory();
if (!Directory.Exists(Path.Combine(contentRoot, "wwwroot")))
{
    var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
    if (Directory.Exists(Path.Combine(projectRoot, "wwwroot")))
        contentRoot = projectRoot;
}

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = contentRoot
});

// Репозиторий и сервис импорта должны быть singleton: данные хранятся в памяти
// и должны сохраняться между HTTP-запросами.
builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();
builder.Services.AddSingleton<IProductImportService, ProductImportService>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Products/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Products}/{action=Index}/{id?}");

app.Run();

// Нужно для интеграционных тестов (WebApplicationFactory).
public partial class Program;
