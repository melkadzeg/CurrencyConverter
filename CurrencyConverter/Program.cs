using CurrencyConverter.Providers;
using CurrencyConverter.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

builder.Services.AddScoped<IFiatProvider, NbuProvider>();
builder.Services.AddScoped<ICryptoProvider, KrakenProvider>();
builder.Services.AddScoped<ConversionService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CurrencyConverter API V1");
    c.RoutePrefix = string.Empty; 
});

app.MapControllers();

app.Run();
