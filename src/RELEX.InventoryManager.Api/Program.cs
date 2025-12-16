using Microsoft.EntityFrameworkCore;
using RELEX.InventoryManager.BusinessManager.Contracts;
using RELEX.InventoryManager.BusinessManager.Managers;
using RELEX.InventoryManager.SqlData.Contexts;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// hook up PostgreSQL
builder.Services.AddDbContext<InventoryContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IInventoryContext, InventoryContext>();

builder.Services.AddScoped<IOrderManager, OrderManager>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<InventoryContext>();
    context.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapSwagger();

app.MapControllers();

app.Run();