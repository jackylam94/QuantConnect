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
    /// Defines the parameters for <see cref="IBuyingPowerModel.GetReservedBuyingPowerForPosition"/>
    /// </summary>
    public class ReservedBuyingPowerForPositionGroupParameters
    {
        /// <summary>
        /// Gets the <see cref="IPositionGroup"/>
        /// </summary>
        public IPositionGroup PositionGroup { get; }

        /// <summary>
        /// Gets the algorithm's <see cref="SecurityManager"/>
        /// </summary>
        public SecurityManager Securities { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReservedBuyingPowerForPositionGroupParameters"/> class
        /// </summary>
        /// <param name="securities">The algorithm's security manager</param>
        /// <param name="positionGroup">The position group</param>
        public ReservedBuyingPowerForPositionGroupParameters(SecurityManager securities, IPositionGroup positionGroup)
        {
            Securities = securities;
            PositionGroup = positionGroup;
        }

        /// <summary>
        /// Creates the result using the specified reserved buying power in units of the account currency
        /// </summary>
        /// <param name="reservedBuyingPower">The reserved buying power in units of the account currency</param>
        /// <returns>The reserved buying power</returns>
        public ReservedBuyingPowerForPositionGroup ResultInAccountCurrency(decimal reservedBuyingPower)
        {
            return new ReservedBuyingPowerForPositionGroup(reservedBuyingPower);
        }
    }
}
