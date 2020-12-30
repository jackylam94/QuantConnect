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
        private readonly ImmutableDictionary<Symbol, SecurityPosition> _identityGroups;

        /// <summary>
        /// Initializes a new empty instance of the <see cref="PositionGroupCollection"/> class
        /// </summary>
        private PositionGroupCollection()
        {
            _groups = ImmutableDictionary<PositionGroupKey, IPositionGroup>.Empty;
            _identityGroups = ImmutableDictionary<Symbol, SecurityPosition>.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionGroupCollection"/> class
        /// </summary>
        /// <remarks>
        /// This constructor is private to ensure the dictionary references aren't leaked. Please use
        /// the <see cref="Create"/> method to initialize new instances of this collection type.
        /// </remarks>
        private PositionGroupCollection(
            ImmutableDictionary<Symbol, SecurityPosition> identityGroups,
            ImmutableDictionary<PositionGroupKey, IPositionGroup> groups
            )
        {
            _groups = groups;
            _identityGroups = identityGroups;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionGroupCollection"/> class by
        /// pulling out all of the groups defined in <see cref="SecurityPosition.Groups"/>
        /// </summary>
        public static PositionGroupCollection Create(IEnumerable<SecurityPosition> groups)
        {
            var identityGroups = groups.ToImmutableDictionary(group => group.Security.Symbol);

            return new PositionGroupCollection(identityGroups, identityGroups
                .SelectMany(group => group.Value.Groups)
                .DistinctBy(group => group.Key)
                .ToImmutableDictionary(group => group.Key)
            );
        }

        /// <summary>
        /// Gets the <see cref="IPositionGroup"/> with the specified <paramref name="key"/>
        /// </summary>
        public IPositionGroup this[PositionGroupKey id] => _groups[id];

        /// <summary>
        /// Gets the <see cref="SecurityPosition"/> with the specified <paramref name="symbol"/>
        /// </summary>
        public SecurityPosition this[Symbol symbol] => _identityGroups[symbol];

        /// <summary>
        /// Updates this collection with all the groups referenced by this provided <see cref="SecurityPosition"/>
        /// </summary>
        public PositionGroupCollection SetItem(SecurityPosition group)
        {
            var identityGroups = _identityGroups.SetItem(group.Symbol, group);

            return new PositionGroupCollection(identityGroups, _groups.SetItems(group.Groups.Select(
                grp => new KeyValuePair<PositionGroupKey, IPositionGroup>(grp.Key, grp)
            )));
        }

        /// <summary>
        /// Updates this collection with all the groups referenced by this provided <see cref="SecurityPosition"/>
        /// </summary>
        public PositionGroupCollection SetItems(IEnumerable<SecurityPosition> groups)
        {
            var collection = groups as IReadOnlyCollection<SecurityPosition> ?? groups.ToList();
            var identityGroups = _identityGroups.SetItems(collection
                .Select(grp => new KeyValuePair<Symbol, SecurityPosition>(grp.Symbol, grp))
            );

            return new PositionGroupCollection(identityGroups, _groups.SetItems(collection
                .SelectMany(grp => grp.Groups)
                .DistinctBy(grp => grp.Key)
                .Select(grp => new KeyValuePair<PositionGroupKey, IPositionGroup>(grp.Key, grp))
            ));
        }

        /// <summary>
        /// Updates this collection with the specified <paramref name="group"/>
        /// </summary>
        public PositionGroupCollection SetItem(IPositionGroup group)
        {
            var groups = _groups.SetItem(group.Key, group);

            return new PositionGroupCollection(_identityGroups, groups);
        }

        /// <summary>
        /// Updates this collection with the specified <paramref name="groups"/>
        /// </summary>
        public PositionGroupCollection SetItems(IEnumerable<IPositionGroup> groups)
        {
            return new PositionGroupCollection(_identityGroups, _groups.SetItems(groups
                .Select(grp => new KeyValuePair<PositionGroupKey, IPositionGroup>(grp.Key, grp))
            ));
        }

        /// <summary>
        /// Removes the <see cref="IPositionGroup"/>  with the specified key
        /// </summary>
        public PositionGroupCollection Remove(PositionGroupKey id)
        {
            return new PositionGroupCollection(_identityGroups, _groups.Remove(id));
        }

        /// <summary>
        /// Removes all of the <see cref="IPositionGroup"/> instances referenced by the provided <paramref name="keys"/>
        /// </summary>
        public PositionGroupCollection RemoveRange(IEnumerable<PositionGroupKey> keys)
        {
            return new PositionGroupCollection(_identityGroups, _groups.RemoveRange(keys));
        }

        /// <summary>
        /// Merges this position group collection with the provided <paramref name="other"/> collection.
        /// If symbols exist in both collections the associated <see cref="SecurityPosition"/> must
        /// be the same reference, and if not, an <see cref="InvalidOperationException"/> will be thrown.
        /// </summary>
        public PositionGroupCollection CombineWith(PositionGroupCollection other)
        {
            var groupUpdates = new List<KeyValuePair<PositionGroupKey, IPositionGroup>>();
            var identityGroupAdditions = new List<KeyValuePair<Symbol, SecurityPosition>>();
            foreach (var kvp in other._identityGroups)
            {
                SecurityPosition existing;
                if (_identityGroups.TryGetValue(kvp.Key, out existing))
                {
                    if (!ReferenceEquals(existing, kvp.Value))
                    {
                        throw new InvalidOperationException(
                            $"Duplicate SecurityPosition for symbol provided: {kvp.Key}"
                        );
                    }
                }
                else
                {
                    identityGroupAdditions.Add(
                        new KeyValuePair<Symbol, SecurityPosition>(kvp.Key, kvp.Value)
                    );
                }

                foreach (var group in kvp.Value.Groups)
                {
                    groupUpdates.Add(
                        new KeyValuePair<PositionGroupKey, IPositionGroup>(group.Key, group)
                    );
                }
            }

            return new PositionGroupCollection(
                _identityGroups.SetItems(identityGroupAdditions),
                _groups.SetItems(groupUpdates)
            );
        }

        /// <summary>
        /// Gets the <see cref="IPositionGroup"/> instances impacted by the <paramref name="contemplatedChanges"/>
        /// </summary>
        public IEnumerable<IPositionGroup> GetImpactedGroups(IEnumerable<IPosition> contemplatedChanges)
        {
            var preventDuplicates = new HashSet<IPositionGroup>();

            foreach (var position in contemplatedChanges)
            {
                // for each symbol we need to check impacted groups for each unique group type the symbol is a member of
                // this is so we can detect overlapping groups, for example, GOOG equity can be in default group and options groups
                // so even if the contemplated change is in the default GOOG group, we still need to look for options groups
                SecurityPosition defaultGroup;
                if (!TryGetSecurityGroup(position.Symbol, out defaultGroup))
                {
                    continue;
                }

                foreach (var type in defaultGroup.Groups.Select(grp => grp.Descriptor).Distinct())
                {
                    foreach (var group in type.GetImpactedGroups(this, position.Symbol))
                    {
                        if (preventDuplicates.Add(group))
                        {
                            yield return group;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to retrieve the <see cref="SecurityPosition"/> for the specified <paramref name="symbol"/>
        /// </summary>
        public bool TryGetSecurityGroup(Symbol symbol, out SecurityPosition group)
        {
            return _identityGroups.TryGetValue(symbol, out group);
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
            // yield SecurityPosition instances
            foreach (var kvp in _identityGroups)
            {
                yield return kvp.Value;
            }

            // yield any groups resolved by matchers
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
