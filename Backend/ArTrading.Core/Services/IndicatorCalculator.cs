namespace ArTrading.Core.Services;

public static class IndicatorCalculator
{
    /// <summary>
    /// Calculate RSI using Wilder's smoothing method.
    /// Returns an array aligned with the input bars.
    /// First <paramref name="period"/> values default to 50 (neutral).
    /// </summary>
    public static decimal[] CalculateRsi(List<OhlcvBar> bars, int period = 14)
    {
        var rsi = new decimal[bars.Count];

        if (bars.Count <= period)
        {
            Array.Fill(rsi, 50m);
            return rsi;
        }

        // Calculate initial average gain/loss over the first `period` bars
        decimal avgGain = 0, avgLoss = 0;
        for (int i = 1; i <= period; i++)
        {
            var change = bars[i].Close - bars[i - 1].Close;
            if (change > 0) avgGain += change;
            else avgLoss += Math.Abs(change);
        }
        avgGain /= period;
        avgLoss /= period;

        // Fill warmup period with neutral RSI
        for (int i = 0; i < period; i++)
            rsi[i] = 50m;

        // First real RSI value
        rsi[period] = avgLoss == 0 ? 100m : 100m - (100m / (1m + avgGain / avgLoss));

        // Wilder's smoothed RSI for remaining bars
        for (int i = period + 1; i < bars.Count; i++)
        {
            var change = bars[i].Close - bars[i - 1].Close;
            var gain = change > 0 ? change : 0;
            var loss = change < 0 ? Math.Abs(change) : 0;

            avgGain = (avgGain * (period - 1) + gain) / period;
            avgLoss = (avgLoss * (period - 1) + loss) / period;

            rsi[i] = avgLoss == 0 ? 100m : 100m - (100m / (1m + avgGain / avgLoss));
        }

        return rsi;
    }
}
