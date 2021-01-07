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

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Provides a collection implementation of <see cref="IPosition"/>
    /// </summary>
    public class PositionCollection : IReadOnlyCollection<IPosition>
    {
        /// <summary>Gets the number of elements in the collection.</summary>
        /// <returns>The number of elements in the collection. </returns>
        public int Count => _count;

        private int _count;
        private readonly Dictionary<Symbol, Entry> _positions;

        private PositionCollection(Dictionary<Symbol, Entry> positions, int count)
        {
            _count = count;
            _positions = positions;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionCollection"/> from the specified <paramref name="positions"/>.
        /// </summary>
        public static PositionCollection Create(IEnumerable<IPosition> positions)
        {
            var count = 0;
            var dictionary = new Dictionary<Symbol, Entry>();
            foreach (var position in positions)
            {
                count++;
                Entry entry;
                if (!dictionary.TryGetValue(position.Symbol, out entry))
                {
                    entry = new Entry();
                    dictionary[position.Symbol] = entry;
                }

                entry.Add(position);
            }

            return new PositionCollection(dictionary, count);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionCollection"/> class by creating a new
        /// set of <see cref="SecurityPosition"/> for each security containing all of the algorithm's
        /// holdings for each security
        /// </summary>
        public static PositionCollection CreateDefault(SecurityManager securities)
        {
            var defaultDescriptor = new SecurityPositionGroupDescriptor(securities,
                new SecurityPositionGroupBuyingPowerModel()
            );

            return CreateDefault(securities.Values, defaultDescriptor);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionCollection"/> class by creating a new
        /// set of <see cref="SecurityPosition"/> for each security containing all of the algorithm's
        /// holdings for each security
        /// </summary>
        public static PositionCollection CreateDefault(IEnumerable<Security> securities, SecurityPositionGroupDescriptor defaultDescriptor)
        {
            return CreateDefault(securities.Select(
                security => new SecurityPosition(security, defaultDescriptor))
            );
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionCollection"/> class from the specified
        /// <see cref="SecurityPosition"/> instances.
        /// </summary>
        /// <param name="positions">The default security positions</param>
        /// <returns>A new position collection containing the specified positions</returns>
        public static PositionCollection CreateDefault(IEnumerable<SecurityPosition> positions)
        {
            var dictionary = positions.ToDictionary(p => p.Symbol, p => new Entry(p));
            return new PositionCollection(dictionary, dictionary.Count);
        }

        /// <summary>
        /// Adds the specified <paramref name="position"/> to this collection. If the position is
        /// an instance of <see cref="SecurityPosition"/>, then it's added as a key
        /// </summary>
        public void Add(IPosition position)
        {
            Entry entry;
            if (!_positions.TryGetValue(position.Symbol, out entry))
            {
                entry = new Entry();
                _positions[position.Symbol] = entry;
            }

            _count++;
            entry.Add(position);
        }

        /// <summary>
        /// Attempts to remove the specified <paramref name="position"/> from this collection.
        /// If the position isn't found, then <code>false</code> will be returned.
        /// </summary>
        public bool Remove(IPosition position)
        {
            Entry entry;
            if (!_positions.TryGetValue(position.Symbol, out entry))
            {
                return false;
            }

            var removed = entry.Remove(position);
            if (removed)
            {
                _count--;
            }

            return removed;
        }

        /// <summary>
        /// Clears this collection of all positions
        /// </summary>
        public void Clear()
        {
            _count = 0;
            _positions.Clear();
        }

        /// <summary>
        /// Gets an enumerable of all positions in this collection grouped by the position's key
        /// </summary>
        public IEnumerable<KeyValuePair<Symbol, IReadOnlyCollection<IPosition>>> GetPositionsBySymbol()
        {
            foreach (var entry in _positions)
            {
                yield return new KeyValuePair<Symbol, IReadOnlyCollection<IPosition>>(
                    entry.Key, entry.Value.Positions
                );
            }
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<IPosition> GetEnumerator()
        {
            foreach (var kvp in _positions)
            {
                foreach (var position in kvp.Value.Positions)
                {
                    yield return position;
                }
            }
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private class Entry
        {
            private readonly HashSet<IPosition> _positions;

            public IReadOnlyCollection<IPosition> Positions => _positions;

            public Entry()
            {
                _positions = new HashSet<IPosition>();
            }

            public Entry(IPosition position)
            {
                _positions = new HashSet<IPosition>{position};
            }

            public void Add(IPosition position)
            {
                _positions.Add(position);
            }

            public bool Remove(IPosition position)
            {
                return _positions.Remove(position);
            }
        }
    }
}
