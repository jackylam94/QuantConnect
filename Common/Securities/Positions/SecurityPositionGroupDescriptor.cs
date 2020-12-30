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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Interfaces;

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Provides an implementation of <see cref="IPositionGroupDescriptor"/> for the <see cref="SecurityPositionGroup"/>.
    /// This is the 'default' position group and its functions for detecting position group impacts only returns
    /// references to positions/groups of the same security.
    /// </summary>
    public class SecurityPositionGroupDescriptor : IPositionGroupDescriptor, IEquatable<SecurityPositionGroupDescriptor>
    {
        /// <summary>
        /// Gets the instance of <see cref="SecurityPositionGroupDescriptor"/>
        /// </summary>
        public static IPositionGroupDescriptor Instance { get; } = null;//new SecurityPositionGroupDescriptor(0m);

        /// <summary>
        /// Gets the type of the <see cref="IPositionGroup"/> implementation
        /// </summary>
        public Type Type { get; } = typeof(SecurityPositionGroup);

        /// <summary>
        /// Gets the instance of <see cref="SecurityPositionGroupResolver"/>
        /// </summary>
        public IPositionGroupResolver Resolver { get; } = SecurityPositionGroupResolver.Instance;

        /// <summary>
        /// Gets the instance of <see cref="SecurityPositionGroupBuyingPowerModel"/>
        /// </summary>
        public IPositionGroupBuyingPowerModel BuyingPowerModel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityPositionGroupDescriptor"/> class
        /// </summary>
        public SecurityPositionGroupDescriptor(IPositionGroupBuyingPowerModel buyingPowerModel)
        {
            BuyingPowerModel = buyingPowerModel;
        }

        /// <summary>
        /// Returns the group's symbol as a user friendly name. If multiple symbols are grouped, they are separated
        /// by a pipe <code>|</code> character.
        /// </summary>
        /// <param name="group">The position group to get a user friendly name for</param>
        /// <returns>A user friendly name defining the specified position group</returns>
        public string GetUserFriendlyName(IPositionGroup group)
        {
            // since this is the 'default' group and only has one security (unless user makes their own) we'll
            // simply hide the fact that this is a position group by simply returning the symbol string
            if (group.Count == 1)
            {
                return group.Single().Symbol.ToString();
            }

            return string.Join("|", group.Select(p => p.Symbol.ToString()));
        }

        public IPosition CreatePosition(Symbol symbol, decimal quantity, decimal unitQuantity)
        {
            return new SecurityPosition();
        }

        /// <summary>
        /// Creates a new <see cref="SecurityPositionGroup"/> from the specified <paramref name="positions"/>.
        /// The provided <paramref name="positions"/> collection must only have one position and it must be of
        /// type <see cref="SecurityPosition"/>, otherwise an <see cref="ArgumentException"/> will be thrown.
        /// </summary>
        /// <param name="positions">The positions to be placed into this type of grouping</param>
        /// <returns>A new position group of type <see cref="IPositionGroupDescriptor.Type"/> containing the specified <paramref name="positions"/></returns>
        public IPositionGroup CreatePositionGroup(IReadOnlyCollection<IPosition> positions)
        {
            if (positions.Count != 1)
            {
                throw new ArgumentException("Collection must contain exactly one position.");
            }

            var securityPosition = positions.Single() as SecurityPosition;
            if (securityPosition == null)
            {
                throw new ArgumentException($"Position must be of type {nameof(SecurityPosition)}.");
            }

            return securityPosition.DefaultGroup;
        }

        /// <summary>
        /// Determines the set of groups that can be impacted by a change in the holdings of the specified <paramref name="symbol"/>.
        /// </summary>
        /// <param name="groups">The set of groups to search for potential impacts</param>
        /// <param name="symbol">The symbol with the contemplated change in holdings</param>
        /// <returns>An enumerable of groups that can be impacted by changes in the <paramref name="symbol"/>'s holdings</returns>
        public IEnumerable<IPositionGroup> GetImpactedGroups(PositionGroupCollection groups, Symbol symbol)
        {
            SecurityPositionGroup defaultGroup;
            if (!groups.TryGetSecurityGroup(symbol, out defaultGroup))
            {
                // if no default group exists then we're guaranteed no other groups exist for this symbol
                yield break;
            }

            yield return defaultGroup;

            foreach (var group in defaultGroup.Groups)
            {
                yield return group;
            }
        }

        /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.</returns>
        public bool Equals(SecurityPositionGroupDescriptor other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Type == other.Type
                && Resolver.Equals(other.Resolver)
                && BuyingPowerModel.Equals(other.BuyingPowerModel);
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

            return Equals((SecurityPositionGroupDescriptor) obj);
        }

        /// <summary>Serves as the default hash function. </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Type.GetHashCode();
                hashCode = (hashCode * 397) ^ Resolver.GetHashCode();
                hashCode = (hashCode * 397) ^ BuyingPowerModel.GetHashCode();
                return hashCode;
            }
        }
    }
}
