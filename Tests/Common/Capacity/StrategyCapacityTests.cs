using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Packets;
using QuantConnect.ToolBox;

namespace QuantConnect.Tests.Common.Capacity
{
    [TestFixture]
    public class StrategyCapacityTests
    {
        public static readonly Dictionary<VolumeCap, List<Symbol>> _symbolsByCapacity = new Dictionary<VolumeCap, List<Symbol>>
        {
            { VolumeCap.Micro, new List<Symbol> {
                Symbol.Create("AADR", SecurityType.Equity, Market.USA),
                Symbol.Create("AAMC", SecurityType.Equity, Market.USA),
                Symbol.Create("AAU", SecurityType.Equity, Market.USA),
                Symbol.Create("ABDC", SecurityType.Equity, Market.USA),
                Symbol.Create("ABIO", SecurityType.Equity, Market.USA),
                Symbol.Create("ABUS", SecurityType.Equity, Market.USA),
                Symbol.Create("AC", SecurityType.Equity, Market.USA),
                Symbol.Create("ACER", SecurityType.Equity, Market.USA),
                Symbol.Create("ACES", SecurityType.Equity, Market.USA),
                Symbol.Create("ACGLO", SecurityType.Equity, Market.USA),
                Symbol.Create("ACH", SecurityType.Equity, Market.USA),
                Symbol.Create("ACHV", SecurityType.Equity, Market.USA),
                Symbol.Create("ACIO", SecurityType.Equity, Market.USA),
                Symbol.Create("ACIU", SecurityType.Equity, Market.USA),
                Symbol.Create("ACNB", SecurityType.Equity, Market.USA),
                Symbol.Create("ACRS", SecurityType.Equity, Market.USA),
                Symbol.Create("ACSI", SecurityType.Equity, Market.USA),
                Symbol.Create("ACT", SecurityType.Equity, Market.USA),
                Symbol.Create("ACT", SecurityType.Equity, Market.USA),
                Symbol.Create("ACTG", SecurityType.Equity, Market.USA)
            }},
            { VolumeCap.Small, new List<Symbol> {
                Symbol.Create("ZYNE", SecurityType.Equity, Market.USA),
                Symbol.Create("ZYME", SecurityType.Equity, Market.USA),
                Symbol.Create("ZUO", SecurityType.Equity, Market.USA),
                Symbol.Create("ZUMZ", SecurityType.Equity, Market.USA),
                Symbol.Create("ZTR", SecurityType.Equity, Market.USA),
                Symbol.Create("ZSL", SecurityType.Equity, Market.USA),
                Symbol.Create("ZSAN", SecurityType.Equity, Market.USA),
                Symbol.Create("ZROZ", SecurityType.Equity, Market.USA),
                Symbol.Create("ZLAB", SecurityType.Equity, Market.USA),
                Symbol.Create("ZIXI", SecurityType.Equity, Market.USA),
                Symbol.Create("ZIV", SecurityType.Equity, Market.USA),
                Symbol.Create("ZIOP", SecurityType.Equity, Market.USA),
                Symbol.Create("ZGNX", SecurityType.Equity, Market.USA),
                Symbol.Create("ZG", SecurityType.Equity, Market.USA),
                Symbol.Create("ZEUS", SecurityType.Equity, Market.USA),
                Symbol.Create("ZAGG", SecurityType.Equity, Market.USA),
                Symbol.Create("YYY", SecurityType.Equity, Market.USA),
                Symbol.Create("YRD", SecurityType.Equity, Market.USA),
                Symbol.Create("YRCW", SecurityType.Equity, Market.USA),
                Symbol.Create("YPF", SecurityType.Equity, Market.USA)
            }},
            { VolumeCap.Medium, new List<Symbol> {
                Symbol.Create("AA", SecurityType.Equity, Market.USA),
                Symbol.Create("AAN", SecurityType.Equity, Market.USA),
                Symbol.Create("AAP", SecurityType.Equity, Market.USA),
                Symbol.Create("AAXN", SecurityType.Equity, Market.USA),
                Symbol.Create("ABB", SecurityType.Equity, Market.USA),
                Symbol.Create("ABC", SecurityType.Equity, Market.USA),
                Symbol.Create("ACAD", SecurityType.Equity, Market.USA),
                Symbol.Create("ACC", SecurityType.Equity, Market.USA),
                Symbol.Create("ACGL", SecurityType.Equity, Market.USA),
                Symbol.Create("ACIW", SecurityType.Equity, Market.USA),
                Symbol.Create("ACM", SecurityType.Equity, Market.USA),
                Symbol.Create("ACWV", SecurityType.Equity, Market.USA),
                Symbol.Create("ACWX", SecurityType.Equity, Market.USA),
                Symbol.Create("ADM", SecurityType.Equity, Market.USA),
                Symbol.Create("ADPT", SecurityType.Equity, Market.USA),
                Symbol.Create("ADS", SecurityType.Equity, Market.USA),
                Symbol.Create("ADUS", SecurityType.Equity, Market.USA),
                Symbol.Create("AEM", SecurityType.Equity, Market.USA),
                Symbol.Create("AEO", SecurityType.Equity, Market.USA),
                Symbol.Create("AEP", SecurityType.Equity, Market.USA)
            }},
            { VolumeCap.Large, new List<Symbol> {
                Symbol.Create("ZTS", SecurityType.Equity, Market.USA),
                Symbol.Create("YUM", SecurityType.Equity, Market.USA),
                Symbol.Create("XLY", SecurityType.Equity, Market.USA),
                Symbol.Create("XLV", SecurityType.Equity, Market.USA),
                Symbol.Create("XLRE", SecurityType.Equity, Market.USA),
                Symbol.Create("XLP", SecurityType.Equity, Market.USA),
                Symbol.Create("XLNX", SecurityType.Equity, Market.USA),
                Symbol.Create("XLF", SecurityType.Equity, Market.USA),
                Symbol.Create("XLC", SecurityType.Equity, Market.USA),
                Symbol.Create("XLB", SecurityType.Equity, Market.USA),
                Symbol.Create("XEL", SecurityType.Equity, Market.USA),
                Symbol.Create("XBI", SecurityType.Equity, Market.USA),
                Symbol.Create("X", SecurityType.Equity, Market.USA),
                Symbol.Create("WYNN", SecurityType.Equity, Market.USA),
                Symbol.Create("WW", SecurityType.Equity, Market.USA),
                Symbol.Create("WORK", SecurityType.Equity, Market.USA),
                Symbol.Create("WMB", SecurityType.Equity, Market.USA),
                Symbol.Create("WM", SecurityType.Equity, Market.USA),
                Symbol.Create("WELL", SecurityType.Equity, Market.USA),
                Symbol.Create("WEC", SecurityType.Equity, Market.USA)
            }},
            { VolumeCap.Mega, new List<Symbol> {
                Symbol.Create("AAPL", SecurityType.Equity, Market.USA),
                Symbol.Create("ADBE", SecurityType.Equity, Market.USA),
                Symbol.Create("AGG", SecurityType.Equity, Market.USA),
                Symbol.Create("AMD", SecurityType.Equity, Market.USA),
                Symbol.Create("AMZN", SecurityType.Equity, Market.USA),
                Symbol.Create("BA", SecurityType.Equity, Market.USA),
                Symbol.Create("BABA", SecurityType.Equity, Market.USA),
                Symbol.Create("BAC", SecurityType.Equity, Market.USA),
                Symbol.Create("BMY", SecurityType.Equity, Market.USA),
                Symbol.Create("C", SecurityType.Equity, Market.USA),
                Symbol.Create("CMCSA", SecurityType.Equity, Market.USA),
                Symbol.Create("CRM", SecurityType.Equity, Market.USA),
                Symbol.Create("CSCO", SecurityType.Equity, Market.USA),
                Symbol.Create("DIS", SecurityType.Equity, Market.USA),
                Symbol.Create("EEM", SecurityType.Equity, Market.USA),
                Symbol.Create("EFA", SecurityType.Equity, Market.USA),
                Symbol.Create("FB", SecurityType.Equity, Market.USA),
                Symbol.Create("FXI", SecurityType.Equity, Market.USA),
                Symbol.Create("GDX", SecurityType.Equity, Market.USA),
                Symbol.Create("GE", SecurityType.Equity, Market.USA)
            }}
        };

