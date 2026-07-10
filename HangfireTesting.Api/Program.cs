using Hangfire;
using HangfireTesting.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// 1. Register Hangfire services + choose storage
builder.Services.AddHangfire(cfg => cfg.UseInMemoryStorage());

// 2. Register the background server that actually runs jobs
//builder.Services.AddHangfireServer();

builder.Services.AddHangfireServer(options =>
{
    options.SchedulePollingInterval = TimeSpan.FromSeconds(1);
});

builder.Services.AddScoped<JobService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                          // serves /openapi/v1.json
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "HangFire Testing API v1");
    });
}

// 3. Expose the dashboard at /hangfire
app.UseHangfireDashboard();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
