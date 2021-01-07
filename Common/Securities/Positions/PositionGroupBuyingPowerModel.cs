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
using System.Linq;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Util;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Provides a base class implementation of <see cref="IPositionGroupBuyingPowerModel"/> that uses the concepts of
    /// initial margin and maintenance margin to determine if we have sufficient capital to open a position or to maintain
    /// a position. This model is very different from <see cref="BuyingPowerModel"/> in that it performs all evaluations
    /// using the concept of <see cref="IPositionGroup"/> where the sum of the margins for each position is not the same
    /// as the margin for the entire group.
    /// </summary>
    public abstract class PositionGroupBuyingPowerModel : IPositionGroupBuyingPowerModel
    {
        /// <summary>
        /// Gets the percentage of portfolio buying power to leave as a buffer
        /// </summary>
        protected decimal RequiredFreeBuyingPowerPercent { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionGroupBuyingPowerModel"/> class
        /// </summary>
        /// <param name="requiredFreeBuyingPowerPercent">The percentage of portfolio buying power to leave as a buffer</param>
        protected PositionGroupBuyingPowerModel(decimal requiredFreeBuyingPowerPercent = 0m)
        {
            RequiredFreeBuyingPowerPercent = requiredFreeBuyingPowerPercent;
        }

        /// <summary>
        /// Gets the margin currently allocated to the specified holding
        /// </summary>
        public abstract MaintenanceMargin GetMaintenanceMargin(PositionGroupMaintenanceMarginParameters parameters);

        /// <summary>
        /// The margin that must be held in order to increase the position by the provided quantity
        /// </summary>
        public abstract InitialMargin GetInitialMarginRequirement(PositionGroupInitialMarginParameters parameters);

        /// <summary>
        /// Gets the total margin required to execute the specified order in units of the account currency including fees
        /// </summary>
        public abstract InitialMargin GetInitialMarginRequiredForOrder(
            PositionGroupInitialMarginForOrderParameters parameters
            );

        /// <summary>
        /// Computes the impact on the portfolio's buying power from adding the position group to the portfolio. This is
        /// a 'what if' analysis to determine what the state of the portfolio would be if these changes were applied. The
        /// delta (before - after) is the margin requirement for adding the positions and if the margin used after the changes
        /// are applied is less than the total portfolio value, this indicates sufficient capital.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio and a position group containing the contemplated
        /// changes to the portfolio</param>
        /// <returns>Returns the portfolio's total portfolio value and margin used before and after the position changes are applied</returns>
        public virtual ReservedBuyingPowerImpact GetReservedBuyingPowerImpact(ReservedBuyingPowerImpactParameters parameters)
        {
            // This process aims to avoid having to compute buying power on the entire portfolio and instead determines
            // the set of groups that can be impacted by the changes being contemplated. The only real way to determine
            // the change in maintenance margin is to determine what groups we'll have after the changes and compute the
            // margin based on that.
            //   1. Determine impacted groups (depends on IPositionGroupDescriptor.GetImpactedGroups)
            //   2. Compute the currently reserved buying power of impacted groups
            //   3. Create position collection using impacted groups and apply contemplated changes
            //   4. Resolve new position groups using position collection with applied contemplated changes
            //   5. Compute the contemplated reserved buying power on these newly resolved groups

            // 1. Determine impacted groups
            var impactedGroups = parameters.Portfolio.Positions.Groups.GetImpactedGroups(parameters.ContemplatedChanges).ToList();

            // 2. Compute current reserved buying power
            var current = 0m;
            foreach (var impactedGroup in impactedGroups)
            {
                current += impactedGroup.BuyingPowerModel
                    .GetReservedBuyingPowerForPositionGroup(parameters.Portfolio, impactedGroup);
            }

            // 3. Determine set of impacted positions to be grouped
            var impactedPositions = impactedGroups.SelectMany(group => group.Select(position => position))
                .Concat(parameters.ContemplatedChanges);

            // 4. Resolve new position groups
            var contemplatedGroups = parameters.Portfolio.Positions.ResolvePositionGroups(impactedPositions);

            // 5. Compute contemplated reserved buying power
            var contemplated = 0m;
            foreach (var contemplatedGroup in contemplatedGroups)
            {
                contemplated += contemplatedGroup.BuyingPowerModel
                    .GetReservedBuyingPowerForPositionGroup(parameters.Portfolio, contemplatedGroup);
            }

            return new ReservedBuyingPowerImpact(
                current, contemplated, impactedGroups, parameters.ContemplatedChanges, contemplatedGroups
            );
        }

        /// <summary>
        /// Check if there is sufficient buying power for the position group to execute this order.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the position group and the order</param>
        /// <returns>Returns buying power information for an order against a position group</returns>
        public virtual HasSufficientPositionGroupBuyingPowerForOrderResult HasSufficientBuyingPowerForOrder(
            HasSufficientPositionGroupBuyingPowerForOrderParameters parameters
            )
        {
            // The addition of position groups requires that we not only check initial margin requirements, but also
            // that we confirm that after the changes have been applied and the new groups resolved our maintenance
            // margin is still in a valid range (less than TPV). For this model, we use the security's sufficient buying
            // power impl to confirm initial margin requirements and lean heavily on GetReservedBuyingPowerImpact for
            // help with confirming that our expected maintenance margin is still less than TPV.
            //   1. Confirm we have sufficient buying power to execute the trade using security's BP model
            //   2. Confirm we haven't exceeded maintenance margin limits via GetReservedBuyingPowerImpact's delta

            // since the call came into this model (default, non-grouped model), this order is NOT a combo order
            // this means that we can delegate to the security's buying power model for the initial margin check and
            // this also means that we're guaranteed that there's exactly one position in the position group

            if (parameters.PositionGroup.Count != 1)
            {
                throw new ArgumentException($"The {nameof(SecurityPositionGroupBuyingPowerModel)} is only intended for non-grouped positions.");
            }

            // 1. Confirm we meet initial margin requirements, accounting for buffer
            var freeMargin = parameters.Portfolio.MarginRemaining * (1 - RequiredFreeBuyingPowerPercent);
            var initialMargin = this.GetInitialMarginRequirement(parameters.Portfolio, parameters.PositionGroup);
            if (freeMargin < initialMargin)
            {
                return parameters.Insufficient(Invariant(
                    $"Id: {parameters.Order.Id}, Initial Margin: {initialMargin.Normalize()}, Free Margin: {freeMargin.Normalize()}"
                ));
            }

            // 2. Confirm that the new groupings arising from the change doesn't make maintenance margin exceed TPV
            var impact = GetReservedBuyingPowerImpact(parameters);
            if (impact.Delta <= freeMargin)
            {
                return parameters.Sufficient();
            }

            return parameters.Insufficient(Invariant(
                $"Id: {parameters.Order.Id}, Maintenance Margin Delta: {impact.Delta.Normalize()}, Free Margin: {freeMargin.Normalize()}"
            ));
        }

        /// <summary>
        /// Get the maximum market position group order quantity to obtain a position with a given buying power
        /// percentage. Will not take into account free buying power.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the position group and the target
        /// signed buying power percentage</param>
        /// <returns>Returns the maximum allowed market order quantity and if zero, also the reason</returns>
        public virtual GetMaximumPositionGroupOrderQuantityResult GetMaximumPositionGroupOrderQuantityForTargetBuyingPower(
            GetMaximumPositionGroupOrderQuantityForTargetBuyingPowerParameters parameters
            )
        {
            // In order to determine maximum order quantity for a particular amount of buying power, we must resolve
            // the group's 'unit' as this will be the quantity step size. If we don't step according to these units
            // then we could be left with a different type of group with vastly different margin requirements, so we
            // must keep the ratios between all of the position quantities the same. First we'll determine the target
            // buying power, taking into account RequiredFreeBuyingPowerPercent to ensure a buffer. Then we'll evaluate
            // the initial margin requirement using the provided position group position quantities. From this value,
            // we can determine if we need to add more quantity or remove quantity by looking at the delta from the target
            // to the computed initial margin requirement. We can also compute, assuming linearity, the change in initial
            // margin requirements for each 'unit' of the position group added. The final value we need before starting to
            // iterate to solve for quantity is the minimum quantities. This is the 'unit' of the position group, and any
            // quantities less than the unit's quantity would yield an entirely different group w/ different margin calcs.
            // Now that we've resolved our target, our group unit and the unit's initial margin requirement, we can iterate
            // increasing/decreasing quantities in multiples of the unit's quantities until we're within a unit's amount of
            // initial margin to the target buying power.
            // NOTE: The first estimate MUST be greater than the target and iteration will successively decrease quantity estimates.
            //   1. Determine current holdings of position group
            //   2. If targeting zero, we can short circuit and return the negative of existing position quantities
            //   3. Determine target buying power, taking into account RequiredFreeBuyingPowerPercent
            //   4. Determine current used margin [we're using initial here to match BuyingPowerModel]
            //   5. Determine if we need to buy or sell to reach the target and convert to absolutes
            //   6. Resolve the group's 'unit' quantities, this is our step size
            //   7. Compute the initial margin requirement for a single unit
            //   7a. Compute and add order fees into the unit initial margin requirement
            //   8. Verify the target is greater than 1 unit's initial margin, otherwise exit w/ zero
            //   8a. Define minimum bounds on the target as target - (unit order margin + fees)
            //   9. Assuming linearity, compute estimate of absolute order quantity to reach target
            //  10. Begin iterating
            //  11. For each quantity estimate, compute initial margin requirement
            //  12. Compute order fees and add to the initial margin requirement
            //  13. Check to see if current estimate yields is w/in one unit's margin of target (must be less than)
            //  14. Compute a new quantity estimate
            //  15. After 13 results in ending iteration, return result w/ direction from #5

            var portfolio = parameters.Portfolio;

            // 1. Determine current holdings of position group
            var currentPositionGroup = portfolio.Positions.GetPositionGroup(parameters.PositionGroup.Key);

            // 2. If targeting zero, short circuit and return the negative of existing quantities
            if (parameters.TargetBuyingPower == 0m)
            {
                return parameters.Result(currentPositionGroup.Quantity);
            }

            // 3. Determine target buying power, taking into account RequiredFreeBuyingPowerPercent
            var bufferFactor = 1 - RequiredFreeBuyingPowerPercent;
            var totalPortfolioValue = portfolio.TotalPortfolioValue;
            var signedTargetFinalMarginValue = bufferFactor * parameters.TargetBuyingPower * totalPortfolioValue;

            // 4. Determine initial margin requirement for current holdings
            var currentSignedUsedMargin = 0m;
            if (currentPositionGroup.Quantity != 0)
            {
                currentSignedUsedMargin = this.GetInitialMarginRequirement(portfolio, currentPositionGroup);
            }

            // 5. Determine if we need to buy or sell to reach target, we'll work in the land of absolutes after this
            var target = Math.Abs(signedTargetFinalMarginValue - currentSignedUsedMargin);
            var direction = Math.Sign(signedTargetFinalMarginValue - currentSignedUsedMargin);

            // 6. Resolve 'unit' -- this defines our step size
            var groupUnit = parameters.PositionGroup.WithUnitQuantities();

            // 7. Compute initial margin requirement for a single unit
            var absUnitMargin = GetInitialMarginRequirement(new PositionGroupInitialMarginParameters(portfolio, groupUnit));
            if (absUnitMargin == 0m)
            {
                // likely due to missing price data
                var zeroPricedPosition = parameters.PositionGroup.FirstOrDefault(
                    p => portfolio.Securities.GetValueOrDefault(p.Symbol)?.Price == 0m
                );
                return parameters.Error(zeroPricedPosition?.Symbol.GetZeroPriceMessage()
                    ?? $"Computed zero initial margin requirement for {parameters.PositionGroup.GetUserFriendlyName()}."
                );
            }

            // 7a. Compute order fees associated w/ unit order and update target absUnitMargin
            var orderFees = GetOrderFeeInAccountCurrency(portfolio, groupUnit).Amount;
            absUnitMargin += orderFees;

            // 8. Verify target is more that the unit margin -- for groups, minimum is same as unit margin
            if (absUnitMargin > target)
            {
                return parameters.SilenceNonErrorReasons
                    ? parameters.Zero()
                    : parameters.Zero(
                        $"The target order margin {target} is less than the minimum initial margin: {absUnitMargin}"
                    );
            }

            // 8a. Define lower bounds on target, we seek an order that is >= targetMinimum and <= target
            var targetMinimum = target - (absUnitMargin + orderFees);

            // 9. Compute initial position group quantity estimate -- group quantities are integers [number of lots/unit quantities]
            var positionGroupQuantity = (int) Math.Ceiling(target / absUnitMargin);
            var positionGroup = groupUnit.WithQuantity(positionGroupQuantity);

            // 10. Begin iterating until order quantity is within target absFinalOrderMargin bounds (coming from above)
            var loopCount = 0;
            const int maxLoopCount = 5;
            var orderMargin = this.GetInitialMarginRequirement(portfolio, positionGroup)
                + GetOrderFeeInAccountCurrency(portfolio, positionGroup).Amount;

            var lastOrderQuantity = 0;
            while (orderMargin > target || orderMargin < targetMinimum)
            {
                // Evaluate delta target and compute new quantity estimate
                var deltaTarget = target - orderMargin;
                var marginPerUnit = orderMargin / positionGroupQuantity;
                var deltaQuantity = (int) Math.Floor(deltaTarget / marginPerUnit);
                if (deltaQuantity == 0)
                {
                    deltaQuantity = orderMargin > target ? -1 : 1;
                }

                positionGroupQuantity += deltaQuantity;

                if (positionGroupQuantity <= 0)
                {
                    return parameters.Zero(
                        $"The target order margin {target} is less than the minimum {absUnitMargin}"
                    );
                }

                ArgumentException error;
                if (UnableToConverge(lastOrderQuantity, positionGroupQuantity, groupUnit, portfolio, target, orderMargin, absUnitMargin, orderFees, out error))
                {
                    throw error;
                }

                if (loopCount >= maxLoopCount)
                {
                    break;
                }

                // 12. Update order margin with new quantity estimate
                positionGroup = positionGroup.WithQuantity(positionGroupQuantity);
                orderMargin = this.GetInitialMarginRequirement(portfolio, positionGroup)
                    + GetOrderFeeInAccountCurrency(portfolio, positionGroup).Amount;

                loopCount++;
                lastOrderQuantity = positionGroupQuantity;

                // 13. Continue iterating while order margin is greater than the target -- quantity is still too big
            }

            // 15. Incorporate direction back into the result
            return parameters.Result(direction * positionGroupQuantity);
        }

        /// <summary>
        /// Get the maximum market position group order quantity to obtain a delta in the buying power used by a position group.
        /// The deltas sign defines the position side to apply it to, positive long, negative short.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the position group and the delta buying power</param>
        /// <returns>Returns the maximum allowed market order quantity and if zero, also the reason</returns>
        /// <remarks>Used by the margin call model to reduce the position by a delta percent.</remarks>
        public virtual GetMaximumPositionGroupOrderQuantityResult GetMaximumPositionGroupOrderQuantityForDeltaBuyingPower(
            GetMaximumPositionGroupOrderQuantityForDeltaBuyingPowerParameters parameters
            )
        {
            return GetMaximumPositionGroupOrderQuantityForTargetBuyingPower(parameters);
        }

        /// <summary>
        /// Computes the margin reserved for holding this position group
        /// </summary>
        public virtual ReservedBuyingPowerForPositionGroup GetReservedBuyingPowerForPositionGroup(
            ReservedBuyingPowerForPositionGroupParameters parameters
            )
        {
            return this.GetMaintenanceMargin(parameters.Portfolio, parameters.PositionGroup);
        }

        /// <summary>
        /// Gets the buying power available for a position group trade
        /// </summary>
        /// <param name="parameters">A parameters object containing the algorithm's portfolio, security, and order direction</param>
        /// <returns>The buying power available for the trade</returns>
        public PositionGroupBuyingPower GetPositionGroupBuyingPower(PositionGroupBuyingPowerParameters parameters)
        {
            // SecurityPositionGroupBuyingPowerModel models buying power the same as non-grouped, so we can simply delegate
            // to the security's model. For posterity, however, I'll lay out the process for computing the available buying
            // power for a position group trade. There's two separate cases, one where we're increasing the position and one
            // where we're decreasing the position and potentially crossing over zero. When decreasing the position we have
            // to account for the reserved buying power that the position currently holds and add that to any free buying power
            // in the portfolio.
            //   1. Get portfolio's MarginRemaining (free buying power)
            //   2. Determine if closing position
            //   2a. Add reserved buying power freed up by closing the position
            //   2b. Rebate initial buying power required for current position [to match current behavior, might not be possible]

            // 1. Get MarginRemaining
            var buyingPower = parameters.Portfolio.MarginRemaining;

            // 2. Determine if closing position
            if (parameters.Direction.Closes(parameters.PositionGroup.GetPositionSide()))
            {
                // 2a. Add reserved buying power of current position
                buyingPower += GetReservedBuyingPowerForPositionGroup(parameters);

                // 2b. Rebate the initial margin equivalent of current position
                // this interface doesn't have a concept of initial margin as it's an impl detail of the BuyingPowerModel base class
                buyingPower += this.GetInitialMarginRequirement(parameters.Portfolio, parameters.PositionGroup);
            }

            return buyingPower;
        }

        /// <summary>
        /// Helper function to compute the order fees associated with executing market orders for the specified <paramref name="positionGroup"/>
        /// </summary>
        protected virtual CashAmount GetOrderFeeInAccountCurrency(SecurityPortfolioManager portfolio, IPositionGroup positionGroup)
        {
            // TODO : Add Order parameter to support Combo order type, pulling the orders per position

            var utcTime = portfolio.Securities.UtcTime;
            var orderFee = new CashAmount(0m, portfolio.CashBook.AccountCurrency);

            foreach (var position in positionGroup)
            {
                var security = portfolio.Securities[position.Symbol];
                var order = new MarketOrder(position.Symbol, position.Quantity, utcTime);
                orderFee += security.FeeModel.GetOrderFee(new OrderFeeParameters(security, order)).Value;
            }

            return orderFee;
        }

        /// <summary>
        /// Checks if <paramref name="lastOrderQuantity"/> equals <see cref="positionGroupQuantity"/> indicating we got the same result on this iteration
        /// meaning we're unable to converge through iteration. This function was split out to support derived types using the same error message as well
        /// as removing the added noise of the check and message creation.
        /// </summary>
        protected static bool UnableToConverge(int lastOrderQuantity, int positionGroupQuantity, IPositionGroup groupUnit, SecurityPortfolioManager portfolio, decimal target, decimal orderMargin, decimal absUnitMargin, decimal orderFees, out ArgumentException error)
        {
            // determine if we're unable to converge by seeing if quantity estimate hasn't changed
            if (lastOrderQuantity == positionGroupQuantity)
            {
                string message;
                if (groupUnit.Count == 1)
                {
                    // single security group
                    var security = portfolio.Securities[groupUnit.Single().Symbol];
                    message = "GetMaximumPositionGroupOrderQuantityForTargetBuyingPower failed to converge to target order margin " +
                        Invariant($"{target}. Current order margin is {orderMargin}. Order quantity {positionGroupQuantity}. ") +
                        Invariant($"Lot size is {security.SymbolProperties.LotSize}. Order fees {orderFees}. Security symbol ") +
                        $"{security.Symbol}. Margin unit {absUnitMargin}.";
                }
                else
                {
                    message = "GetMaximumPositionGroupOrderQuantityForTargetBuyingPower failed to converge to target order margin " +
                        Invariant($"{target}. Current order margin is {orderMargin}. Order quantity {positionGroupQuantity}. ") +
                        Invariant($"Position Group Unit is {groupUnit.Key}. Order fees {orderFees}. Position Group Name ") +
                        $"{groupUnit.GetUserFriendlyName()}. Margin unit {absUnitMargin}.";
                }

                error = new ArgumentException(message);
                return true;
            }

            error = null;
            return false;
        }
    }
}
