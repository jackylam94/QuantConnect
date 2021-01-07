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
    public class PositionCollection : IEnumerable<IPosition>
    {
        private readonly Dictionary<Symbol, Entry> _positions;
        private readonly SecurityPositionGroupDescriptor _defaultDescriptor;

        private PositionCollection(
            Dictionary<Symbol, Entry> positions,
            SecurityPositionGroupDescriptor defaultDescriptor
            )
        {
            _positions = positions;
            _defaultDescriptor = defaultDescriptor;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionCollection"/> class by creating a new
        /// set of <see cref="SecurityPosition"/> for each security that are completely disconnected from
        /// the algorithm's current set of positions and position groups.
        /// </summary>
        /// <remarks>
        /// The disconnect from the algorithm supports 'what-if' scenarios where we can contemplate how changing
        /// security holdings will impact the resolved position groups and the portfolios net margin requirements
        /// </remarks>
        public static PositionCollection Create(SecurityManager securities)
        {
            return Create(securities.Values,
                new SecurityPositionGroupDescriptor(securities,
                new SecurityPositionGroupBuyingPowerModel()
                )
            );
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionCollection"/> class by creating a new
        /// set of <see cref="SecurityPosition"/> for each security that are completely disconnected from
        /// the algorithm's current set of positions and position groups.
        /// </summary>
        /// <remarks>
        /// The disconnect from the algorithm supports 'what-if' scenarios where we can contemplate how changing
        /// security holdings will impact the resolved position groups and the portfolios net margin requirements
        /// </remarks>
        public static PositionCollection Create(IEnumerable<Security> securities, SecurityPositionGroupDescriptor defaultDescriptor)
        {
            return new PositionCollection(securities.ToDictionary(
                security => security.Symbol,
                security => new Entry(new SecurityPosition(security, defaultDescriptor))),
                defaultDescriptor
            );
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionCollection"/> from the specified <paramref name="positions"/>.
        /// This enumerable of positions MUST include an instance of <see cref="SecurityPosition"/> for each symbol
        /// </summary>
        public static PositionCollection Create(IEnumerable<IPosition> positions, SecurityPositionGroupDescriptor defaultDescriptor)
        {
            var dictionary = new Dictionary<Symbol, Entry>();
            foreach (var position in positions)
            {
                Entry entry;
                if (!dictionary.TryGetValue(position.Symbol, out entry))
                {
                    entry = new Entry();
                    dictionary[position.Symbol] = entry;
                }

                entry.Add(position);
            }

            return new PositionCollection(dictionary, defaultDescriptor);
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

            return entry.Remove(position);
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
