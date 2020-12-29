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
    /// Provides a collection for <see cref="IPosition"/>, keyed by <see cref="SecurityPosition"/>,
    /// supporting multiple positions per symbol. The common usage is to initialize this collection with
    /// <see cref="SecurityPosition"/> instances and break apart individual <see cref="IPosition"/> as holdings
    /// are grouped into <see cref="IPositionGroup"/> as part of the <see cref="IPositionGroupResolver"/>
    /// </summary>
    public class PositionCollection : IEnumerable<IPosition>
    {
        /// <summary>
        /// Event raised each time a new <see cref="IPosition"/> is added. This event is NOT raised
        /// when adding a new <see cref="SecurityPosition"/>: see <see cref="SecurityPositionAdded"/>
        /// </summary>
        public event EventHandler<PositionAddedEventArgs> PositionAdded;

        /// <summary>
        /// Event raised each time a new <see cref="SecurityPosition"/> is added.
        /// </summary>
        public event EventHandler<SecurityPositionAddedEventArgs> SecurityPositionAdded;

        /// <summary>
        /// Event raised each time a <see cref="IPosition"/> is added. This event is NOT raised when
        /// removed a <see cref="SecurityPosition"/>: see <see cref="SecurityPositionRemoved"/>
        /// </summary>
        public event EventHandler<PositionRemovedEventArgs> PositionRemoved;

        /// <summary>
        /// Event raised each time a <see cref="SecurityPosition"/> is removed.
        /// </summary>
        public event EventHandler<SecurityPositionRemovedEventArgs> SecurityPositionRemoved;

        private readonly Dictionary<Symbol, Entry> _positions;

        /// <summary>
        /// Initializes an new empty instance of the <see cref="PositionCollection"/> class
        /// </summary>
        public PositionCollection()
        {
            _positions = new Dictionary<Symbol, Entry>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionCollection"/> class
        /// </summary>
        private PositionCollection(Dictionary<Symbol, Entry> positions)
        {
            _positions = positions;
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
        public static PositionCollection Create(IEnumerable<Security> securities)
        {
            return new PositionCollection(securities.ToDictionary(
                security => security.Symbol,
                security => new Entry(new SecurityPosition(security))
            ));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionCollection"/> from the specified <paramref name="positions"/>.
        /// This enumerable of positions MUST include an instance of <see cref="SecurityPosition"/> for each symbol
        /// </summary>
        public static PositionCollection Create(IEnumerable<IPosition> positions)
        {
            var dictionary = new Dictionary<Symbol, Entry>();
            foreach (var grouping in positions.GroupBy(p => p.Symbol))
            {
                var set = new HashSet<IPosition>();
                SecurityPosition securityPosition = null;
                foreach (var pos in grouping)
                {
                    if (pos.GetType() == typeof(SecurityPosition))
                    {
                        if (securityPosition != null)
                        {
                            throw new InvalidOperationException($"Duplicate SecurityPosition provided for {pos.Symbol}");
                        }

                        securityPosition = (SecurityPosition) pos;
                    }
                    else
                    {
                        set.Add(pos);
                    }
                }

                if (securityPosition == null)
                {
                    throw new InvalidOperationException($"SecurityPosition was not provided for {grouping.Key}");
                }

                dictionary.Add(grouping.Key, new Entry(securityPosition, set));
            }

            return new PositionCollection(dictionary);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="PositionCollection"/> class populated with default <see cref="SecurityPosition"/>
        /// instances containing all holdings for each symbol specified in <paramref name="symbols"/>
        /// </summary>
        /// <param name="securities">The algorithm's security manager used to create new <see cref="SecurityPosition"/> instances
        /// that are disconnected from the official set. This enables contemplating changes to position groups without affecting the
        /// current state of position groups</param>
        /// <param name="symbols">The symbols to include in the new position collection</param>
        /// <returns>A collection of default <see cref="SecurityPosition"/> for the provided <paramref name="symbols"/></returns>
        public static PositionCollection CreateWithSecurityPositions(SecurityManager securities, IEnumerable<Symbol> symbols)
        {
            var positions = new Dictionary<Symbol, Entry>();
            foreach (var symbol in symbols)
            {
                Entry entry;
                if (!positions.TryGetValue(symbol, out entry))
                {
                    var security = securities[symbol];
                    entry = new Entry(new SecurityPosition(security));
                    positions[symbol] = entry;
                }
            }

            return new PositionCollection(positions);
        }

        /// <summary>
        /// Creates a new <see cref="PositionGroupCollection"/> containing each <see cref="SecurityPositionGroup"/> present
        /// in this collection. The security position groups are resolved via <see cref="SecurityPosition.DefaultGroup"/>
        /// </summary>
        public PositionGroupCollection CreateDefaultPositionGroupCollection()
        {
            return PositionGroupCollection.Create(_positions.Values
                .Select(entry => entry.SecurityPosition.DefaultGroup)
            );
        }

        /// <summary>
        /// Adds the specified <paramref name="position"/> to this collection. If the position is
        /// an instance of <see cref="SecurityPosition"/>, then it's added as a key
        /// </summary>
        public void Add(IPosition position)
        {
            Entry entry;
            if (position.GetType() == typeof(SecurityPosition))
            {
                entry = new Entry((SecurityPosition) position);
                _positions.Add(entry.Symbol, entry);
                OnSecurityPositionAdded(entry.SecurityPosition);
            }
            else if (_positions.TryGetValue(position.Symbol, out entry))
            {
                entry.Positions.Add(position);
                OnPositionAdded(position);
            }
            else
            {
                throw new KeyNotFoundException($"The SecurityPosition for {position.Symbol} hasn't " +
                    "been added yet and must be added before adding positions for this symbol."
                );
            }
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

            if (position.GetType() == typeof(SecurityPosition))
            {
                return Remove(position.Symbol);
            }

            if (entry.Positions.Remove(position))
            {
                OnPositionRemoved(position);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to remove all positions related to the provided <paramref name="symbol"/>.
        /// If positions for the symbol are not found, then <code>false</code> will be returned.
        /// </summary>
        public bool Remove(Symbol symbol)
        {
            Entry entry;
            if (!_positions.TryGetValue(symbol, out entry))
            {
                return false;
            }

            _positions.Remove(symbol);
            OnSecurityPositionRemoved(entry.SecurityPosition);
            return true;
        }

        /// <summary>
        /// Creates a new <see cref="IPosition"/> but deducting the specified <paramref name="quantity"/>
        /// from the <paramref name="symbol"/>'s default <see cref="SecurityPosition"/>.
        /// If there is insufficient quantity remaining in the default position, an <see cref="InvalidOperationException"/> will be thrown.
        /// If the requested symbol is not found, an <see cref="KeyNotFoundException"/> will be thrown.
        /// </summary>
        /// <param name="symbol">The symbol of the position to create</param>
        /// <param name="quantity">The quantity of the new position</param>
        /// <returns></returns>
        public IPosition CreatePosition(Symbol symbol, decimal quantity)
        {
            Entry entry;
            if (!_positions.TryGetValue(symbol, out entry))
            {
                throw new KeyNotFoundException($"Symbol was not found: {symbol}");
            }

            return entry.CreatePosition(quantity);
        }

        /// <summary>
        /// Gets the <see cref="SecurityPosition"/> for the specified <paramref name="symbol"/>
        /// </summary>
        /// <param name="symbol">The symbol to search for</param>
        public SecurityPosition GetSecurityPosition(Symbol symbol)
        {
            Entry entry;
            if (!_positions.TryGetValue(symbol, out entry))
            {
                throw new KeyNotFoundException($"{symbol} was not found in the positions collection.");
            }

            return entry.SecurityPosition;
        }

        /// <summary>
        /// Gets all of the positions tracked by this collection for the specified symbol, optionally including
        /// the security's default <see cref="SecurityPosition"/>
        /// </summary>
        /// <param name="symbol">The symbol to search for</param>
        /// <param name="includeSecurityPosition">True to include the default <see cref="SecurityPosition"/>, otherwise false</param>
        /// <returns>An enumerable of the <see cref="IPosition"/> tracked by this collection for the specified <paramref name="symbol"/></returns>
        public IEnumerable<IPosition> GetPositions(Symbol symbol, bool includeSecurityPosition)
        {
            Entry entry;
            if (!_positions.TryGetValue(symbol, out entry))
            {
                // typically when returning an enumerable you don't throw
                yield break;
            }

            if (includeSecurityPosition)
            {
                yield return entry.SecurityPosition;
            }

            foreach (var position in entry.Positions)
            {
                yield return position;
            }
        }

        /// <summary>
        /// Gets all of the default <see cref="SecurityPosition"/> contained within this collection.
        /// </summary>
        public IEnumerable<SecurityPosition> GetSecurityPositions()
        {
            foreach (var entry in _positions)
            {
                yield return entry.Value.SecurityPosition;
            }
        }

        /// <summary>
        /// Clones this collection
        /// </summary>
        public PositionCollection Clone()
        {
            return new PositionCollection(_positions.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<IPosition> GetEnumerator()
        {
            foreach (var kvp in _positions)
            {
                yield return kvp.Value.SecurityPosition;

                foreach (var position in kvp.Value.Positions)
                {
                    yield return position;
                }
            }
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Event invocator for the <see cref="PositionAdded"/> event
        /// </summary>
        protected virtual void OnPositionAdded(IPosition position)
        {
            PositionAdded?.Invoke(this, new PositionAddedEventArgs(this, position));
        }

        /// <summary>
        /// Event invocator for the <see cref="SecurityPositionAdded"/> event
        /// </summary>
        protected virtual void OnSecurityPositionAdded(SecurityPosition position)
        {
            SecurityPositionAdded?.Invoke(this, new SecurityPositionAddedEventArgs(this, position));
        }

        /// <summary>
        /// Event invocator for the <see cref="PositionRemoved"/> event
        /// </summary>
        protected virtual void OnPositionRemoved(IPosition position)
        {
            PositionRemoved?.Invoke(this, new PositionRemovedEventArgs(this, position));
        }

        /// <summary>
        /// Event invocator for the <see cref="SecurityPositionRemoved"/> event
        /// </summary>
        protected virtual void OnSecurityPositionRemoved(SecurityPosition position)
        {
            SecurityPositionRemoved?.Invoke(this, new SecurityPositionRemovedEventArgs(this, position));
        }

        private struct Entry
        {
            public HashSet<IPosition> Positions { get; }
            public Symbol Symbol => SecurityPosition.Symbol;
            public SecurityPosition SecurityPosition { get; }

            public Entry(SecurityPosition securityPosition)
            {
                SecurityPosition = securityPosition;
                Positions = new HashSet<IPosition>();
            }

            public Entry(SecurityPosition securityPosition, HashSet<IPosition> positions)
            {
                SecurityPosition = securityPosition;
                Positions = positions;
            }

            public IPosition CreatePosition(decimal quantity)
                => SecurityPosition.CreatePosition(quantity);
        }
    }
}
