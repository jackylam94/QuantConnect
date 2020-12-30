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

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Defines the parameters for <see cref="IBuyingPowerModel.GetMaximumOrderQuantityForTargetBuyingPower"/>
    /// </summary>
    public class GetMaximumPositionGroupOrderQuantityForTargetBuyingPowerParameters
    {
        /// <summary>
        /// Gets the target signed percentage buying power
        /// </summary>
        public decimal TargetBuyingPower { get; }

        /// <summary>
        /// True enables the <see cref="IPositionGroupBuyingPowerModel"/> to skip setting <see cref="GetMaximumPositionGroupOrderQuantityResult.Reason"/>
        /// for non error situations, for performance
        /// </summary>
        public bool SilenceNonErrorReasons { get; }

        /// <summary>
        /// Gets the 'unit' position group, who's maximum quantity we seek
        /// </summary>
        public IPositionGroup PositionGroup { get; }

        /// <summary>
        /// Gets the algorithm's portfolio manager
        /// </summary>
        public SecurityPortfolioManager Portfolio { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetMaximumOrderQuantityForTargetBuyingPowerParameters"/> class
        /// </summary>
        /// <param name="portfolio">The algorithm's portfolio manager</param>
        /// <param name="positionGroup">The position group to be purchased with 'unit' quantity for the group</param>
        /// <param name="targetBuyingPower">The target percentage buying power</param>
        /// <param name="silenceNonErrorReasons">True will not return <see cref="GetMaximumPositionGroupOrderQuantityResult.Reason"/>
        /// set for non error situation, this is for performance</param>
        public GetMaximumPositionGroupOrderQuantityForTargetBuyingPowerParameters(
            SecurityPortfolioManager portfolio,
            IPositionGroup positionGroup,
            decimal targetBuyingPower,
            bool silenceNonErrorReasons = false
            )
        {
            Portfolio = portfolio;
            PositionGroup = positionGroup;
            TargetBuyingPower = targetBuyingPower;
            SilenceNonErrorReasons = silenceNonErrorReasons;
        }

        /// <summary>
        /// Creates a new <see cref="GetMaximumPositionGroupOrderQuantityResult"/> with zero quantity and an error message.
        /// </summary>
        public GetMaximumPositionGroupOrderQuantityResult Error(string reason)
        {
            return new GetMaximumPositionGroupOrderQuantityResult(0, reason, true);
        }

        /// <summary>
        /// Creates a new <see cref="GetMaximumPositionGroupOrderQuantityResult"/> with zero quantity and no message.
        /// </summary>
        public GetMaximumPositionGroupOrderQuantityResult Zero()
        {
            return new GetMaximumPositionGroupOrderQuantityResult(0, string.Empty, false);
        }

        /// <summary>
        /// Creates a new <see cref="GetMaximumPositionGroupOrderQuantityResult"/> with zero quantity and an info message.
        /// </summary>
        public GetMaximumPositionGroupOrderQuantityResult Zero(string reason)
        {
            return new GetMaximumPositionGroupOrderQuantityResult(0, reason, false);
        }

        /// <summary>
        /// Creates a new <see cref="GetMaximumPositionGroupOrderQuantityResult"/> for the specified quantity and no message.
        /// </summary>
        public GetMaximumPositionGroupOrderQuantityResult Result(decimal quantity)
        {
            return new GetMaximumPositionGroupOrderQuantityResult(quantity, string.Empty, false);
        }
    }
}
