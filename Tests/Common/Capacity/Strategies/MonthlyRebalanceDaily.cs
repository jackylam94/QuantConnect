using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Algorithm;
using QuantConnect.Data;

namespace QuantConnect.Tests.Common.Capacity.Strategies
{
    public class MonthlyRebalanceDaily : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2019, 12, 31);
            SetEndDate(2020, 4, 5);
            SetCash(100000);

            var spy = AddEquity("SPY", Resolution.Daily).Symbol;
            AddEquity("GE", Resolution.Daily);
            AddEquity("FB", Resolution.Daily);
            AddEquity("DIS", Resolution.Daily);
            AddEquity("CSCO", Resolution.Daily);
            AddEquity("CRM", Resolution.Daily);
            AddEquity("C", Resolution.Daily);
            AddEquity("BAC", Resolution.Daily);
            AddEquity("BABA", Resolution.Daily);
            AddEquity("AAPL", Resolution.Daily);

            Schedule.On(DateRules.MonthStart(spy), TimeRules.Noon, () =>
            {
                foreach (var symbol in Securities.Keys)
                {
                    SetHoldings(symbol, 0.10);
                }
            });
        }
    }
}
