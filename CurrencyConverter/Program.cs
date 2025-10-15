using CurrencyConverter.Providers;
using CurrencyConverter.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

//builder.Services.AddScoped<ICurrencyConverterService, CurrencyConverterService>();
//builder.Services.AddScoped<ICurrencyRateProvider, CurrencyRateProvider>();

builder.Services.AddScoped<IFiatProvider, NbuProvider>();
builder.Services.AddScoped<ICryptoProvider, KrakenProvider>();
builder.Services.AddScoped<ConversionService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); 
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
