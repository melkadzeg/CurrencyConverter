using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class CurrencyConverterController : ControllerBase
{
    private readonly ICurrencyConverterService _converterService;

    public CurrencyConverterController(ICurrencyConverterService converterService)
    {
        _converterService = converterService;
    }

    [HttpPost("convert")]
    public async Task<ActionResult<CurrencyConversionResponse>> Convert([FromBody] CurrencyConversionRequest request)
    {
        var result = await _converterService.ConvertAsync(request);
        return Ok(result);
    }
}