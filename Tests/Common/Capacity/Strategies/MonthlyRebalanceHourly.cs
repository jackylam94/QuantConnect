using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Algorithm;
using QuantConnect.Data;

namespace QuantConnect.Tests.Common.Capacity.Strategies
{
    public class MonthlyRebalanceHourly : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2019, 12, 31);
            SetEndDate(2020, 4, 5);
            SetCash(100000);

            AddEquity("SPY", Resolution.Hour);
            AddEquity("GE", Resolution.Hour);
            AddEquity("FB", Resolution.Hour);
            AddEquity("DIS", Resolution.Hour);
            AddEquity("CSCO", Resolution.Hour);
            AddEquity("CRM", Resolution.Hour);
            AddEquity("C", Resolution.Hour);
            AddEquity("BAC", Resolution.Hour);
            AddEquity("BABA", Resolution.Hour);
            AddEquity("AAPL", Resolution.Hour);

            Schedule.On(DateRules.MonthStart(), TimeRules.Noon, () =>
            {
                foreach (var symbol in Securities.Keys)
                {
                    SetHoldings(symbol, 0.10);
                }
            });
        }
    }
}
