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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); 
}

app.MapControllers();

app.Run();
