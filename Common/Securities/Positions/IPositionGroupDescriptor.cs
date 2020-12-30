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

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Defines a particular type, or classification, of position groups and functions as a descriptor.
    /// This includes access to models and factory functions to support constructing groups of the type
    /// described by this descriptor.
    /// </summary>
    /// <remarks>
    /// At time of writing there are currently three different types of position groups being considered.
    /// 1. Default: <see cref="SecurityPositionGroup"/>
    /// 2. Options strategy
    /// 3. Futures strategy
    /// </remarks>
    public interface IPositionGroupDescriptor
    {
        /// <summary>
        /// Gets the type of the <see cref="IPositionGroup"/> implementation
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Gets <see cref="IPositionGroupResolver"/> used for resolving groups of this type
        /// </summary>
        IPositionGroupResolver Resolver { get; }

        /// <summary>
        /// Gets the <see cref="IPositionGroupBuyingPowerModel"/> used for groups of this type
        /// </summary>
        IPositionGroupBuyingPowerModel BuyingPowerModel { get; }

        /// <summary>
        /// Gets a user friendly name for this position group.
        /// </summary>
        /// <param name="group">The position group to get a user friendly name for</param>
        /// <returns>A user friendly name defining the specified position group</returns>
        string GetUserFriendlyName(IPositionGroup group);

        /// <summary>
        /// Creates a new <see cref="IPosition"/> intended to be a member of a position group of the type
        /// described by this descriptor
        /// </summary>
        /// <param name="symbol">The position's symbol</param>
        /// <param name="quantity">The position's quantity</param>
        /// <param name="unitQuantity">The position's unit quantity within the group</param>
        /// <returns>A new position with the specified properties</returns>
        IPosition CreatePosition(Symbol symbol, decimal quantity, decimal unitQuantity);

        /// <summary>
        /// Creates a new <see cref="IPositionGroup"/> from the specified <paramref name="positions"/>
        /// </summary>
        /// <param name="positions">The positions to be placed into this type of grouping</param>
        /// <returns>A new position group of type <see cref="Type"/> containing the specified <paramref name="positions"/></returns>
        IPositionGroup CreatePositionGroup(IReadOnlyCollection<IPosition> positions);

        /// <summary>
        /// Determines the set of groups that can be impacted by a change in the specified <paramref name="symbol"/>'s holdings.
        /// </summary>
        /// <param name="groups">The set of groups to search for potential impacts</param>
        /// <param name="symbol">The symbol with the contemplated change in holdings</param>
        /// <returns>An enumerable of groups that can be impacted by changes in the <paramref name="symbol"/>'s holdings</returns>
        IEnumerable<IPositionGroup> GetImpactedGroups(PositionGroupCollection groups, Symbol symbol);
    }
}
