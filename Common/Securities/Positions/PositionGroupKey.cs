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
using System.Collections.Immutable;
using System.Text;
using QuantConnect.Util;

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Defines a deterministic key for <see cref="IPositionGroup"/> that is dependent on the modeling defined by the
    /// group's <see cref="IPositionGroupDescriptor"/> and the quantity ratios between all of the group's positions.
    /// </summary>
    public struct PositionGroupKey : IEnumerable<PositionGroupKey.UnitQuantity>, IEquatable<PositionGroupKey>
    {
        /// <summary>
        /// Gets the descriptor of the group they key identifies
        /// </summary>
        public IPositionGroupDescriptor Descriptor { get; }

        /// <summary>
        /// Gets the unit quantities and symbol for each position the group requires
        /// </summary>
        public ImmutableArray<UnitQuantity> UnitQuantities { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionGroupKey"/> struct
        /// </summary>
        /// <param name="descriptor">The position group's descriptor</param>
        /// <param name="unitQuantities">The unit quantities of each position</param>
        public PositionGroupKey(IPositionGroupDescriptor descriptor, ImmutableArray<UnitQuantity> unitQuantities)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (unitQuantities.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(unitQuantities));
            }
            Descriptor = descriptor;
            UnitQuantities = unitQuantities;
        }

        /// <summary>
        /// Gets the <see cref="IPosition.UnitQuantity"/> for the specified <paramref name="symbol"/>
        /// </summary>
        public decimal GetUnitQuantity(Symbol symbol)
        {
            for (int i = 0; i < UnitQuantities.Length; i++)
            {
                var uq = UnitQuantities[i];
                if (uq.Symbol.Equals(symbol))
                {
                    return uq.Quantity;
                }
            }

            throw new KeyNotFoundException($"{symbol} was not found in the group.");
        }

        /// <summary>
        /// Creates a new position with a quantity equal to <code>units*UnitQuantity</code>
        /// </summary>
        /// <param name="symbol">The symbol for the new position</param>
        /// <param name="units">The number of units in the position. This value is the same as the position group's quantity</param>
        /// <returns>A new position with a quantity computed as the specified number of units</returns>
        public IPosition CreatePosition(Symbol symbol, decimal units)
        {
            var unitQuantity = GetUnitQuantity(symbol);
            return new Position(symbol, units * unitQuantity, unitQuantity);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionGroupKey"/> struct. This constructor is suitable to be
        /// called from within a position group's constructor, provided the group's descriptor and positions have been
        /// assigned first.
        /// </summary>
        /// <param name="positionGroup">The position group to create a key for</param>
        public static PositionGroupKey Create(IPositionGroup positionGroup)
        {
            return Create(positionGroup.Descriptor, positionGroup);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionGroupKey"/> struct. This constructor is suitable to be
        /// called from within a position group's constructor, provided the group's descriptor and positions have been
        /// assigned first.
        /// </summary>
        /// <param name="descriptor">The position group's descriptor</param>
        /// <param name="positions">The positions comprising the group</param>
        public static PositionGroupKey Create(IPositionGroupDescriptor descriptor, IReadOnlyCollection<IPosition> positions)
        {
            return new PositionGroupKey(descriptor,
                positions.ToImmutableArray(position => new UnitQuantity(position))
            );
        }

        /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.</returns>
        public bool Equals(PositionGroupKey other)
        {
            if (!Descriptor.Equals(other.Descriptor))
            {
                return false;
            }

            if (UnitQuantities.Length != other.UnitQuantities.Length)
            {
                return false;
            }

            for (int i = 0; i < UnitQuantities.Length; i++)
            {
                if (!Equals(UnitQuantities[i], other.UnitQuantities[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Indicates whether this instance and a specified object are equal.</summary>
        /// <param name="obj">The object to compare with the current instance. </param>
        /// <returns>true if <paramref name="obj" /> and this instance are the same type and represent the same value; otherwise, false. </returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is PositionGroupKey && Equals((PositionGroupKey) obj);
        }

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Descriptor.GetHashCode();
                for (int i = 0; i < UnitQuantities.Length; i++)
                {
                    hashCode = (hashCode * 397) ^ UnitQuantities[i].GetHashCode();
                }

                return hashCode;
            }
        }

        /// <summary>Returns the fully qualified type name of this instance.</summary>
        /// <returns>The fully qualified type name.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < UnitQuantities.Length; i++)
            {
                var uq = UnitQuantities[i];
                sb.Append(uq.Symbol.ToString());
                sb.Append(':');
                sb.Append(uq.Quantity.Normalize());
                if (i < UnitQuantities.Length - 1)
                {
                    sb.Append(" & ");
                }
            }

            return sb.ToString();
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<UnitQuantity> GetEnumerator()
        {
            for (int i = 0; i < UnitQuantities.Length; i++)
            {
                yield return UnitQuantities[i];
            }
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Not equals operator
        /// </summary>
        public static bool operator ==(PositionGroupKey left, PositionGroupKey right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Not equals operator
        /// </summary>
        public static bool operator !=(PositionGroupKey left, PositionGroupKey right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Specifies a position's symbol and it's unit quantity within a particular group
        /// </summary>
        public struct UnitQuantity : IEquatable<UnitQuantity>
        {
            /// <summary>
            /// Gets the position's symbol
            /// </summary>
            public readonly Symbol Symbol;

            /// <summary>
            /// Gets the position's unit quantity within the referenced group
            /// </summary>
            public readonly decimal Quantity;

            /// <summary>
            /// Initializes a new instance of the <see cref="UnitQuantity"/> struct
            /// </summary>
            /// <param name="symbol">The position's symbol</param>
            /// <param name="quantity">The position's unit quantity</param>
            public UnitQuantity(Symbol symbol, decimal quantity)
            {
                Symbol = symbol;
                Quantity = quantity;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="UnitQuantity"/> struct directly from the <see cref="IPosition"/>
            /// </summary>
            /// <param name="position">The position to create a unit quantity from</param>
            public UnitQuantity(IPosition position)
            {
                Symbol = position.Symbol;
                Quantity = position.UnitQuantity;
            }

            /// <summary>
            /// Creates a new <see cref="IPosition"/> with zero quantity
            /// </summary>
            /// <returns>A new position with a computed quantity of <code>positionGroupQuantity * Quantity</code></returns>
            public IPosition Empty()
            {
                return new Position(Symbol, 0m, Quantity);
            }

            /// <summary>
            /// Creates a new <see cref="IPosition"/> with its quantity set to match the specified <paramref name="positionGroupQuantity"/>
            /// </summary>
            /// <param name="positionGroupQuantity">The quantity of the position group this position is being added to</param>
            /// <returns>A new position with a computed quantity of <code>positionGroupQuantity * Quantity</code></returns>
            public IPosition CreatePosition(decimal positionGroupQuantity)
            {
                return new Position(Symbol, positionGroupQuantity * Quantity, Quantity);
            }

            /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
            /// <param name="other">An object to compare with this object.</param>
            /// <returns>true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.</returns>
            public bool Equals(UnitQuantity other)
            {
                return Quantity == other.Quantity
                    && Symbol.Equals(other.Symbol);
            }

            /// <summary>Indicates whether this instance and a specified object are equal.</summary>
            /// <param name="obj">The object to compare with the current instance. </param>
            /// <returns>true if <paramref name="obj" /> and this instance are the same type and represent the same value; otherwise, false. </returns>
            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                {
                    return false;
                }

                return obj is UnitQuantity && Equals((UnitQuantity) obj);
            }

            /// <summary>Returns the hash code for this instance.</summary>
            /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
            public override int GetHashCode()
            {
                unchecked
                {
                    return (Quantity.GetHashCode() * 397) ^ Symbol.GetHashCode();
                }
            }

            /// <summary>
            /// Not equals operator
            /// </summary>
            public static bool operator ==(UnitQuantity left, UnitQuantity right)
            {
                return left.Equals(right);
            }

            /// <summary>
            /// Not equals operator
            /// </summary>
            public static bool operator !=(UnitQuantity left, UnitQuantity right)
            {
                return !left.Equals(right);
            }
        }
    }
}
