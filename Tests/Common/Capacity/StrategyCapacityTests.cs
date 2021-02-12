using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Packets;
using QuantConnect.Tests.Common.Capacity.Strategies;
using QuantConnect.ToolBox;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Capacity
{
    [TestFixture]
    public class StrategyCapacityTests
    {
        [TestCase(nameof(SpyBondPortfolioRebalance), 57106809)]
        [TestCase(nameof(BeastVsPenny), 206303)]
        [TestCase(nameof(MonthlyRebalanceHourly), 43261380)]
        [TestCase(nameof(MonthlyRebalanceDaily), 470738228)]
        [TestCase(nameof(IntradayMinuteScalping), 6117862)]
        [TestCase(nameof(EmaPortfolioRebalance100), 2893)]
        public void TestCapacity(string strategy, int expectedCapacity)
        {
            var resolution = Resolution.Minute;
            var timeZone = TimeZones.NewYork;
            var orders = JsonConvert.DeserializeObject<BacktestResult>(File.ReadAllText(Path.Combine("Common", "Capacity", "Strategies", $"{strategy}.json")), new OrderJsonConverter())
                .Orders
                .Values
                .OrderBy(o => o.Time)
                .ToList();

            if (orders.Count == 0)
            {
                throw new Exception("Expected non-zero amount of orders");
            }

            var start = orders[0].Time;
            // Add a buffer of 1 day so that orders placed in the trading day
            // are snapshotted. In the case of MonthlyRebalanceDaily, the last data point we get
            // is at 2020-04-01 00:00:00 Eastern Time, but our last order came in on 12:00:00 Eastern time of the same day.
            // We need a buffer of at least 10 minutes, which afterwards the data will stop updating the statistics and no
            // new snapshots will be generated
            var end = orders[orders.Count - 1].Time.AddDays(1);

            var strategyCapacity = new StrategyCapacity();

            var readers = new List<IEnumerator<BaseData>>();
            var symbols = orders.Select(x => x.Symbol).ToHashSet();
            foreach (var symbol in symbols)
            {
                var config = new SubscriptionDataConfig(typeof(TradeBar), symbol, resolution, timeZone, timeZone, true, false, false);
                foreach (var date in Time.EachDay(start, end))
                {
                    if (File.Exists(LeanData.GenerateZipFilePath(Globals.DataFolder, symbol, date, resolution, config.TickType)))
                    {
                        readers.Add(new LeanDataReader(config, symbol, resolution, date, Globals.DataFolder).Parse().GetEnumerator());
                    }
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

            if (currentData.Count != 0)
            {
                dataBinnedByTime.Add(currentData);
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
                foreach (var orderEvent in orderEvents)
                {
                    strategyCapacity.OnOrderEvent(orderEvent);
                }
            }

            foreach (var capacity in strategyCapacity.Capacity)
            {
                Log.Trace($"Capacity {Time.UnixTimeStampToDateTime(capacity.x)} {capacity.y}");
            }

            Assert.AreEqual(expectedCapacity, (double)strategyCapacity.Capacity.Last().y, 1.0);
        }

        [Test]
        public void CopyFilesFromRemoteSource()
        {
            var start = new DateTime(2020, 1, 1);
            var end = new DateTime(2020, 1, 31);

            var resolutions = new[] { Resolution.Minute, Resolution.Hour, Resolution.Daily };
            foreach (var symbol in JsonConvert.DeserializeObject<List<Symbol>>(File.ReadAllText(Path.Combine("Common", "Capacity", "symbols.json"))))
            {
                foreach (var resolution in resolutions)
                {
                    if (resolution < Resolution.Hour)
                    {
                        foreach (var date in Time.EachDay(start, end))
                        {
                            var filePath = LeanData.GenerateZipFilePath(Globals.DataFolder, symbol, date, resolution, TickType.Trade);
                            var filePathOutput = LeanData.GenerateZipFilePath(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "Data")), symbol, date, resolution, TickType.Trade);
                            if (File.Exists(filePath) && !File.Exists(filePathOutput))
                            {
                                Directory.GetParent(filePathOutput).Create();
                                File.Copy(filePath, filePathOutput);
                            }
                        }
                    }
                    else
                    {
                        var filePath = LeanData.GenerateZipFilePath(Globals.DataFolder, symbol, end, resolution, TickType.Trade);
                        var filePathOutput = LeanData.GenerateZipFilePath(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "Data")), symbol, end, resolution, TickType.Trade);
                        if (File.Exists(filePath) && !File.Exists(filePathOutput))
                        {
                            Directory.GetParent(filePathOutput).Create();
                            File.Copy(filePath, filePathOutput);
                        }
                    }
                }
            }
        }
    }
}
