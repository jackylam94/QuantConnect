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
    /// Provides extension methods for <see cref="IPosition"/>
    /// </summary>
    public static class PositionExtensions
    {
        /// <summary>
        /// Determines whether or not the specified <paramref name="position"/> is empty (zero quantity).
        /// </summary>
        public static bool IsEmpty(this IPosition position)
        {
            return position.Quantity == 0m;
        }

        /// <summary>
        /// Creates an <see cref="IPosition"/> using the specified <paramref name="template"/> with zero quantity.
        /// The <paramref name="template"/> is directly returned if its quantity is already set to zero.
        /// </summary>
        /// <param name="template">The position used as a template to produce a zero quantity position</param>
        /// <returns>A position with the same properties as the template and its quantity set to zero</returns>
        public static IPosition Empty(this IPosition template)
        {
            if (template.Quantity == 0m)
            {
                return template;
            }

            return new Position(template.Symbol, 0, template.UnitQuantity);
        }

        /// <summary>
        /// Creates a new <see cref="IPosition"/> instance with opposite quantity, such that adding <paramref name="position"/>
        /// and the result of this function together would yield an empty position.
        /// </summary>
        public static IPosition Negate(this IPosition position)
        {
            if (position.Quantity == 0m)
            {
                return position;
            }

            return new Position(position.Symbol, -position.Quantity, position.UnitQuantity);
        }

        /// <summary>
        /// Creates a new <see cref="IPosition"/> with it's <see cref="IPosition.UnitQuantity"/>.
        /// </summary>
        /// <param name="position">The position to turn into a unit position</param>
        /// <returns>The unit position</returns>
        public static IPosition WithUnitQuantity(this IPosition position)
        {
            return position.ForGroupQuantity(1m);
        }

        /// <summary>
        /// Creates a new <see cref="IPosition"/> with the quantity required to create a group with the specified <paramref name="groupQuantity"/>.
        /// The <see cref="IPosition.Quantity"/> will equal the <paramref name="groupQuantity"/> times <see cref="IPosition.UnitQuantity"/>
        /// </summary>
        /// <param name="position">The position to resize</param>
        /// <param name="groupQuantity">The quantity of the group this position belongs to</param>
        /// <returns>A new position with the appropriate size</returns>
        public static IPosition ForGroupQuantity(this IPosition position, decimal groupQuantity)
        {
            var quantity = position.UnitQuantity * groupQuantity;
            if (position.Quantity == quantity)
            {
                return position;
            }

            return new Position(position.Symbol, quantity, position.UnitQuantity);
        }

        /// <summary>
        /// Gets the group's quantity implied by the <paramref name="position"/>'s quantity and unit quantity.
        /// </summary>
        /// <param name="position">The position</param>
        /// <returns>The implied group quantity</returns>
        public static decimal GetImpliedGroupQuantity(this IPosition position)
        {
            return position.Quantity / position.UnitQuantity;
        }
    }
}
