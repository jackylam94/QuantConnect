using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Logging;
using QuantConnect.Orders;

namespace QuantConnect
{
    /// <summary>
    /// Class to facilitate the calculation of the strategy capacity
    /// </summary>
    public class StrategyCapacity
    {
        /// <summary>
        /// Capacity of the strategy at different points in time
        /// </summary>
        public List<KeyValuePair<DateTime, decimal>> Capacity { get; } = new List<KeyValuePair<DateTime, decimal>>();

        private readonly List<OrderEvent> _orderEvents = new List<OrderEvent>();
        private readonly List<Slice> _dataCache = new List<Slice>();

        /// <summary>
        /// Triggered on a new order event
        /// </summary>
        /// <param name="orderEvent">Order event</param>
        public virtual void OnOrderEvent(IEnumerable<OrderEvent> orderEvents)
        {
            _orderEvents.AddRange(orderEvents);
            Update();
        }

        /// <summary>
        /// Triggered on a new slice update
        /// </summary>
        /// <param name="data"></param>
        public virtual void OnData(Slice data)
        {
            _dataCache.Add(data);
            Update();
        }

        protected virtual void Update()
        {
            Log.Trace($"Update: \n \nLast data: {string.Join("", _dataCache.LastOrDefault()?.Bars?.Select(x => "\n  " + x.Value.EndTime.ToStringInvariant("yyyy-MM-dd HH:mm:ss")  + " :: " + x.ToString()))}\n \nOrderEvents: {string.Join("", _orderEvents.Select(x => "\n  " + x.ToString()))}");
        }
    }
}
