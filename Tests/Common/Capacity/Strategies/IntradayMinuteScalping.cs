using System;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Orders;

namespace QuantConnect.Tests.Common.Capacity.Strategies
{
    public class IntradayMinuteScalping : QCAlgorithm
    {
        private Symbol _spy;
        private ExponentialMovingAverage _fast;
        private ExponentialMovingAverage _slow;

        public override void Initialize()
        {
            SetStartDate(2020, 1, 1);
            SetStartDate(2020, 1, 30);
            SetCash(10000);
            SetWarmup(100);

            _spy = AddEquity("SPY", Resolution.Minute).Symbol;
            _fast = EMA(_spy, 10);
            _slow = EMA(_spy, 30);
        }

        public override void OnData(Slice data)
        {
            if (_fast > _slow)
            {
                SetHoldings(_spy, 1);
            }
            else {
                SetHoldings(_spy, -1);
            }
        }
    }
}
