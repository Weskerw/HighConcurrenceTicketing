using MassTransit;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Ticketing.Core.Interfaces;
using Ticketing.Infrastructure.Data;
using Ticketing.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Controller & Endpoints
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. Redis Connection
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(redisConnectionString));

// 3. PostgreSQL & EF Core
var postgresConnectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? "Host=localhost;Database=ticketing_db;Username=postgres;Password=postgrespassword";

builder.Services.AddDbContext<TicketingDbContext>(options =>
    options.UseNpgsql(postgresConnectionString));

// 4. Custom Lock Service
builder.Services.AddScoped<ILockService, RedisLockService>();

// 5. MassTransit & RabbitMQ Integration
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();