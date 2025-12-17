using AspNetCore.Swagger.Themes;
using Microsoft.EntityFrameworkCore;
using RELEX.InventoryManager.Api;
using RELEX.InventoryManager.BusinessManager;
using RELEX.InventoryManager.Common.Configutations;
using RELEX.InventoryManager.SqlData;
using RELEX.InventoryManager.SqlData.Contexts;

var builder = WebApplication.CreateBuilder(args);

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();

// load configurations
builder.Services.Configure<InventoryOptions>(options => configuration.GetSection("InventorySettings").Bind(options));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddDatabaseContext(configuration.GetValue<string>("InventorySettings:Database:ConnectionString")!);

builder.Services.AddBusinessManagers();
builder.Services.AddBusinessManagerValidators();

builder.Services.AddControllers();

builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// execute migrations at startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<InventoryContext>();
    context.Database.Migrate();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(Theme.Dark);
}

app.MapSwagger();

app.MapControllers();

app.Run();