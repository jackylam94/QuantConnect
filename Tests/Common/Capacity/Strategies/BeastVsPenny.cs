using QuantConnect.Algorithm;

namespace QuantConnect.Tests.Common.Capacity.Strategies
{
    public class BeastVsPenny : QCAlgorithm
    {
        private Symbol _spy;

        public override void Initialize()
        {
            SetStartDate(2020, 1, 1);
            SetEndDate(2020, 3, 31);
            SetCash(10000);

            _spy = AddEquity("SPY", Resolution.Hour).Symbol;
            var penny = AddEquity("ABUS", Resolution.Hour).Symbol;

            Schedule.On(DateRules.EveryDay(_spy), TimeRules.AfterMarketOpen(_spy, 1, false), () =>
            {
                SetHoldings(_spy, 0.5m);
                SetHoldings(penny, 0.5m);
            });
        }
    }
}
