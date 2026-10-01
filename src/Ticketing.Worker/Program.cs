using MassTransit;
using Microsoft.EntityFrameworkCore;
using Ticketing.Infrastructure.Data;
using Ticketing.Worker.Consumers;

var builder = Host.CreateApplicationBuilder(args);

// 1. PostgreSQL & DbContext
var postgresConnectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? "Host=localhost;Database=ticketing_db;Username=postgres;Password=postgrespassword";

builder.Services.AddDbContext<TicketingDbContext>(options =>
    options.UseNpgsql(postgresConnectionString));

// 2. MassTransit Consumer Setup
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<TicketReservationConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // Configura automaticamente l'endpoint per la coda del Consumer
        cfg.ReceiveEndpoint("ticket-reservation-queue", e =>
        {
            // Pattern di resilienza: 3 tentativi di retry con backoff esponenziale in caso di eccezioni temporanee
            e.UseMessageRetry(r => r.Exponential(3, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(200)));

            e.ConfigureConsumer<TicketReservationConsumer>(context);
        });
    });
});

var host = builder.Build();
host.Run();