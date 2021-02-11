using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Indicators;

namespace QuantConnect.Lean.Engine
{
    /// <summary>
    /// Class to facilitate the calculation of the strategy capacity
    /// </summary>
    public class StrategyCapacity
    {
        /// <summary>
        /// Capacity of the strategy at different points in time
        /// </summary>
        public List<ChartPoint> Capacity { get; } = new List<ChartPoint>();

        private readonly Dictionary<Symbol, SymbolData> _portfolio = new Dictionary<Symbol, SymbolData>();
        private DateTime _previousEvent;
        private Slice _data;
        private readonly Dictionary<Symbol, TradeBar> _dataCache;

        public StrategyCapacity(DateTime startDate)
        {
            _previousEvent = startDate;
            _dataCache = new Dictionary<Symbol, TradeBar>();
        }

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
            _data = data;
            foreach (var kvp in data.Bars)
            {
                _dataCache[kvp.Key] = kvp.Value;
            }
        }

        protected virtual void Update(OrderEvent orderEvent)
        {
            var lastTime = orderEvent.UtcTime;
            var symbol = orderEvent.Symbol;

            if (_previousEvent.Month != lastTime.Month)
            {
                TakeCapacitySnapshot(lastTime);
            }

            _previousEvent = lastTime;

            TradeBar bar;
            if (!_dataCache.TryGetValue(symbol, out bar) || orderEvent.Status != OrderStatus.Filled)
            {
                return;
            }

            SymbolData symbolData;
            if (!_portfolio.TryGetValue(symbol, out symbolData))
            {
                symbolData = new SymbolData(bar);
                _portfolio[symbol] = symbolData;
            }

            symbolData.Update(bar, orderEvent);
        }

        private void TakeCapacitySnapshot(DateTime time)
        {
            var totalDollarVolume = _portfolio.Values.Sum(x => x.DollarVolume);
            var symbolByDollarVolume = _portfolio
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.DollarVolume / totalDollarVolume);

            var minEma = _portfolio
                .OrderBy(kvp => kvp.Value.Ema.Current.Value)
                .First();

            Capacity.Add(new ChartPoint(time, minEma.Value.Ema.Current.Value / symbolByDollarVolume[minEma.Key]));
        }

        private class SymbolData
        {
            public ExponentialMovingAverage Ema { get; }
            public decimal Price { get; private set; }

            public decimal DollarVolume { get; private set; }

            public SymbolData(TradeBar bar, int period = 10)
            {
                Ema = new ExponentialMovingAverage(period);
            }

            public void Update(TradeBar bar, OrderEvent orderEvent)
            {
                Ema.Update(bar.EndTime, bar.Close * bar.Volume * 0.05m);

                Price = bar.Close;
                DollarVolume += orderEvent.FillPrice * orderEvent.AbsoluteFillQuantity;
            }
        }
    }
}
