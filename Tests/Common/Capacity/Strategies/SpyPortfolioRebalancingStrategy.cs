using System;
using QuantConnect.Algorithm;
using QuantConnect.Data;

namespace QuantConnect.Tests.Common.Capacity.Strategies
{
    public class SpyPortfolioRebalancingStrategy : QCAlgorithm
    {
        private Symbol _spy;

        public override void Initialize()
        {
            SetStartDate(2020, 1, 1);
            SetEndDate(2020, 3, 31);
            SetCash(200000000);

            _spy = AddEquity("SPY", Resolution.Hour).Symbol;

            Schedule.On(DateRules.EveryDay(_spy), TimeRules.AfterMarketOpen(_spy, 1, false), () =>
            {
                SetHoldings(_spy, 1);
            });
        }
    }
}
