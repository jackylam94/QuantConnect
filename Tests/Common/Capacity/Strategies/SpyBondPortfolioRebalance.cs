using System;
using QuantConnect.Algorithm;
using QuantConnect.Data;

namespace QuantConnect.Tests.Common.Capacity.Strategies
{
    public class SpyBondPortfolioRebalance : QCAlgorithm
    {
        private Symbol _spy;

        public override void Initialize()
        {
            SetStartDate(2020, 1, 1);
            SetEndDate(2020, 3, 31);
            SetCash(10000);

            _spy = AddEquity("SPY", Resolution.Hour).Symbol;
            var bnd = AddEquity("BND", Resolution.Hour).Symbol;

            Schedule.On(DateRules.EveryDay(_spy), TimeRules.AfterMarketOpen(_spy, 1, false), () =>
            {
                SetHoldings(_spy, 0.5m);
                SetHoldings(bnd, 0.5m);
            });
        }
    }
}
