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
using System.Linq;
using MathNet.Numerics;
using QuantConnect.Util;

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Provides extension methods and general methods for working with/combining <see cref="IPositionGroup"/> instances
    /// </summary>
    public static class PositionGroup
    {
        /// <summary>
        /// Determines whether or not the specified <paramref name="group"/> is empty.
        /// </summary>
        public static bool IsEmpty(this IPositionGroup group)
        {
            return group.Quantity == 0m;
        }

        /// <summary>
        /// Creates a position group that matched the specified <paramref name="group"/> but with all positions empty.
        /// The specified <paramref name="group"/> is directly returned if it is already empty.
        /// </summary>
        /// <param name="group">The position group to make empty</param>
        /// <returns>An empty position group with the same descriptor and position unit quantities as the specified group</returns>
        public static IPositionGroup Empty(this IPositionGroup group)
        {
            if (group.IsEmpty())
            {
                return group;
            }

            return Create(group.Descriptor, group.ToArray(position => position.Empty()));
        }

        /// <summary>
        /// Creates a new <see cref="IPositionGroup"/> that is empty. The key ensures we communicate data about the
        /// group's modeling and the position quantity ratios.
        /// </summary>
        public static IPositionGroup Empty(PositionGroupKey key)
        {
            return new ExplicitPositionGroup(key, key.UnitQuantities.ToArray(uq => uq.Empty()));
        }

        /// <summary>
        /// Determines whether or not all positions within the specified <paramref name="group"/>
        /// have <see cref="IPosition.Quantity"/><code>==</code><see cref="IPosition.UnitQuantity"/>
        /// </summary>
        /// <param name="group">The position group</param>
        /// <returns>True if each position's quantity equals it's unit quantity</returns>
        public static bool IsUnit(this IPositionGroup group)
        {
            foreach (var position in group)
            {
                if (position.Quantity != position.UnitQuantity)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Creates a new <see cref="IPositionGroup"/> that has the same structure and quantity ratios as the provided
        /// <paramref name="group"/>, but with all quantities negated.
        /// </summary>
        public static IPositionGroup Negate(this IPositionGroup group)
        {
            if (group.IsEmpty())
            {
                return group;
            }

            // negate each position's quantities and be sure to copy over the provided descriptor
            return Create(group.Descriptor, group.ToArray(p => p.Negate()));
        }

        /// <summary>
        /// Gets the position side (long/short/none) of the specified <paramref name="group"/>
        /// </summary>
        public static PositionSide GetPositionSide(this IPositionGroup group)
        {
            return (PositionSide) Math.Sign(group.Quantity);
        }

        /// <summary>
        /// Gets the unit position group. If the provided <paramref name="group"/> already has quantity=1 then it
        /// is directly returned, otherwise, a new group is created via <see cref="WithQuantity"/> = 1
        /// </summary>
        /// <param name="group">The position group whose unit we seek</param>
        /// <returns>The unit position group</returns>
        public static IPositionGroup WithUnitQuantities(this IPositionGroup group)
        {
            if (group.Quantity == 1m)
            {
                return group;
            }

            return group.WithQuantity(1m);
        }

        /// <summary>
        /// Creates a new <see cref="IPositionGroup"/> with the specified <paramref name="groupQuantity"/>.
        /// If the quantity provided equals the template's quantity then the template is returned.
        /// </summary>
        /// <param name="template">The group template</param>
        /// <param name="groupQuantity">The quantity of the new group</param>
        /// <returns>A position group with the same position ratios as the template but with the specified group quantity</returns>
        public static IPositionGroup WithQuantity(this IPositionGroup template, decimal groupQuantity)
        {
            if (template.Quantity == groupQuantity)
            {
                return template;
            }

            return Create(template.Descriptor,
                template.ToArray(p => p.ForGroupQuantity(groupQuantity))
            );
        }

        /// <summary>
        /// Gets a user friendly name for the provided <paramref name="group"/>
        /// </summary>
        public static string GetUserFriendlyName(this IPositionGroup group)
        {
            return group.Descriptor.GetUserFriendlyName(group);
        }

        /// <summary>
        /// Creates a new <see cref="IPositionGroup"/> explicitly defined using the specified <paramref name="positions"/>.
        /// The returned group will use the <see cref="SecurityPositionGroupBuyingPowerModel"/> and therefore the computed margin
        /// of the returned group will be the same as the sum of the margins of the individual positions.
        /// </summary>
        public static IPositionGroup Create(params IPosition[] positions)
        {
            return Create(SecurityPositionGroupDescriptor.Instance, positions);
        }

        /// <summary>
        /// Creates a new <see cref="IPositionGroup"/> explicitly defined using the specified <paramref name="positions"/>
        /// and a caller provided, potentially even user defined, <paramref name="descriptor"/> for providing the group's
        /// <see cref="IPositionGroupBuyingPowerModel"/> and its <see cref="IPositionGroupResolver"/>.
        /// </summary>
        public static IPositionGroup Create(IPositionGroupDescriptor descriptor, params IPosition[] positions)
        {
            return new ExplicitPositionGroup(
                PositionGroupKey.Create(descriptor, positions),
                positions
            );
        }

        private sealed class ExplicitPositionGroup : IPositionGroup
        {
            private readonly IPosition[] _positions;

            public decimal Quantity { get; }
            public PositionGroupKey Key { get; }
            public int Count => _positions.Length;
            public IPositionGroupDescriptor Descriptor => Key.Descriptor;
            public IPositionGroupBuyingPowerModel BuyingPowerModel => Descriptor.BuyingPowerModel;

            public ExplicitPositionGroup(PositionGroupKey key, IEnumerable<IPosition> positions)
            {
                Key = key;
                _positions = positions as IPosition[] ?? positions.ToArray();
                Quantity = _positions[0].GetImpliedGroupQuantity();
            }

            public IPosition GetPosition(Symbol symbol)
            {
                for (int i = 0; i < _positions.Length; i++)
                {
                    var position = _positions[i];
                    if (position.Symbol.Equals(symbol))
                    {
                        return position;
                    }
                }

                throw new KeyNotFoundException($"{symbol} was not found in the group.");
            }

            public IEnumerator<IPosition> GetEnumerator()
            {
                for (int i = 0; i < _positions.Length; i++)
                {
                    yield return _positions[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
