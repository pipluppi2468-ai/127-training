using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using OrderHub.Core.Interfaces;
using OrderHub.Core.Services;
using OrderHub.Infrastructure.Data;
using OrderHub.Infrastructure.Repositories;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<OrderHubDbContext>(options =>
    options.UseSqlServer("Server=localhost/MSSQLSERVER16;Database=OrderHubTraining;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"));

builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<OrderHubTools>();

await builder.Build().RunAsync();
