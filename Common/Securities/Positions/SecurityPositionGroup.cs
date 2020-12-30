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

using System.Collections;
using System.Collections.Generic;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Securities.Positions
{
    public interface IDefaultPositionGroup : IPositionGroup
    {
        IEnumerable<IPositionGroup> Groups { get; }
    }

    public interface IDefaultPosition : IPosition
    {
        Security Security { get; }
    }

    /// <summary>
    /// Provides an implementation of <see cref="IPositionGroup"/> that contains exactly one <see cref="IPosition"/>.
    /// This is used as a 'group of last resort' and contains the quantity of security holdings that have not been
    /// allocated to any other group. The position quantity is computed based on the algorithm-level
    /// <see cref="PositionGroupCollection"/> by determining the ungrouped quantity of the security.
    /// </summary>
    public class SecurityPositionGroup : IPositionGroup
    {
        /// <summary>
        /// Gets the count of positions in this group
        /// </summary>
        public int Count { get; } = 1;

        /// <summary>
        /// Gets the unique, deterministic key associated with this group. Groups that
        /// have equal keys can be safely merged into a larger group.
        /// </summary>
        public PositionGroupKey Key { get; }

        /// <summary>
        /// The quantity of a default group is simply the number of lots. The lot size used here is not necessarily
        /// the same as the one that would be used if this security was in a different group. For example, equities
        /// in the default group have a lot size of +1, but in an options group they have a lot size equal to the
        /// contract multiplier
        /// </summary>
        public decimal Quantity => Position.Quantity;

        /// <summary>
        /// Gets the type of the position group
        /// </summary>
        public IPositionGroupDescriptor Descriptor { get; } = SecurityPositionGroupDescriptor.Instance;

        /// <summary>
        /// Gets the symbol of the security this group represents
        /// </summary>
        public Symbol Symbol => Position.Symbol;

        /// <summary>
        /// Gets the security's default position. This position contains the remaining quantity
        /// of security holdings that have not been allocated to a group.
        /// </summary>
        public SecurityPosition Position { get; }

        /// <summary>
        /// Gets the position groups this security is a member of
        /// </summary>
        public IEnumerable<IPositionGroup> Groups => Position.Groups;

        /// <summary>
        /// Gets the model defining how margin is computed for this group of positions
        /// </summary>
        public IPositionGroupBuyingPowerModel BuyingPowerModel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityPositionGroup"/> class
        /// </summary>
        /// <param name="security">The security</param>
        /// <param name="buyingPowerModel">The position group's buying power model</param>
        public SecurityPositionGroup(Security security, IPositionGroupBuyingPowerModel buyingPowerModel)
            : this(new SecurityPosition(security, null), buyingPowerModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityPositionGroup"/> class
        /// </summary>
        /// <param name="position">The security position</param>
        /// <param name="buyingPowerModel">The position group's buying power model</param>
        public SecurityPositionGroup(SecurityPosition position, IPositionGroupBuyingPowerModel buyingPowerModel)
        {
            Position = position;
            BuyingPowerModel = buyingPowerModel;
            Key = PositionGroupKey.Create(Descriptor, new[] {Position});
        }

        /// <summary>
        /// Gets the position in this group for the specified symbol
        /// </summary>
        public IPosition GetPosition(Symbol symbol)
        {
            if (Position.Symbol == symbol)
            {
                return Position;
            }

            throw new KeyNotFoundException($"{symbol} was not found in the group. This group is only for {Position.Symbol}");
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return Invariant($"Group: {Symbol.Value}: {Position.Quantity}");
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<IPosition> GetEnumerator()
        {
            yield return Position;
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
