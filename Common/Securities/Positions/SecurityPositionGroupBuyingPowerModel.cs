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
    /// Provides an implementation of <see cref="IPositionGroupBuyingPowerModel"/> that computes margin as the sum
    /// of individual security margins. This is the default model to be used with the <see cref="SecurityPositionGroup"/>
    /// and is equivalent to the computing margin while ignoring any grouping of securities.
    /// </summary>
    public class SecurityPositionGroupBuyingPowerModel : PositionGroupBuyingPowerModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityPositionGroupBuyingPowerModel"/> class
        /// </summary>
        /// <param name="requiredFreeBuyingPowerPercent"></param>
        public SecurityPositionGroupBuyingPowerModel(decimal requiredFreeBuyingPowerPercent)
            : base(requiredFreeBuyingPowerPercent)
        {
        }

        protected override decimal GetInitialMarginRequirement(SecurityManager securities, IPositionGroup @group)
        {
            throw new NotImplementedException();
        }

        protected override decimal GetMaintenanceMarginRequirement(SecurityManager securities, IPositionGroup group)
        {
            // SecurityPositionGroupBuyingPowerModel models buying power the same as non-grouped, so we can simply sum up
            // the reserved buying power via the security's model. We should really only ever get a single position here,
            // but it's not incorrect to ask the model for what the reserved buying power would be using default modeling
            var buyingPower = 0m;
            foreach (var position in group)
            {
                var security = securities[position.Symbol];
                var result = security.BuyingPowerModel.GetReservedBuyingPowerForPosition(
                    new ReservedBuyingPowerForPositionParameters(security)
                );

                buyingPower += result.AbsoluteUsedBuyingPower;
            }

            return new ReservedBuyingPowerForPositionGroup(buyingPower);
        }

        private SecurityPositionGroup GetSecurityPositionGroup(IPositionGroup positionGroup)
        {
            var group = positionGroup as SecurityPositionGroup;
            if (group == null)
            {
                throw new ArgumentException(
                    $"{GetType().Name}.{nameof(GetMaximumPositionGroupOrderQuantityForTargetBuyingPower)} " +
                    $"only supports position groups of type {nameof(SecurityPositionGroup)}"
                );
            }

            return group;
        }
    }
}
