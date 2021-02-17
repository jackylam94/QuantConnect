using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Logging;

namespace QuantConnect.Lean.Engine
{
    /// <summary>
    /// Class to facilitate the calculation of the strategy capacity
    /// </summary>
    public class StrategyCapacity
    {
        private int _previousMonth;
        private readonly Dictionary<Symbol, DateTimeZone> _timeZones;
        private readonly Dictionary<Symbol, SymbolData> _portfolio;

        /// <summary>
        /// Capacity of the strategy at different points in time
        /// </summary>
        public List<ChartPoint> Capacity { get; } = new List<ChartPoint>();

        public StrategyCapacity(Dictionary<Symbol, DateTimeZone> timeZones)
        {
            _timeZones = timeZones;
            _portfolio = new Dictionary<Symbol, SymbolData>();
        }

        /// <summary>
        /// Triggered on a new slice update
        /// </summary>
        /// <param name="data"></param>
        public virtual void OnData(Slice data)
        {
            if (data.Time.Month != _previousMonth && _previousMonth != 0)
            {
                TakeCapacitySnapshot(data.Time);
            }

            foreach (var kvp in data.Bars)
            {
                SymbolData symbolData;
                if (!_portfolio.TryGetValue(kvp.Key, out symbolData))
                {
                    symbolData = new SymbolData(_timeZones[kvp.Key]);
                    _portfolio[kvp.Key] = symbolData;
                }

                symbolData.OnData(kvp.Value);
            }

            _previousMonth = data.Time.Month;
        }

        /// <summary>
        /// Triggered on a new order event
        /// </summary>
        /// <param name="orderEvent">Order event</param>
        public virtual void OnOrderEvent(OrderEvent orderEvent)
        {
            var symbol = orderEvent.Symbol;

            SymbolData symbolData;
            if (!_portfolio.TryGetValue(symbol, out symbolData))
            {
                symbolData = new SymbolData(_timeZones[symbol]);
                _portfolio[symbol] = symbolData;
            }

            symbolData.OnOrderEvent(orderEvent);
        }

        private void TakeCapacitySnapshot(DateTime time)
        {
            if (_portfolio.Values.All(x => !x.TradedBetweenSnapshots))
            {
                ResetData();
                return;
            }

            var totalAbsoluteSymbolDollarVolume = _portfolio.Values
                .Sum(x => x.AbsoluteTradingDollarVolume);

            var symbolByPercentageOfAbsoluteDollarVolume = _portfolio
                .Where(kvp => kvp.Value.TradedBetweenSnapshots)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.AbsoluteTradingDollarVolume / totalAbsoluteSymbolDollarVolume);

            Log.Trace($"Total Absolute Dollar Vol. By Symbol: {string.Join("", symbolByPercentageOfAbsoluteDollarVolume.Select(kvp => "\n    " + kvp.Key + ":: " + kvp.Value.ToStringInvariant()))}\n");
            //Log.Trace($"Total Average Capacity By Symbol: {string.Join("", _portfolio.Select(kvp => "\n    " + kvp.Key + ":: " + kvp.Value.AverageCapacity.ToStringInvariant()))}\n");

            var minimumMarketVolume = _portfolio
                .Where(kvp => kvp.Value.TradedBetweenSnapshots)
                .OrderBy(kvp => kvp.Value.AverageCapacity)
                .FirstOrDefault();

            Log.Trace($"Minimum Symbol Average Capacity: {minimumMarketVolume.Key} :: {minimumMarketVolume.Value.AverageCapacity}");

            Capacity.Add(new ChartPoint(time, (minimumMarketVolume.Value.AverageCapacity) / symbolByPercentageOfAbsoluteDollarVolume[minimumMarketVolume.Key]));

            ResetData();
        }

        protected void ResetData()
        {
            foreach (var symbolData in _portfolio.Values)
            {
                symbolData.Reset();
            }
        }

        private class SymbolData
        {
            private TradeBar _previousBar;
            private OrderEvent _previousTrade;
            public decimal AverageCapacity => (_marketCapacityDollarVolume / TradeCount) * 0.10m;

            private DateTime _timeout;
            private double _fastTradingVolumeDiscountFactor;
            private decimal _averageVolume;
            private readonly DateTimeZone _timeZone;

            public bool TradedBetweenSnapshots { get; private set; }

            public int TradeCount { get; private set; }
            public decimal AbsoluteTradingDollarVolume { get; private set; }
            private decimal _marketCapacityDollarVolume;

            public SymbolData(DateTimeZone timeZone)
            {
                _timeZone = timeZone;
                _fastTradingVolumeDiscountFactor = 1;
            }

            public void OnOrderEvent(OrderEvent orderEvent)
            {
                TradedBetweenSnapshots = true;
                AbsoluteTradingDollarVolume += orderEvent.FillPrice * orderEvent.AbsoluteFillQuantity;
                TradeCount++;

                // Use 6000000 as the maximum bound for trading volume in a single minute.
                // Any bars that exceed 6 million total volume will be capped to a timeout of one minute.
                var k = _averageVolume != 0
                    ? 6000000 / _averageVolume
                    : 10;

                var timeoutMinutes = k > 60 ? 60 : (int)Math.Max(1, (double)k);

                // To reduce the capacity of high frequency strategies, we scale down the
                // volume captured on each bar proportional to the trades per day.
                _fastTradingVolumeDiscountFactor = 2 * (((orderEvent.UtcTime - (_previousTrade?.UtcTime ?? orderEvent.UtcTime.AddDays(-1))).TotalMinutes) / 390);
                _fastTradingVolumeDiscountFactor = _fastTradingVolumeDiscountFactor > 1 ? 1 : Math.Max(0.01, _fastTradingVolumeDiscountFactor);

                // When trades occur within 10 minutes the total volume we will capture is implicitly limited
                // because of the reduced time that we're capturing the volume
                _timeout = orderEvent.UtcTime.ConvertFromUtc(_timeZone).AddMinutes(timeoutMinutes);
                _previousTrade = orderEvent;
            }

            public void OnData(TradeBar bar)
            {
                var absoluteMarketDollarVolume = bar.Close * bar.Volume;
                if (_previousBar == null)
                {
                    _previousBar = bar;
                    _averageVolume = bar.Volume;

                    return;
                }

                // If we have an illiquid stock, we will get bars that might not be continuous
                _averageVolume = (bar.Volume + _previousBar.Volume) / (decimal)(bar.EndTime - _previousBar.EndTime).TotalMinutes;

                if (bar.EndTime <= _timeout)
                {
                    _marketCapacityDollarVolume += absoluteMarketDollarVolume * (decimal)_fastTradingVolumeDiscountFactor;
                }

                _previousBar = bar;
            }

            public void Reset()
            {
                TradedBetweenSnapshots = false;

                _marketCapacityDollarVolume = 0;
                AbsoluteTradingDollarVolume = 0;
                TradeCount = 0;
            }
        }
    }
}
