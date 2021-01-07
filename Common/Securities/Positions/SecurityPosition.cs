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
using System.Collections;
using System.Collections.Generic;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Provides an implementation of <see cref="IPosition"/> that has a computed <see cref="Quantity"/> property based
    /// on the quantity of security holdings that have not been allocated to any other group. Every security has an
    /// associated <see cref="SecurityPosition"/> and this position is also used to track all other groups for which
    /// the security belongs.
    /// </summary>
    public class SecurityPosition : IPosition, IPositionGroup
    {
        /// <summary>
        /// Gets the number one, which is the count of positions in the default group.
        /// </summary>
        public int Count { get; } = 1;

        /// <summary>
        /// Gets the security
        /// </summary>
        public Security Security { get; }

        /// <summary>
        /// Gets the position's symbol
        /// </summary>
        public Symbol Symbol => Security.Symbol;

        /// <summary>
        /// Gets the deterministic key for this group
        /// </summary>
        public PositionGroupKey Key { get; }

        /// <summary>
        /// Gets the quantity in this position
        /// </summary>
        public decimal Quantity { get; private set; }

        /// <summary>
        /// Gets the type of the position group
        /// </summary>
        public IPositionGroupDescriptor Descriptor { get; }

        /// <summary>
        /// Gets the model defining how margin is computed for this group of positions
        /// </summary>
        public IPositionGroupBuyingPowerModel BuyingPowerModel => Descriptor.BuyingPowerModel;

        /// <summary>
        /// Gets the size of a single lot for the security. For equities in option strategy groups, this would match
        /// the contract's multiplier (normally 100) and that position wouldn't be represented w/ this type.
        /// </summary>
        public decimal UnitQuantity => Security.SymbolProperties.LotSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityPosition"/> class
        /// </summary>
        /// <param name="security">The security</param>
        /// <param name="descriptor">The position group descriptor for the default group</param>
        public SecurityPosition(Security security, SecurityPositionGroupDescriptor descriptor)
            : this(security, security.Holdings.Quantity, descriptor)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityPosition"/> class
        /// </summary>
        /// <param name="security">The security</param>
        /// <param name="quantity">The quantity to allocate to this position instance</param>
        /// <param name="descriptor">The position group descriptor for the default group</param>
        public SecurityPosition(Security security, decimal quantity, IPositionGroupDescriptor descriptor)
        {
            if (security == null)
            {
                throw new ArgumentNullException(nameof(security));
            }
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            Security = security;
            Quantity = quantity;
            Descriptor = descriptor;
            Key = PositionGroupKey.Create(descriptor, this);
        }

        /// <summary>
        /// Gets whether this position  is empty
        /// </summary>
        /// <remarks>
        /// This is provided to remove ambiguity in extension methods as this type implements both:
        /// <see cref="PositionExtensions.IsEmpty"/> and <see cref="PositionGroup.IsEmpty"/>
        /// </remarks>
        public bool IsEmpty() => Quantity != 0;

        /// <summary>
        /// Returns <code>this</code> if the provided <paramref name="symbol"/> matches,
        /// otherwise a <see cref="KeyNotFoundException"/> is thrown.
        /// </summary>
        public IPosition GetPosition(Symbol symbol)
        {
            if (Symbol == symbol)
            {
                return this;
            }

            throw new KeyNotFoundException($"{symbol} was not found in the group. This group is only for {Symbol}");
        }

        /// <summary>
        /// Creates a new <see cref="IPosition"/> with the specified <paramref name="quantity"/>.
        /// If there is insufficient quantity then an <see cref="InvalidOperationException"/> will be thrown.
        /// </summary>
        public IPosition CreatePosition(decimal quantity)
        {
            if (Quantity < quantity)
            {
                throw new InvalidOperationException("Insufficient quantity remaining. " +
                    $"Symbol: {Symbol} RemainingQuantity: {Quantity} RequestedQuantity: {quantity}"
                );
            }

            // deduct from the default position
            Quantity -= quantity;

            return new Position(Symbol, quantity, UnitQuantity);
        }

        /// <summary>Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object. </summary>
        /// <param name="other">An object to compare with this instance. </param>
        /// <returns>A value that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance precedes <paramref name="other" /> in the sort order.  Zero This instance occurs in the same position in the sort order as <paramref name="other" />. Greater than zero This instance follows <paramref name="other" /> in the sort order. </returns>
        public int CompareTo(IPosition other)
        {
            return Position.RelationalComparer.Compare(this, other);
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return Invariant($"Position: {Symbol.Value}: {Quantity}");
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<IPosition> GetEnumerator()
        {
            yield return this;
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
