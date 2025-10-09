public record CurrencyConversionResponse
{
    public string SourceCurrency { get; set; }
    public string DestinationCurrency { get; set; }
    public decimal SourceAmount { get; set; }
    public decimal DestinationAmount { get; set; }
    public decimal Rate { get; set; }
}