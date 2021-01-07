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
using System.Collections.Specialized;
using System.Linq;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Manages an <see cref="IAlgorithm"/>'s collection of <see cref="IPositionGroup"/>
    /// </summary>
    public class PositionGroupManager : IReadOnlyCollection<IPositionGroup>
    {
        /// <summary>
        /// Gets the count of position groups under management
        /// </summary>
        public int Count => Groups?.Count ?? 0;

        private bool _requiresGroupResolution;

        private readonly SecurityManager _securities;
        private readonly CompositePositionGroupResolver _resolver;
        private readonly List<IPositionGroupDescriptor> _descriptors;

        /// <summary>
        /// Gets the resolver used to determine the set of <see cref="IPositionGroup"/> for the algorithm
        /// </summary>
        public IPositionGroupResolver Resolver => _resolver;

        /// <summary>
        /// Gets the current set of <see cref="IPositionGroup"/> as resolved by the <see cref="Resolver"/>.
        /// NOTE: Do not save a reference to this property as it is changed each time groups are re-resolved
        /// </summary>
        public PositionGroupCollection Groups { get; private set; }

        /// <summary>
        /// Gets the collection of <see cref="IPositionGroupDescriptor"/> defining how groups are resolved and constructed
        /// </summary>
        public IReadOnlyCollection<IPositionGroupDescriptor> Descriptors => _descriptors;

        /// <summary>
        /// Gets the default position group descriptor used for security identity groups
        /// </summary>
        public SecurityPositionGroupDescriptor DefaultDescriptor { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionGroupManager"/> class
        /// </summary>
        /// <param name="securities">The algorithm's security manager</param>
        public PositionGroupManager(SecurityManager securities, SecurityPositionGroupDescriptor defaultDescriptor)
        {
            _securities = securities;
            _requiresGroupResolution = true;
            DefaultDescriptor = defaultDescriptor;
            Groups = PositionGroupCollection.Empty;
            _descriptors = new List<IPositionGroupDescriptor> {defaultDescriptor};
            _resolver = new CompositePositionGroupResolver{defaultDescriptor.Resolver};

            // we must be notified each time our holdings change, so each time a security is added, we
            // want to bind to its SecurityHolding.QuantityChanged event so we can trigger the resolver

            securities.CollectionChanged += (sender, args) =>
            {
                if (DefaultDescriptor == null)
                {
                    throw new InvalidOperationException(
                        "The default SecurityPositionGroupDescriptor must be registered before adding securities to the algorithm."
                    );
                }

                foreach (Security security in args.NewItems)
                {
                    IPositionGroup group;
                    var key = PositionGroupKey.Create(DefaultDescriptor, security);
                    if (args.Action == NotifyCollectionChangedAction.Add)
                    {
                        if (!Groups.TryGetPositionGroup(key, out group))
                        {
                            // simply adding a security doesn't require group resolution until it has holdings
                            // all we need to do is make sure we add the default SecurityPosition
                            group = new SecurityPosition(security, DefaultDescriptor);
                            Groups = Groups.SetItem(group);
                            security.Holdings.QuantityChanged += HoldingsOnQuantityChanged;
                            if (security.Invested)
                            {
                                // if this security has holdings then we'll need to resolve position groups
                                _requiresGroupResolution = true;
                            }
                        }
                    }
                    else if (args.Action == NotifyCollectionChangedAction.Remove)
                    {
                        if (Groups.TryGetPositionGroup(key, out group))
                        {
                            security.Holdings.QuantityChanged -= HoldingsOnQuantityChanged;
                            if (security.Invested)
                            {
                                // only trigger group resolution if we had holdings in the removed security
                                _requiresGroupResolution = true;
                            }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Registers the specified <paramref name="descriptor"/> and sets at which index its resolver should run.
        /// Specify <code>index=0</code> to run first and <code>index=Descriptors.Count-1</code> to run last. The
        /// last resolver is always the default resolver for the <see cref="SecurityPosition"/>.
        /// </summary>
        /// <param name="index">The index the descriptor's resolver should run at.</param>
        /// <param name="descriptor">The position group's descriptor to register</param>
        public void RegisterDescriptor(int index, IPositionGroupDescriptor descriptor)
        {
            if (index > Descriptors.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index,
                    "Index must be less than the Descriptors.Count to ensure the SecurityPositionGroupResolver always runs last."
                );
            }

            _descriptors.Add(descriptor);
            _resolver.Add(descriptor.Resolver, index);
        }

        /// <summary>
        /// Re-resolves the current set of <see cref="IPositionGroup"/> for the algorithm.
        /// </summary>
        public void ResolvePositionGroups()
        {
            if (_requiresGroupResolution)
            {
                Groups = Resolver.ResolvePositionGroups(
                    PositionCollection.Create(_securities)
                );

                _requiresGroupResolution = false;
            }
        }

        /// <summary>
        /// Resolves the groups resulting from the specified <paramref name="positions"/>. This is useful for
        /// what-if analysis of contemplated position changes
        /// </summary>
        /// <param name="positions">The positions</param>
        /// <returns>The positions resolved into groups using the registered resolvers</returns>
        public PositionGroupCollection ResolvePositionGroups(IEnumerable<IPosition> positions)
        {
            return Resolver.ResolvePositionGroups(positions as PositionCollection
                ?? PositionCollection.Create(positions, DefaultDescriptor)
            );
        }

        /// <summary>
        /// Gets the default <see cref="SecurityPosition"/> for the specified <paramref name="symbol"/>
        /// </summary>
        /// <param name="symbol">The symbol whose default group we seek</param>
        /// <returns>The default position group for the specified symbol</returns>
        public SecurityPosition GetDefaultPositionGroup(Symbol symbol)
        {
            return GetDefaultPositionGroup(_securities[symbol]);
        }

        /// <summary>
        /// Gets the default <see cref="SecurityPosition"/> for the specified <paramref name="security"/>
        /// </summary>
        /// <param name="security">The security whose default group we seek</param>
        /// <returns>The default position group for the specified security</returns>
        public SecurityPosition GetDefaultPositionGroup(Security security)
        {
            IPositionGroup group;
            if (!Groups.TryGetPositionGroup(PositionGroupKey.Create(DefaultDescriptor, security), out group))
            {
                throw new KeyNotFoundException($"Default position group for {security.Symbol} was not found.");
            }

            return (SecurityPosition) group;
        }

        /// <summary>
        /// Gets the algorithm's current holdings in the position group identified by the specified <paramref name="key"/>
        /// </summary>
        /// <param name="key">The key defining the position group we seek</param>
        /// <returns>An <see cref="IPositionGroup"/> containing the algorithm's current holdings in this specified group</returns>
        public IPositionGroup GetPositionGroup(PositionGroupKey key)
        {
            IPositionGroup group;
            return Groups.TryGetPositionGroup(key, out group) ? group : PositionGroup.Empty(key);
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<IPositionGroup> GetEnumerator()
        {
            return Groups.GetEnumerator();
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void HoldingsOnQuantityChanged(object sender, SecurityHoldingQuantityChangedEventArgs e)
        {
            // we don't want to re-resolve every time quantity changes because then we might by re-resolving
            // multiple times in the same time step. instead, we'll mark us as needing resolution and the
            // AlgorithmManager will invoke ResolvePositionGroups at the end of the time step
            _requiresGroupResolution = true;
        }
    }
}
