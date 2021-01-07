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

using System.Collections.Generic;
using QuantConnect.Orders;

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Provides extension methods for <see cref="IPositionGroupBuyingPowerModel"/> to remove noise
    /// from initializing parameter classes.
    /// </summary>
    public static class PositionGroupBuyingPowerModelExtensions
    {
        /// <summary>
        /// Gets the margin currently allocated to the specified position group
        /// </summary>
        public static decimal GetMaintenanceMargin(
            this IPositionGroupBuyingPowerModel model,
            SecurityPortfolioManager portfolio,
            IPositionGroup positionGroup,
            bool isCurrentHoldings
            )
        {
            return model.GetMaintenanceMargin(
                new PositionGroupMaintenanceMarginParameters(portfolio, positionGroup, isCurrentHoldings)
            );
        }

        /// <summary>
        /// The margin that must be held in order to change positions by the changes defined by the provided position group
        /// </summary>
        public static decimal GetInitialMarginRequirement(
            this IPositionGroupBuyingPowerModel model,
            SecurityPortfolioManager portfolio,
            IPositionGroup positionGroup
            )
        {
            return model.GetInitialMarginRequirement(
                new PositionGroupInitialMarginParameters(portfolio, positionGroup)
            ).Value;
        }

        /// <summary>
        /// Gets the total margin required to execute the specified order in units of the account currency including fees
        /// </summary>
        public static decimal GetInitialMarginRequiredForOrder(
            this IPositionGroupBuyingPowerModel model,
            SecurityPortfolioManager portfolio,
            IPositionGroup positionGroup,
            Order order
            )
        {
            return model.GetInitialMarginRequiredForOrder(
                new PositionGroupInitialMarginForOrderParameters(portfolio, positionGroup, order)
            ).Value;
        }

        /// <summary>
        /// Check if there is sufficient buying power for the position group to execute this order.
        /// </summary>\
        public static HasSufficientPositionGroupBuyingPowerForOrderResult HasSufficientBuyingPowerForOrder(
            this IPositionGroupBuyingPowerModel model,
            SecurityPortfolioManager portfolio,
            IPositionGroup positionGroup,
            Order order
            )
        {
            return model.HasSufficientBuyingPowerForOrder(new HasSufficientPositionGroupBuyingPowerForOrderParameters(
                portfolio, positionGroup, order
            ));
        }

        /// <summary>
        /// Computes the impact on the portfolio's buying power from adding the position group to the portfolio. This is
        /// a 'what if' analysis to determine what the state of the portfolio would be if these changes were applied. The
        /// delta (before - after) is the margin requirement for adding the positions and if the margin used after the changes
        /// are applied is less than the total portfolio value, this indicates sufficient capital.
        /// </summary>
        /// <returns>Returns the portfolio's total portfolio value and margin used before and after the position changes are applied</returns>
        public static ReservedBuyingPowerImpact GetReservedBuyingPowerImpact(
            this IPositionGroupBuyingPowerModel model,
            SecurityPortfolioManager portfolio,
            IReadOnlyCollection<IPosition> contemplatedChanges
            )
        {
            return model.GetReservedBuyingPowerImpact(
                new ReservedBuyingPowerImpactParameters(portfolio, contemplatedChanges)
            );
        }

        /// <summary>
        /// Computes the margin reserved for holding this position group
        /// </summary>
        public static ReservedBuyingPowerForPositionGroup GetReservedBuyingPowerForPositionGroup(
            this IPositionGroupBuyingPowerModel model,
            SecurityPortfolioManager portfolio,
            IPositionGroup positionGroup
            )
        {
            return model.GetReservedBuyingPowerForPositionGroup(
                new ReservedBuyingPowerForPositionGroupParameters(portfolio, positionGroup)
            );
        }

        /// <summary>
        /// Get the maximum market position group order quantity to obtain a position with a given buying power
        /// percentage. Will not take into account free buying power.
        /// </summary>
        public static GetMaximumPositionGroupOrderQuantityResult GetMaximumPositionGroupOrderQuantityForTargetBuyingPower(
            this IPositionGroupBuyingPowerModel model,
            SecurityPortfolioManager portfolio,
            IPositionGroup positionGroup,
            decimal targetBuyingPower,
            bool silenceNonErrorReasons = false
            )
        {
            return model.GetMaximumPositionGroupOrderQuantityForTargetBuyingPower(
                new GetMaximumPositionGroupOrderQuantityForTargetBuyingPowerParameters(
                    portfolio, positionGroup, targetBuyingPower, silenceNonErrorReasons
                )
            );
        }

        /// <summary>
        /// Get the maximum market position group order quantity to obtain a position with a given buying power
        /// percentage. Will not take into account free buying power.
        /// </summary>
        public static GetMaximumPositionGroupOrderQuantityResult GetMaximumPositionGroupOrderQuantityForDeltaBuyingPower(
            this IPositionGroupBuyingPowerModel model,
            SecurityPortfolioManager portfolio,
            IPositionGroup positionGroup,
            decimal targetBuyingPower,
            bool silenceNonErrorReasons = false
            )
        {
            return model.GetMaximumPositionGroupOrderQuantityForDeltaBuyingPower(
                new GetMaximumPositionGroupOrderQuantityForDeltaBuyingPowerParameters(
                  portfolio, positionGroup, targetBuyingPower, silenceNonErrorReasons
                )
            );
        }

        /// <summary>
        /// Gets the buying power available for a position group trade
        /// </summary>
        public static decimal GetPositionGroupBuyingPower(
            this IPositionGroupBuyingPowerModel model,
            SecurityPortfolioManager portfolio,
            IPositionGroup positionGroup,
            OrderDirection direction
            )
        {
            return model.GetPositionGroupBuyingPower(new PositionGroupBuyingPowerParameters(
                portfolio, positionGroup, direction
            )).Value;
        }
    }
}
