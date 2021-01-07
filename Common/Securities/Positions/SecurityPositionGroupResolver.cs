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

using System.Linq;

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Provides an implementation of <see cref="IPositionGroupResolver"/> that resolves all positions provided into
    /// the default group type of <see cref="SecurityPosition"/>, each containing exactly one position of type
    /// <see cref="SecurityPosition"/>
    /// </summary>
    public class SecurityPositionGroupResolver : IPositionGroupResolver
    {
        private readonly SecurityManager _securities;
        private readonly SecurityPositionGroupDescriptor _descriptor;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityPositionGroupResolver"/> class
        /// </summary>
        /// <param name="securities">The algorithm's security manager</param>
        /// <param name="descriptor">The descriptor for security position groups</param>
        public SecurityPositionGroupResolver(SecurityManager securities, SecurityPositionGroupDescriptor descriptor)
        {
            _securities = securities;
            _descriptor = descriptor;
        }

        /// <summary>
        /// Resolves the optimal set of <see cref="IPositionGroup"/> from the provided <paramref name="positions"/>.
        /// Implementations are required to deduct grouped positions from the <paramref name="positions"/> collection.
        /// </summary>
        public PositionGroupCollection ResolvePositionGroups(PositionCollection positions)
        {
            var groups = new PositionGroupCollection(positions.GetPositionsBySymbol().Select(kvp =>
            {
                if (kvp.Value.Count == 1)
                {
                    var securityPosition = kvp.Value.Single() as SecurityPosition;
                    if (securityPosition != null)
                    {
                        return securityPosition;
                    }
                }

                return new SecurityPosition(
                    _securities[kvp.Key], kvp.Value.Sum(p => p.Quantity), _descriptor
                );
            }).ToList());

            // this resolver should always run last, but we should still note that we've consumed all available positions
            positions.Clear();

            return groups;
        }
    }
}
