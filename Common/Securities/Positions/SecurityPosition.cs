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
    public class SecurityPosition : IPosition, IPositionGroup, IEnumerable<IPositionGroup>, IEquatable<SecurityPosition>, IDisposable
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
        public decimal Quantity => GetQuantity();

        /// <summary>
        /// Gets the type of the position group
        /// </summary>
        public IPositionGroupDescriptor Descriptor { get; }

        /// <summary>
        /// Gets the model defining how margin is computed for this group of positions
        /// </summary>
        public IPositionGroupBuyingPowerModel BuyingPowerModel => Descriptor.BuyingPowerModel;

        /// <summary>
        /// Gets the groups this security is a member of, excluding its default <see cref="SecurityPosition"/>
        /// </summary>
        public IEnumerable<IPositionGroup> Groups => _groups.Values;

        /// <summary>
        /// Gets the size of a single lot for the security. For equities in option strategy groups, this would match
        /// the contract's multiplier (normally 100) and that position wouldn't be represented w/ this type.
        /// </summary>
        public decimal UnitQuantity => Security.SymbolProperties.LotSize;

        private bool disposed;
        private decimal _quantity;
        private bool _quantityInvalidated;

        private readonly Dictionary<PositionGroupKey, IPositionGroup> _groups;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityPosition"/> class
        /// </summary>
        /// <param name="security">The security</param>
        /// <param name="descriptor">The position group descriptor for the default group</param>
        public SecurityPosition(Security security, SecurityPositionGroupDescriptor descriptor)
        {
            Security = security;
            Descriptor = descriptor;
            _quantity = security.Holdings.Quantity;
            _groups = new Dictionary<PositionGroupKey, IPositionGroup>();

            // each time this security's holdings change we'll need to recompute the quantity allocated to this group
            security.Holdings.QuantityChanged += HoldingsOnQuantityChanged;
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

            // the quantity associated w/ this position will be deducted from this SecurityPosition
            // when it is placed into a group and added via to this position via the AddGroup method

            return new Position(Symbol, quantity, UnitQuantity);
        }

        /// <summary>
        /// Removes the position group with the specified key, returning <code>true</code> if a matching group existed and was removed
        /// </summary>
        public bool RemoveGroup(PositionGroupKey key)
        {
            _quantityInvalidated = true;
            return _groups.Remove(key);
        }

        /// <summary>
        /// Adds the specified <paramref name="group"/>, throwing an <see cref="ArgumentException"/> if a group with the
        /// same <see cref="IPositionGroup.Key"/> is already present in the collection.
        /// </summary>
        public void AddGroup(IPositionGroup group)
        {
            _quantityInvalidated = true;
            _groups.Add(group.Key, group);
        }

        /// <summary>
        /// Adds the <see cref="IPositionGroup"/> to the collection and overwrites the group with a matching
        /// <see cref="IPositionGroup.Key"/>, returning <code>true</code> if an entry was overwritten and
        /// <code>false</code> if not such matching entry existed.
        /// </summary>
        public bool SetGroup(IPositionGroup group)
        {
            _quantityInvalidated = true;
            var exists = _groups.ContainsKey(group.Key);
            _groups[group.Key] = group;
            return exists;
        }

        /// <summary>
        /// Attempts to retrieve the <see cref="IPositionGroup"/> with the specified <paramref name="key"/>
        /// </summary>
        public bool TryGetGroup(PositionGroupKey key, out IPositionGroup group)
        {
            return _groups.TryGetValue(key, out group);
        }

        /// <summary>Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object. </summary>
        /// <param name="other">An object to compare with this instance. </param>
        /// <returns>A value that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance precedes <paramref name="other" /> in the sort order.  Zero This instance occurs in the same position in the sort order as <paramref name="other" />. Greater than zero This instance follows <paramref name="other" /> in the sort order. </returns>
        public int CompareTo(IPosition other)
        {
            return Position.RelationalComparer.Compare(this, other);
        }

        /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.</returns>
        public bool Equals(SecurityPosition other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Symbol.Equals(other.Symbol);
        }

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        /// <param name="obj">The object to compare with the current object. </param>
        /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((SecurityPosition) obj);
        }

        /// <summary>Serves as the default hash function. </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return Security.Symbol.GetHashCode();
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        IEnumerator<IPosition> IEnumerable<IPosition>.GetEnumerator()
        {
            yield return this;
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return Invariant($"Position: {Symbol.Value}: {Quantity}");
        }

        /// <summary>
        /// Event handler for <see cref="SecurityHolding.QuantityChanged"/>
        /// </summary>
        protected virtual void HoldingsOnQuantityChanged(object sender, SecurityHoldingQuantityChangedEventArgs e)
        {
            _quantityInvalidated = true;
        }

        /// <summary>
        /// Fetches the quantity of ungrouped security holdings. This function recomputes the quantity
        /// after <see cref="SecurityHolding.QuantityChanged"/> and after changes to the <see cref="_groups"/>
        /// </summary>
        private decimal GetQuantity()
        {
            var remaining = Security.Holdings.Quantity;
            if (_quantityInvalidated)
            {
                foreach (var kvp in _groups)
                {
                    var position = kvp.Value.GetPosition(Security.Symbol);
                    remaining -= position.Quantity;
                }

                _quantity = remaining;
                _quantityInvalidated = false;
            }

            return _quantity;
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            Security.Holdings.QuantityChanged -= HoldingsOnQuantityChanged;
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<IPositionGroup> GetEnumerator() => _groups.Values.GetEnumerator();

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Defines a finalizer to ensure <see cref="Dispose"/> is called even if we forget to.
        /// NOTE: We should still endeavor to call dispose because finalizers aren't guaranteed to run
        /// </summary>
        ~SecurityPosition()
        {
            Dispose();
        }

        /// <summary>
        /// Equals operator
        /// </summary>
        public static bool operator ==(SecurityPosition left, SecurityPosition right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Not equals operator.
        /// </summary>
        public static bool operator !=(SecurityPosition left, SecurityPosition right)
        {
            return !Equals(left, right);
        }
    }
}
