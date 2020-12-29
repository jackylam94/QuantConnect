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
    public class GetMaximumOrderQuantityForPositionGroupTargetBuyingPowerParameters
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
        /// Gets the algorithm's security manager
        /// </summary>
        public SecurityManager Securities { get; }

        /// <summary>
        /// Gets the algorithm's position group manager
        /// </summary>
        public PositionGroupManager PositionGroupManager { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetMaximumOrderQuantityForTargetBuyingPowerParameters"/> class
        /// </summary>
        /// <param name="securities">The algorithm's security manager</param>
        /// <param name="portfolio">The algorithm's portfolio manager</param>
        /// <param name="positionGroupManager">The algorithm's position group manager</param>
        /// <param name="positionGroup">The position group to be purchased with 'unit' quantity for the group</param>
        /// <param name="targetBuyingPower">The target percentage buying power</param>
        /// <param name="silenceNonErrorReasons">True will not return <see cref="GetMaximumPositionGroupOrderQuantityResult.Reason"/>
        /// set for non error situation, this is for performance</param>
        public GetMaximumOrderQuantityForPositionGroupTargetBuyingPowerParameters(
            SecurityManager securities,
            SecurityPortfolioManager portfolio,
            PositionGroupManager positionGroupManager,
            IPositionGroup positionGroup,
            decimal targetBuyingPower,
            bool silenceNonErrorReasons = false
            )
        {
            Securities = securities;
            Portfolio = portfolio;
            PositionGroup = positionGroup;
            TargetBuyingPower = targetBuyingPower;
            PositionGroupManager = positionGroupManager;
            SilenceNonErrorReasons = silenceNonErrorReasons;
        }
    }
}
