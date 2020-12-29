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
    /// Specifies how margin is calculated for a particular <see cref="IPositionGroup"/>
    /// </summary>
    public interface IPositionGroupBuyingPowerModel
    {
        /// <summary>
        /// Computes the impact on the portfolio's buying power from adding the position group to the portfolio. This is
        /// a 'what if' analysis to determine what the state of the portfolio would be if these changes were applied. The
        /// delta (before - after) is the margin requirement for adding the positions and if the margin used after the changes
        /// are applied is less than the total portfolio value, this indicates sufficient capital.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio and a position group containing the contemplated
        /// changes to the portfolio</param>
        /// <returns>Returns the portfolio's total portfolio value and margin used before and after the position changes are applied</returns>
        ReservedBuyingPowerImpact GetReservedBuyingPowerImpact(
            ReservedBuyingPowerImpactParameters parameters
            );

        /// <summary>
        /// Check if there is sufficient buying power for the position group to execute this order.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the position group and the order</param>
        /// <returns>Returns buying power information for an order against a position group</returns>
        HasSufficientPositionGroupBuyingPowerForOrderResult HasSufficientBuyingPowerForOrder(
            HasSufficientPositionGroupBuyingPowerForOrderParameters parameters
            );

        /// <summary>
        /// Get the maximum market position group order quantity to obtain a position with a given buying power
        /// percentage. Will not take into account free buying power.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the position group and the target
        /// signed buying power percentage</param>
        /// <returns>Returns the maximum allowed market order quantity and if zero, also the reason</returns>
        GetMaximumPositionGroupOrderQuantityResult GetMaximumPositionGroupOrderQuantityForTargetBuyingPower(
            GetMaximumOrderQuantityForPositionGroupTargetBuyingPowerParameters parameters
            );

        /// <summary>
        /// Get the maximum market position group order quantity to obtain a delta in the buying power used by a position group.
        /// The deltas sign defines the position side to apply it to, positive long, negative short.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the position group and the delta buying power</param>
        /// <returns>Returns the maximum allowed market order quantity and if zero, also the reason</returns>
        /// <remarks>Used by the margin call model to reduce the position by a delta percent.</remarks>
        GetMaximumPositionGroupOrderQuantityResult GetMaximumPositionGroupOrderQuantityForDeltaBuyingPower(
            GetMaximumPositionGroupOrderQuantityForDeltaBuyingPowerParameters parameters
            );

        /// <summary>
        /// Computes the margin reserved for holding this position group
        /// </summary>
        ReservedBuyingPowerForPositionGroup GetReservedBuyingPowerForPositionGroup(
            ReservedBuyingPowerForPositionGroupParameters parameters
            );

        /// <summary>
        /// Gets the buying power available for a position group trade
        /// </summary>
        /// <param name="parameters">A parameters object containing the algorithm's portfolio, security, and order direction</param>
        /// <returns>The buying power available for the trade</returns>
        PositionGroupBuyingPower GetPositionGroupBuyingPower(PositionGroupBuyingPowerParameters parameters);
    }
}
