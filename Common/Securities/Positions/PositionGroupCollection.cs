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
using System.Linq;
using QuantConnect.Util;

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Provides an immutable collection type for <see cref="IPositionGroup"/> and <see cref="SecurityPosition"/>.
    /// The <see cref="SecurityPosition"/> is the 'default' group and must exist for every symbol referenced in
    /// each position group added.
    /// </summary>
    public class PositionGroupCollection : IReadOnlyCollection<IPositionGroup>
    {
        /// <summary>
        /// Gets an empty <see cref="PositionGroupCollection"/>
        /// </summary>
        public static PositionGroupCollection Empty { get; } = new PositionGroupCollection();

        /// <summary>
        /// Gets the number of position groups in this collection
        /// </summary>
        public int Count => _groups.Count;

        private readonly ImmutableDictionary<PositionGroupKey, IPositionGroup> _groups;
        private readonly ImmutableDictionary<Symbol, HashSet<IPositionGroup>> _groupsBySymbol;

        /// <summary>
        /// Initializes a new empty instance of the <see cref="PositionGroupCollection"/> class
        /// </summary>
        private PositionGroupCollection()
        {
            _groups = ImmutableDictionary<PositionGroupKey, IPositionGroup>.Empty;
            _groupsBySymbol = ImmutableDictionary<Symbol, HashSet<IPositionGroup>>.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionGroupCollection"/> class
        /// </summary>
        /// <remarks>
        /// This constructor assumes relationships between the two collections. Please use the other
        /// constructor or one of the <see cref="Create"/> methods.
        /// </remarks>
        private PositionGroupCollection(
            ImmutableDictionary<Symbol, HashSet<IPositionGroup>> groupsBySymbol,
            ImmutableDictionary<PositionGroupKey, IPositionGroup> groups
            )
        {
            if (groups == null)
            {
                throw new ArgumentNullException(nameof(groups));
            }
            if (groupsBySymbol == null)
            {
                throw new ArgumentNullException(nameof(groupsBySymbol));
            }

            _groups = groups;
            _groupsBySymbol = groupsBySymbol;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionGroupCollection"/> class
        /// </summary>
        public PositionGroupCollection(IReadOnlyCollection<IPositionGroup> groups)
            : this(
                groups.GroupBySymbol().ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value),
                groups.ToImmutableDictionary(grp => grp.Key)
            )
        {
        }

        /// <summary>
        /// Gets the <see cref="IPositionGroup"/> with the specified <paramref name="key"/>
        /// </summary>
        public IPositionGroup this[PositionGroupKey id] => _groups[id];

        /// <summary>
        /// Gets the position groups with the specified <paramref name="symbol"/>
        /// </summary>
        public IEnumerable<IPositionGroup> this[Symbol symbol] => _groupsBySymbol[symbol];

        /// <summary>
        /// Updates this collection with the specified <paramref name="group"/>
        /// </summary>
        public PositionGroupCollection SetItem(IPositionGroup group)
        {
            var bySymbol = _groupsBySymbol;
            var groups = _groups.SetItem(group.Key, group);
            foreach (var position in group)
            {
                var existing = bySymbol.GetValueOrDefault(position.Symbol)
                    ?? new HashSet<IPositionGroup>();
                existing.Add(group);
                bySymbol = bySymbol.SetItem(position.Symbol, existing);
            }

            return new PositionGroupCollection(bySymbol, groups);
        }

        /// <summary>
        /// Updates this collection with the specified <paramref name="groups"/>
        /// </summary>
        public PositionGroupCollection SetItems(IEnumerable<IPositionGroup> groups)
        {
            var newGroups = _groups;
            var bySymbol = _groupsBySymbol;
            foreach (var group in groups)
            {
                newGroups = newGroups.SetItem(group.Key, group);
                foreach (var position in group)
                {
                    var existing = bySymbol.GetValueOrDefault(position.Symbol)
                        ?? new HashSet<IPositionGroup>();
                    existing.Add(group);
                    bySymbol = bySymbol.SetItem(position.Symbol, existing);
                }
            }

            return new PositionGroupCollection(bySymbol, newGroups);
        }

        /// <summary>
        /// Removes the <see cref="IPositionGroup"/>  with the specified key
        /// </summary>
        public PositionGroupCollection Remove(PositionGroupKey id)
        {
            IPositionGroup group;
            var bySymbol = _groupsBySymbol;
            if (_groups.TryGetValue(id, out group))
            {
                foreach (var position in group)
                {
                    HashSet<IPositionGroup> forSymbol;
                    if (bySymbol.TryGetValue(position.Symbol, out forSymbol))
                    {
                        forSymbol.Remove(group);
                        bySymbol = bySymbol.SetItem(position.Symbol, forSymbol);
                    }
                }
            }
            return new PositionGroupCollection(bySymbol, _groups.Remove(id));
        }

        /// <summary>
        /// Removes all of the <see cref="IPositionGroup"/> instances referenced by the provided <paramref name="keys"/>
        /// </summary>
        public PositionGroupCollection RemoveRange(IEnumerable<PositionGroupKey> keys)
        {
            var groups = _groups;
            var bySymbol = _groupsBySymbol;
            foreach (var key in keys)
            {
                IPositionGroup group;
                if (_groups.TryGetValue(key, out group))
                {
                    groups = groups.Remove(key);
                    foreach (var position in group)
                    {
                        HashSet<IPositionGroup> forSymbol;
                        if (bySymbol.TryGetValue(position.Symbol, out forSymbol))
                        {
                            forSymbol.Remove(group);
                            bySymbol = bySymbol.SetItem(position.Symbol, forSymbol);
                        }
                    }
                }
            }
            return new PositionGroupCollection(bySymbol, groups);
        }

        /// <summary>
        /// Merges this position group collection with the provided <paramref name="other"/> collection.
        /// If symbols exist in both collections the associated <see cref="SecurityPosition"/> must
        /// be the same reference, and if not, an <see cref="InvalidOperationException"/> will be thrown.
        /// </summary>
        public PositionGroupCollection CombineWith(PositionGroupCollection other)
        {
            if (other == Empty)
            {
                return this;
            }

            return SetItems(other);
        }

        /// <summary>
        /// Gets the position groups the specified <paramref name="symbol"/> is currently a member of
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <returns>An enumerable of position groups containing this symbol</returns>
        public IEnumerable<IPositionGroup> GetPositionGroups(Symbol symbol)
        {
            HashSet<IPositionGroup> groups;
            if (_groupsBySymbol.TryGetValue(symbol, out groups))
            {
                return groups;
            }

            return Enumerable.Empty<IPositionGroup>();
        }

        /// <summary>
        /// Gets the <see cref="IPositionGroup"/> instances impacted by the <paramref name="contemplatedChanges"/>
        /// </summary>
        public IEnumerable<IPositionGroup> GetImpactedGroups(IEnumerable<IPosition> contemplatedChanges)
        {
            var preventDuplicates = new HashSet<IPositionGroup>();

            foreach (var position in contemplatedChanges)
            {
                HashSet<IPositionGroup> groups;
                if (!_groupsBySymbol.TryGetValue(position.Symbol, out groups))
                {
                    continue;
                }

                foreach (var group in groups.Where(preventDuplicates.Add))
                {
                    if (group.Quantity > 0)
                    {
                        yield return group;
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to retrieve the <see cref="IPositionGroup"/> for the specified <paramref name="key"/>
        /// </summary>
        public bool TryGetPositionGroup(PositionGroupKey id, out IPositionGroup group)
        {
            return _groups.TryGetValue(id, out group);
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<IPositionGroup> GetEnumerator()
        {
            foreach (var kvp in _groups)
            {
                yield return kvp.Value;
            }
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
