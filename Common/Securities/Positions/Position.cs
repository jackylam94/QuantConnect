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

using System.Collections.Concurrent;
using System.Collections.Generic;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Provides the simplest implementation of <see cref="IPosition"/> as well as
    /// </summary>
    public class Position : IPosition
    {
        /// <summary>
        /// Gets the default <see cref="IComparer{T}"/> to be used for deterministic ordering of positions.
        /// This comparer first sorts by the position's unit quantity and then by the symbol
        /// </summary>
        public static IComparer<IPosition> RelationalComparer { get; } = new UnitQuantitySymbolComparer();

        /// <summary>
        /// Creates a new <see cref="IPosition"/> for the specified <paramref name="symbol"/> with zero quantity.
        /// </summary>
        public static IPosition Empty(Symbol symbol)
        {
            return EmptyPosition.Get(symbol);
        }

        /// <summary>
        /// Gets the position's symbol
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Gets the quantity in this position
        /// </summary>
        public decimal Quantity { get; }

        /// <summary>
        /// Gets the quantity of this position when it's group quantity equals 1. For ungrouped securities,
        /// this value equals its lot size, which is normally 1. For equities in options groups, this value
        /// will equal the contract's multiplier, which is normally 100.
        /// </summary>
        public decimal UnitQuantity { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Position"/> class
        /// </summary>
        /// <param name="symbol">The position's symbol</param>
        /// <param name="quantity">The position's quantity</param>
        /// <param name="unitQuantity">The unit quantity for this position</param>
        public Position(Symbol symbol, decimal quantity, decimal unitQuantity)
        {
            Symbol = symbol;
            Quantity = quantity;
            UnitQuantity = unitQuantity;
        }


        /// <summary>Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object. </summary>
        /// <param name="other">An object to compare with this instance. </param>
        /// <returns>A value that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance precedes <paramref name="other" /> in the sort order.  Zero This instance occurs in the same position in the sort order as <paramref name="other" />. Greater than zero This instance follows <paramref name="other" /> in the sort order. </returns>
        public int CompareTo(IPosition other)
        {
            return RelationalComparer.Compare(this, other);
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return Invariant($"Position: {Symbol.Value}: {Quantity}");
        }

        /// <summary>
        /// Provides an implementation of <see cref="IPosition"/> that is by definition empty, with zero quantity,
        /// and lot/contract sizes equal to unity. This type is created via the <see cref="Empty"/> method and
        /// should be used as a 'null' instance for positions, for example as the result of <see cref="IPositionGroup.GetPosition"/>
        /// when the requested symbol doesn't exist in the position group.
        /// </summary>
        private sealed class EmptyPosition : IPosition
        {
            // remove GC pressure and heap allocations by storing these references
            private static readonly ConcurrentDictionary<Symbol, EmptyPosition> Values
                = new ConcurrentDictionary<Symbol, EmptyPosition>();

            public static EmptyPosition Get(Symbol symbol)
            {
                return Values.GetOrAdd(symbol, s => new EmptyPosition(s));
            }

            public Symbol Symbol { get; }
            public decimal Quantity => 0;
            public decimal UnitQuantity => 1;

            private EmptyPosition(Symbol symbol)
            {
                Symbol = symbol;
            }

            public int CompareTo(IPosition other)
            {
                return RelationalComparer.Compare(this, other);
            }
        }

        /// <summary>
        /// Provides an implementation of <see cref="IComparer{T}"/>, accessed via the <see cref="RelationalComparer"/>
        /// </summary>
        private sealed class UnitQuantitySymbolComparer : IComparer<IPosition>
        {
            public int Compare(IPosition x, IPosition y)
            {
                if (ReferenceEquals(x, y))
                {
                    return 0;
                }

                if (ReferenceEquals(null, y))
                {
                    return 1;
                }

                if (ReferenceEquals(null, x))
                {
                    return -1;
                }

                var unitQuantityComparison = x.UnitQuantity.CompareTo(y.UnitQuantity);
                if (unitQuantityComparison != 0)
                {
                    return unitQuantityComparison;
                }

                return x.Symbol.CompareTo(y.Symbol);
            }
        }
    }
}
