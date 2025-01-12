using Microsoft.EntityFrameworkCore;
using WebServer.Controllers;
using WebServer.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register the DBManager with the SQLite database
builder.Services.AddDbContext<DBManager>(options =>
    options.UseSqlite("Data Source=Clients.db")
           .EnableSensitiveDataLogging()  // Enable logging for query details
           .LogTo(Console.WriteLine)      // Log details to the console
);
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