        public enum VolumeCap
        {
            Micro,
            Small,
            Medium,
            Large,
            Mega
        }

        [Test]
        public void TestCapacity()
        {
            var strategyCapacity = new StrategyCapacity();
            var resolutions = new[] { /*Resolution.Minute, Resolution.Hour, */Resolution.Daily };
            var timeZone = TimeZones.NewYork;
            var orders = JsonConvert.DeserializeObject<BacktestResult>(File.ReadAllText(Path.Combine("Common", "Capacity", "example_strategy.json")), new OrderJsonConverter())
                .Orders
                .Values
                .OrderBy(o => o.Time)
                .ToList();

            var start = new DateTime(2020, 1, 1);
            var end = new DateTime(2020, 1, 30);

            var readers = new List<IEnumerator<BaseData>>();
            foreach (var symbol in _symbolsByCapacity.Values.SelectMany(s => s))
            {
                foreach (var resolution in resolutions)
                {
                    var config = new SubscriptionDataConfig(typeof(TradeBar), symbol, resolution, timeZone, timeZone, true, false, false);
                    readers.Add(new LeanDataReader(config, symbol, resolution, end, Globals.DataFolder).Parse().GetEnumerator());
                }
            }

            var dataEnumerators = readers.ToArray();
            var synchronizer = new SynchronizingEnumerator(dataEnumerators);

            var dataBinnedByTime = new List<List<BaseData>>();
            var currentData = new List<BaseData>();
            var currentTime = DateTime.MinValue;

            while (synchronizer.MoveNext())
            {
                if (synchronizer.Current == null || synchronizer.Current.EndTime > end)
                {
                    break;
                }

                if (synchronizer.Current.EndTime < start)
                {
                    continue;
                }

                if (currentTime == DateTime.MinValue)
                {
                    currentTime = synchronizer.Current.EndTime;
                }

                if (currentTime != synchronizer.Current.EndTime)
                {
                    dataBinnedByTime.Add(currentData);
                    currentData = new List<BaseData>();
                    currentData.Add(synchronizer.Current);
                    currentTime = synchronizer.Current.EndTime;

                    continue;
                }

                currentData.Add(synchronizer.Current);
            }

            var cursor = 0;

            foreach (var dataBin in dataBinnedByTime)
            {
                var dataTime = dataBin[0].EndTime;
                var slice = new Slice(dataTime, dataBin);
                var orderEvents = new List<OrderEvent>();

                while (cursor < orders.Count)
                {
                    var order = orders[cursor];
                    if (order.Time.ConvertFromUtc(timeZone) > dataTime)
                    {
                        break;
                    }

                    var orderEvent = new OrderEvent(order, order.Time, OrderFee.Zero);
                    orderEvent.FillPrice = order.Price;
                    orderEvent.FillQuantity = order.Quantity;

                    orderEvents.Add(orderEvent);
                    cursor++;
                }

                strategyCapacity.OnData(slice);
                strategyCapacity.OnOrderEvent(orderEvents);
            }

            Assert.AreEqual(new List<KeyValuePair<DateTime, decimal>>(), strategyCapacity.Capacity);
        }
    }
}
