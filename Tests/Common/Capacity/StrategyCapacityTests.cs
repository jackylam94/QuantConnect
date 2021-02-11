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
        [TestCase(nameof(SpyBondPortfolioRebalance), 1918404307)]
        [TestCase(nameof(BeastVsPenny), 2000000)]
        public void TestCapacity(string strategy, int expectedCapacity)
        {
            var start = new DateTime(2020, 1, 1);
            var end = new DateTime(2020, 2, 29);

            var strategyCapacity = new StrategyCapacity(start);

            var resolutions = new[] { /*Resolution.Minute, Resolution.Hour,*/ Resolution.Daily };
            var timeZone = TimeZones.NewYork;
            var orders = JsonConvert.DeserializeObject<BacktestResult>(File.ReadAllText(Path.Combine("Common", "Capacity", "Strategies", $"{strategy}.json")), new OrderJsonConverter())
                .Orders
                .Values
                .OrderBy(o => o.Time)
                .ToList();

            var readers = new List<IEnumerator<BaseData>>();
            foreach (var symbol in JsonConvert.DeserializeObject<List<Symbol>>(File.ReadAllText(Path.Combine("Common", "Capacity", "symbols.json"))))//_symbolsByCapacity.Values.SelectMany(s => s))
            {
                foreach (var resolution in resolutions)
                {
                    var config = new SubscriptionDataConfig(typeof(TradeBar), symbol, resolution, timeZone, timeZone, true, false, false);
                    if (resolution < Resolution.Hour)
                    {
                        foreach (var date in Time.EachDay(start, end))
                        {
                            if (File.Exists(LeanData.GenerateZipFilePath(Globals.DataFolder, symbol, date, resolution, config.TickType)))
                            {
                                readers.Add(new LeanDataReader(config, symbol, resolution, date, Globals.DataFolder).Parse().GetEnumerator());
                            }
                        }
                    }
                    else
                    {
                        readers.Add(new LeanDataReader(config, symbol, resolution, end, Globals.DataFolder).Parse().GetEnumerator());
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

            Assert.AreEqual(expectedCapacity, (double)strategyCapacity.Capacity.First().y, 1.0);
        }

        [Test]
        public void CopyStuffOver()
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
