using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Indicators;
using QuantConnect.Logging;

namespace QuantConnect.Lean.Engine
{
    /// <summary>
    /// Class to facilitate the calculation of the strategy capacity
    /// </summary>
    public class StrategyCapacity
    {
        private int _previousDay;
        private readonly Dictionary<Symbol, SymbolData> _portfolio = new Dictionary<Symbol, SymbolData>();

        /// <summary>
        /// Capacity of the strategy at different points in time
        /// </summary>
        public List<ChartPoint> Capacity { get; } = new List<ChartPoint>();

        /// <summary>
        /// Triggered on a new order event
        /// </summary>
        /// <param name="orderEvent">Order event</param>
        public virtual void OnOrderEvent(OrderEvent orderEvent)
        {
            Update(orderEvent);
        }

        /// <summary>
        /// Triggered on a new slice update
        /// </summary>
        /// <param name="data"></param>
        public virtual void OnData(Slice data)
        {
            if (data.Time.Day != _previousDay)
            {
                TakeCapacitySnapshot(data.Time);
                _previousDay = data.Time.Day;
            }

            foreach (var kvp in data.Bars)
            {
                SymbolData symbolData;
                if (!_portfolio.TryGetValue(kvp.Key, out symbolData))
                {
                    continue;
                }

                if (data.Time < symbolData.Timeout)
                {
                    symbolData.Update(kvp.Value);
                }
            }
        }

        protected virtual void Update(OrderEvent orderEvent)
        {
            var orderEventTime = orderEvent.UtcTime.ConvertFromUtc(TimeZones.NewYork);
            if (orderEventTime.Day != _previousDay)
            {
                TakeCapacitySnapshot(orderEventTime);
                _previousDay = orderEventTime.Day;
            }

            var symbol = orderEvent.Symbol;

            SymbolData symbolData;
            if (!_portfolio.TryGetValue(symbol, out symbolData))
            {
                symbolData = new SymbolData(orderEvent);
                _portfolio[symbol] = symbolData;
            }

            symbolData.SetTimeout(orderEvent);
            symbolData.Update(orderEvent);
        }

        private void TakeCapacitySnapshot(DateTime time)
        {
            var totalAbsoluteDollarVolume = _portfolio.Values.Sum(x => x.AbsoluteDollarVolume);
            var totalDollarVolume = _portfolio.Values.Sum(x => x.DollarVolume);

            var symbolByAbsoluteDollarVolume = _portfolio
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.AbsoluteDollarVolume / totalAbsoluteDollarVolume);
            var symbolByDollarVolume = _portfolio
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.DollarVolume / totalDollarVolume);

            Log.Trace($"Total Absolute Dollar Vol. {totalAbsoluteDollarVolume} -- Total Dollar Vol. {totalDollarVolume}");
            Log.Trace($"Total Absolute Dollar Vol. By Symbol: {string.Join("", symbolByAbsoluteDollarVolume.Select(kvp => "\n    " + kvp.Key + ":: " + kvp.Value.ToStringInvariant()))}\n");
            Log.Trace($"Total Volume By Symbol: {string.Join("", _portfolio.Select(kvp => "\n    " + kvp.Key + ":: " + kvp.Value.Volume.ToStringInvariant()))}\n");

            //Capacity.Add(new ChartPoint(time, minEma.Value.Ema.Current.Value / symbolByDollarVolume[minEma.Key]));
            foreach (var symbolData in _portfolio.Values)
            {
                symbolData.Reset();
            }
        }

        private class SymbolData
        {
            private const int _timeoutMinutes = 10;
            private const decimal _volumePercentage = 0.30m;

            public decimal AbsoluteDollarVolume { get; private set; }
            public decimal Volume { get; private set; }
            public decimal DollarVolume { get; private set; }
            public DateTime Timeout { get; private set; }

            public SymbolData(OrderEvent orderEvent)
            {
                SetTimeout(orderEvent);
            }

            public void Update(OrderEvent orderEvent)
            {
                AbsoluteDollarVolume += orderEvent.FillPrice * orderEvent.AbsoluteFillQuantity * 0.30m;
                DollarVolume += orderEvent.FillPrice * orderEvent.FillQuantity * 0.30m;
            }

            public void Update(TradeBar bar)
            {
                Volume += bar.Volume * _volumePercentage;
            }

            public void SetTimeout(OrderEvent orderEvent)
            {
                Timeout = orderEvent.UtcTime.ConvertFromUtc(TimeZones.NewYork).AddMinutes(_timeoutMinutes);
            }

            public void Reset()
            {
                Volume = 0;
                DollarVolume = 0;
                AbsoluteDollarVolume = 0;
            }
        }
    }
}
