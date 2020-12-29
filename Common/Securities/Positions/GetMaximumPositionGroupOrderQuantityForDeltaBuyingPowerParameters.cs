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

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Defines the parameters for <see cref="IBuyingPowerModel.GetMaximumOrderQuantityForDeltaBuyingPower"/>
    /// </summary>
    public class GetMaximumPositionGroupOrderQuantityForDeltaBuyingPowerParameters
    {
        /// <summary>
        /// Gets the algorithm's security manager
        /// </summary>
        public SecurityManager Securities { get; }

        /// <summary>
        /// Gets the algorithm's portfolio manager
        /// </summary>
        public SecurityPortfolioManager Portfolio { get; }

        /// <summary>
        /// Gets the algorithm's position group manager
        /// </summary>
        public PositionGroupManager PositionGroupManager { get; }

        /// <summary>
        /// Gets the position group
        /// </summary>
        public IPositionGroup PositionGroup { get; }

        /// <summary>
        /// The delta buying power.
        /// </summary>
        /// <remarks>Sign defines the position side to apply the delta, positive long, negative short side.</remarks>
        public decimal DeltaBuyingPower { get; }

        /// <summary>
        /// True enables the <see cref="IBuyingPowerModel"/> to skip setting <see cref="GetMaximumOrderQuantityResult.Reason"/>
        /// for non error situations, for performance
        /// </summary>
        public bool SilenceNonErrorReasons { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetMaximumPositionGroupOrderQuantityForDeltaBuyingPowerParameters"/> class
        /// </summary>
        /// <param name="securities">The algorithm's security manager</param>
        /// <param name="portfolio">The algorithm's portfolio manager</param>
        /// <param name="positionGroupManager">The algorithm's position group manager</param>
        /// <param name="positionGroup">The position group</param>
        /// <param name="deltaBuyingPower">The delta buying power to apply. Sign defines the position side to apply the delta</param>
        /// <param name="silenceNonErrorReasons">True will not return <see cref="GetMaximumPositionGroupOrderQuantityResult.Reason"/>
        /// set for non error situation, this is for performance</param>
        public GetMaximumPositionGroupOrderQuantityForDeltaBuyingPowerParameters(
            SecurityManager securities,
            SecurityPortfolioManager portfolio,
            PositionGroupManager positionGroupManager,
            IPositionGroup positionGroup,
            decimal deltaBuyingPower,
            bool silenceNonErrorReasons = false
            )
        {
            Securities = securities;
            Portfolio = portfolio;
            PositionGroup = positionGroup;
            DeltaBuyingPower = deltaBuyingPower;
            PositionGroupManager = positionGroupManager;
            SilenceNonErrorReasons = silenceNonErrorReasons;
        }

        /// <summary>
        /// Automatic conversion to <see cref="ReservedBuyingPowerForPositionGroupParameters"/>
        /// </summary>
        public static implicit operator ReservedBuyingPowerForPositionGroupParameters(
            GetMaximumPositionGroupOrderQuantityForDeltaBuyingPowerParameters parameters
            )
        {
            return new ReservedBuyingPowerForPositionGroupParameters(parameters.Securities, parameters.PositionGroup);
        }

        public static implicit operator GetMaximumPositionGroupOrderQuantityForTargetBuyingPowerParameters(
            GetMaximumPositionGroupOrderQuantityForDeltaBuyingPowerParameters parameters
            )
        {
            // we convert this delta request into a target buying power request through projection
            // by determining the currently used (reserved) buying power and adding the delta to
            // arrive at a target buying power percentage

            var manager = parameters.PositionGroupManager;
            var model = parameters.PositionGroup.Descriptor.BuyingPowerModel;
            var usedBuyingPower = model.GetReservedBuyingPowerForPositionGroup(parameters);
            var currentPositionGroup = manager.GetPositionGroup(parameters.PositionGroup.Key);
            var signedUsedBuyingPower = Math.Sign(currentPositionGroup.Quantity) * usedBuyingPower;
            var targetBuyingPower = signedUsedBuyingPower + parameters.DeltaBuyingPower;

            return new GetMaximumPositionGroupOrderQuantityForTargetBuyingPowerParameters(
                parameters.Securities,
                parameters.Portfolio,
                parameters.PositionGroupManager,
                parameters.PositionGroup, parameters.Portfolio.TotalPortfolioValue != 0
                    ? targetBuyingPower / parameters.Portfolio.TotalPortfolioValue
                    : 0);
        }
    }
}
