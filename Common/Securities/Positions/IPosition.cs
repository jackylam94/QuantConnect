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
    /// Defines a single position. This may not represent an algorithm's total holdings
    /// for a particular symbol, but instead may be just a portion of holdings intended
    /// to be grouped, for example, into an <see cref="IPositionGroup"/>
    /// </summary>
    public interface IPosition : IComparable<IPosition>
    {
        /// <summary>
        /// Gets the position's symbol
        /// </summary>
        Symbol Symbol { get; }

        /// <summary>
        /// Gets the quantity in this position. This value MUST be a integer increment of the <see cref="UnitQuantity"/>.
        /// This is NOT the integer multiple of <see cref="UnitQuantity"/>.
        /// </summary>
        decimal Quantity { get; }

        /// <summary>
        /// Gets the size of a single lot for the security. For equities in option strategy groups, this should match
        /// the contract's multiplier (normally 100).
        /// </summary>
        decimal UnitQuantity { get; }
    }
}
