public record CurrencyConversionRequest
{
    public string SourceCurrency { get; set; }
    public string DestinationCurrency { get; set; }
    public decimal SourceAmount { get; set; }
}