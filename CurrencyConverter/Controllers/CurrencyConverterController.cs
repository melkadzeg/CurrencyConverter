using CurrencyConverter.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class CurrencyConverterController : ControllerBase
{
    private readonly ConversionService _service;
    public CurrencyConverterController(ConversionService service)
    {
        _service = service;
    }

    [HttpPost("convert")]
    public async Task<ActionResult<CurrencyConversionResponse>> Convert([FromBody] CurrencyConversionRequest request)
    {
        var result = await _service.ConvertAsync(request);
        return Ok(result);
    }
}