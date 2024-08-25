using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StockPriceApplication.Models;
using StockPriceApplication.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
var result = builder.Configuration.GetSection("Stocks");
builder.Services.Configure<List<StockOption>>(builder.Configuration.GetSection("Stocks"));
builder.Services.AddSingleton<ISseRelativeReturnCalculator, SseRelativeReturnCalculator>();
builder.Services.AddSingleton<ISseIndexNetChangeCalculator, SseIndexNetChangeCalculator>();
builder.Services.AddSingleton<IStockNetChangeCalculator, StockNetChangeCalculator>();

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
