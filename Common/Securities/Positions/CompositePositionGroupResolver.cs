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

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Provides an implementation of <see cref="IPositionGroupResolver"/> that invokes multiple wrapped implementations
    /// in succession. Each successive call to <see cref="IPositionGroupResolver.ResolvePositionGroups"/> will receive
    /// the remaining positions that have yet to be grouped. Any non-grouped positions are placed into identity groups.
    /// </summary>
    public class CompositePositionGroupResolver : IPositionGroupResolver, IReadOnlyCollection<IPositionGroupResolver>
    {
        /// <summary>
        /// Gets the count of registered resolvers
        /// </summary>
        public int Count => _resolvers.Count;

        private readonly List<IPositionGroupResolver> _resolvers;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositePositionGroupResolver"/> class
        /// </summary>
        /// <param name="resolvers">The position group resolvers to be invoked in order</param>
        public CompositePositionGroupResolver(params IPositionGroupResolver[] resolvers)
            : this ((IEnumerable<IPositionGroupResolver>) resolvers)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositePositionGroupResolver"/> class
        /// </summary>
        /// <param name="resolvers">The position group resolvers to be invoked in order</param>
        public CompositePositionGroupResolver(IEnumerable<IPositionGroupResolver> resolvers)
        {
            _resolvers = resolvers.ToList();
        }

        /// <summary>
        /// Adds the specified <paramref name="resolver"/> to the end of the list of resolvers. This resolver will run last.
        /// </summary>
        /// <param name="resolver">The resolver to be added</param>
        public void Add(IPositionGroupResolver resolver)
        {
            _resolvers.Add(resolver);
        }

        /// <summary>
        /// Inserts the specified <paramref name="resolver"/> into the list of resolvers at the specified index.
        /// </summary>
        /// <param name="resolver">The resolver to be inserted</param>
        /// <param name="index">The zero based index indicating where to insert the resolver, zero inserts to the beginning
        /// of the list making this resolver un first and <see cref="Count"/> inserts the resolver to the end of the list
        /// making this resolver run last</param>
        public void Add(IPositionGroupResolver resolver, int index)
        {
            // insert handles bounds checking
            _resolvers.Insert(index, resolver);
        }

        /// <summary>
        /// Removes the specified <paramref name="resolver"/> from the list of resolvers
        /// </summary>
        /// <param name="resolver">The resolver to be removed</param>
        /// <returns>True if the resolver was removed, false if it wasn't found in the list</returns>
        public bool Remove(IPositionGroupResolver resolver)
        {
            return _resolvers.Remove(resolver);
        }

        /// <summary>
        /// Resolves the optimal set of <see cref="IPositionGroup"/> from the provided <paramref name="positions"/>.
        /// Implementations are required to deduct grouped positions from the <paramref name="positions"/> collection.
        /// </summary>
        public PositionGroupCollection ResolvePositionGroups(PositionCollection positions)
        {
            // we start with no groups, each resolver's result will get merged in
            var groups = PositionGroupCollection.Empty;

            // each call to ResolvePositionGroups is expected to deduct grouped positions from the PositionCollection
            foreach (var resolver in _resolvers)
            {
                var resolved = resolver.ResolvePositionGroups(positions);
                groups = groups.CombineWith(resolved);
            }

            if (positions.Count > 0)
            {
                throw new InvalidOperationException("All positions must be resolved into groups.");
            }

            return groups;
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<IPositionGroupResolver> GetEnumerator()
        {
            return _resolvers.GetEnumerator();
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
