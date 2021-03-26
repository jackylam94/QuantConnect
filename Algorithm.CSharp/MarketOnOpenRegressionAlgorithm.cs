/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm for Market On Open orders.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="trading and orders" />
    public class MarketOnOpenRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly decimal[] _expectedFillPrices = { 167.45m, 165.82m };

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 9);
            SetCash(100000);

            var resolution = Resolution.Tick;
            var equity = AddEquity("SPY", resolution);
            var symbol = equity.Symbol;
            equity.SetDataNormalizationMode(DataNormalizationMode.Raw);

            Schedule.On(
                DateRules.EveryDay(symbol),
                resolution == Resolution.Daily ? TimeRules.Midnight : TimeRules.At(15, 0),
                () => MarketOnOpenOrder(symbol, 100));
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (!orderEvent.Status.IsFill()) return;

            var expected = _expectedFillPrices[orderEvent.OrderId - 1];
            if (expected != orderEvent.FillPrice)
            {
                throw new Exception($"Unexpected value for Fill Price of {orderEvent.FillPrice}. Expected: {expected}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-25.312%"},
            {"Drawdown", "0.400%"},
            {"Expectancy", "0"},
            {"Net Profit", "-0.213%"},
            {"Sharpe Ratio", "-11.467"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.122"},
            {"Beta", "0.151"},
            {"Annual Standard Deviation", "0.021"},
            {"Annual Variance", "0"},
            {"Information Ratio", "4.469"},
            {"Tracking Error", "0.116"},
            {"Treynor Ratio", "-1.562"},
            {"Total Fees", "$2.00"},
            {"Fitness Score", "0.041"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "-134.806"},
            {"Portfolio Turnover", "0.083"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "b872c3e4a3f5afa8394b66bb1a8d68fe"}
        };
    }
}