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

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Defines a single position group which is a collection.
    /// </summary>
    public interface IPositionGroup : IReadOnlyCollection<IPosition>
    {
        /// <summary>
        /// Gets the deterministic key for this group
        /// </summary>
        PositionGroupKey Key { get; }

        /// <summary>
        /// Gets the signed quantity of this group
        /// </summary>
        /// <remarks>
        /// This value represents the number of 'units' contained within this position group. Dividing all position
        /// quantities by this value would yield the unit quantities for this group. The sign of this value indicates
        /// whether this group is long or short and is also used when determining maximum order quantities in the
        /// <see cref="IPositionGroupBuyingPowerModel"/> functions.
        /// </remarks>
        decimal Quantity { get; }

        /// <summary>
        /// Gets the type of the position group
        /// </summary>
        IPositionGroupDescriptor Descriptor { get; }

        /// <summary>
        /// Gets the model defining how margin is computed for this group of positions
        /// </summary>
        IPositionGroupBuyingPowerModel BuyingPowerModel { get; }

        /// <summary>
        /// Gets the position in this group for the specified <paramref name="symbol"/>. If the symbol is not a member
        /// of this group then a <see cref="KeyNotFoundException"/> should be thrown.
        /// </summary>
        IPosition GetPosition(Symbol symbol);
    }
}
