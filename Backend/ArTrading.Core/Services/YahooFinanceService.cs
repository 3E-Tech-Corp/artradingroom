using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ArTrading.Core.Services;

public class OhlcvBar
{
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
}

public interface IMarketDataService
{
    Task<List<OhlcvBar>> GetHistoricalDataAsync(string ticker, DateTime start, DateTime end);
}

public class YahooFinanceService : IMarketDataService
{
    private readonly HttpClient _http;
    private readonly ILogger<YahooFinanceService> _logger;

    public YahooFinanceService(HttpClient http, ILogger<YahooFinanceService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<OhlcvBar>> GetHistoricalDataAsync(string ticker, DateTime start, DateTime end)
    {
        var period1 = new DateTimeOffset(start.Date, TimeSpan.Zero).ToUnixTimeSeconds();
        var period2 = new DateTimeOffset(end.Date.AddDays(1), TimeSpan.Zero).ToUnixTimeSeconds();

        var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(ticker)}" +
                  $"?period1={period1}&period2={period2}&interval=1d&events=history";

        _logger.LogInformation("Fetching Yahoo Finance data for {Ticker} from {Start} to {End}", ticker, start, end);

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

        var response = await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("Yahoo Finance returned {Status} for {Ticker}: {Body}", response.StatusCode, ticker, body);
            throw new InvalidOperationException($"Could not fetch market data for {ticker}. Yahoo Finance returned {response.StatusCode}.");
        }

        var json = await response.Content.ReadAsStringAsync();
        return ParseYahooResponse(json, ticker);
    }

    private List<OhlcvBar> ParseYahooResponse(string json, string ticker)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var result = root.GetProperty("chart").GetProperty("result")[0];
        var timestamps = result.GetProperty("timestamp");
        var quote = result.GetProperty("indicators").GetProperty("quote")[0];

        var opens = quote.GetProperty("open");
        var highs = quote.GetProperty("high");
        var lows = quote.GetProperty("low");
        var closes = quote.GetProperty("close");
        var volumes = quote.GetProperty("volume");

        var bars = new List<OhlcvBar>();

        for (int i = 0; i < timestamps.GetArrayLength(); i++)
        {
            // Skip bars with null values (holidays, missing data)
            if (closes[i].ValueKind == JsonValueKind.Null)
                continue;

            bars.Add(new OhlcvBar
            {
                Date = DateTimeOffset.FromUnixTimeSeconds(timestamps[i].GetInt64()).UtcDateTime.Date,
                Open = GetDecimalOrZero(opens[i]),
                High = GetDecimalOrZero(highs[i]),
                Low = GetDecimalOrZero(lows[i]),
                Close = GetDecimalOrZero(closes[i]),
                Volume = volumes[i].ValueKind == JsonValueKind.Null ? 0 : volumes[i].GetInt64()
            });
        }

        _logger.LogInformation("Fetched {Count} bars for {Ticker}", bars.Count, ticker);
        return bars;
    }

    private static decimal GetDecimalOrZero(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.Null) return 0;
        return el.TryGetDecimal(out var val) ? val : (decimal)el.GetDouble();
    }
}
