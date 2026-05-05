namespace ArTrading.Core.Services;

public static class IndicatorCalculator
{
    /// <summary>RSI using Wilder's smoothing. First <paramref name="period"/> values = 50 (neutral).</summary>
    public static decimal[] CalculateRsi(List<OhlcvBar> bars, int period = 14)
    {
        var rsi = new decimal[bars.Count];

        if (bars.Count <= period)
        {
            Array.Fill(rsi, 50m);
            return rsi;
        }

        decimal avgGain = 0, avgLoss = 0;
        for (int i = 1; i <= period; i++)
        {
            var change = bars[i].Close - bars[i - 1].Close;
            if (change > 0) avgGain += change;
            else avgLoss += Math.Abs(change);
        }
        avgGain /= period;
        avgLoss /= period;

        for (int i = 0; i < period; i++)
            rsi[i] = 50m;

        rsi[period] = avgLoss == 0 ? 100m : 100m - (100m / (1m + avgGain / avgLoss));

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

    /// <summary>Simple Moving Average. Returns NaN-equivalent (0) for warmup bars.</summary>
    public static decimal[] CalculateSma(List<OhlcvBar> bars, int period)
    {
        var sma = new decimal[bars.Count];

        for (int i = 0; i < bars.Count; i++)
        {
            if (i < period - 1)
            {
                sma[i] = bars[i].Close; // use current price during warmup so no false signals
                continue;
            }

            decimal sum = 0;
            for (int j = i - period + 1; j <= i; j++)
                sum += bars[j].Close;

            sma[i] = sum / period;
        }

        return sma;
    }

    /// <summary>Exponential Moving Average.</summary>
    public static decimal[] CalculateEma(List<OhlcvBar> bars, int period)
    {
        var ema = new decimal[bars.Count];
        var k = 2m / (period + 1);

        ema[0] = bars[0].Close;
        for (int i = 1; i < bars.Count; i++)
            ema[i] = bars[i].Close * k + ema[i - 1] * (1 - k);

        return ema;
    }

    /// <summary>
    /// Returns % distance of price above/below an SMA.
    /// Positive = price above SMA, negative = price below.
    /// e.g. PRICE_VS_SMA50 > 0 means "price is above the 50-day SMA"
    /// </summary>
    public static decimal[] PriceVsSma(List<OhlcvBar> bars, int period)
    {
        var sma = CalculateSma(bars, period);
        var result = new decimal[bars.Count];

        for (int i = 0; i < bars.Count; i++)
            result[i] = sma[i] == 0 ? 0 : (bars[i].Close - sma[i]) / sma[i] * 100m;

        return result;
    }

    /// <summary>
    /// SMA crossover signal: returns positive when fast SMA is above slow SMA.
    /// e.g. SMA_CROSS_50_200 > 0 = golden cross (50-day above 200-day).
    /// </summary>
    public static decimal[] SmaCrossover(List<OhlcvBar> bars, int fastPeriod, int slowPeriod)
    {
        var fast = CalculateSma(bars, fastPeriod);
        var slow = CalculateSma(bars, slowPeriod);
        var result = new decimal[bars.Count];

        for (int i = 0; i < bars.Count; i++)
            result[i] = slow[i] == 0 ? 0 : (fast[i] - slow[i]) / slow[i] * 100m;

        return result;
    }

    /// <summary>
    /// Bollinger Band position: returns % of how far price is from the middle band,
    /// normalized to the band width. 0 = at middle, 100 = at upper band, -100 = at lower band.
    /// </summary>
    public static decimal[] BollingerPosition(List<OhlcvBar> bars, int period = 20, decimal stdDevMultiplier = 2m)
    {
        var sma = CalculateSma(bars, period);
        var result = new decimal[bars.Count];

        for (int i = 0; i < bars.Count; i++)
        {
            if (i < period - 1) { result[i] = 0; continue; }

            decimal variance = 0;
            for (int j = i - period + 1; j <= i; j++)
                variance += (bars[j].Close - sma[i]) * (bars[j].Close - sma[i]);
            var stdDev = (decimal)Math.Sqrt((double)(variance / period));

            var upperBand = sma[i] + stdDevMultiplier * stdDev;
            var lowerBand = sma[i] - stdDevMultiplier * stdDev;
            var bandwidth = upperBand - lowerBand;

            result[i] = bandwidth == 0 ? 0 : (bars[i].Close - lowerBand) / bandwidth * 200m - 100m;
        }

        return result;
    }
}
