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
using System.Collections.Immutable;
using NUnit.Framework;
using QuantConnect.Securities.Positions;
using static QuantConnect.Securities.Positions.PositionGroupKey;

namespace QuantConnect.Tests.Common.Securities.Positions
{
    [TestFixture]
    public class PositionGroupKeyTests
    {
        private PositionGroupKey key;

        [SetUp]
        public void Setup()
        {
            key = new PositionGroupKey(SecurityPositionGroupDescriptor.Instance,
                ImmutableArray<UnitQuantity>.Empty
                    .Add(new UnitQuantity(Symbols.SPY, 100))
                    .Add(new UnitQuantity(Symbols.SPY_C_192_Feb19_2016, -1))
            );
        }

        [Test]
        public void EqualsAndCreate_AreDeterministic_BasedOnUnitQuantities_AndModels()
        {
            var key2 = new PositionGroupKey(SecurityPositionGroupDescriptor.Instance,
                ImmutableArray<UnitQuantity>.Empty
                    .Add(new UnitQuantity(Symbols.SPY, 100))
                    .Add(new UnitQuantity(Symbols.SPY_C_192_Feb19_2016, -1))
            );

            Assert.AreEqual(key, key2);
            Assert.AreEqual(key.GetHashCode(), key2.GetHashCode());

            var key3 = new PositionGroupKey(new DifferentModel(),
                ImmutableArray<UnitQuantity>.Empty
                    .Add(new UnitQuantity(Symbols.SPY, 100))
                    .Add(new UnitQuantity(Symbols.SPY_C_192_Feb19_2016, -1))
            );

            Assert.AreNotEqual(key, key3);
            Assert.AreNotEqual(key.GetHashCode(), key3.GetHashCode());
        }

        [Test]
        public void GetUnitQuantity_Returns_UnitQuantityForSymbol()
        {
            var unitQuantity = key.GetUnitQuantity(Symbols.SPY);
            Assert.AreEqual(100, unitQuantity);

            unitQuantity = key.GetUnitQuantity(Symbols.SPY_C_192_Feb19_2016);
            Assert.AreEqual(-1, unitQuantity);
        }

        [Test]
        public void CreatePosition_ReturnsNewPosition_WithQuantityComputedByNumberOfUnits()
        {
            var position = key.CreatePosition(Symbols.SPY, 5);
            Assert.AreEqual(500, position.Quantity);

            position = key.CreatePosition(Symbols.SPY_C_192_Feb19_2016, 5);
            Assert.AreEqual(-5, position.Quantity);
        }

        [Test]
        public void Create_FromPositionGroup_ResolvesUnitQuantities()
        {
            var group1 = PositionGroup.Create(SecurityPositionGroupDescriptor.Instance,
                new Position(Symbols.SPY, 500, 100),
                new Position(Symbols.SPY_C_192_Feb19_2016, -5, -1)
            );

            var expected = new[]
            {
                new UnitQuantity(Symbols.SPY, 100),
                new UnitQuantity(Symbols.SPY_C_192_Feb19_2016, -1)
            };

            CollectionAssert.AreEqual(expected, Create(group1).UnitQuantities);
        }

        private class DifferentModel : IPositionGroupDescriptor
        {
            public Type Type { get; }
            public IPositionGroupResolver Resolver { get; }
            public IPositionGroupBuyingPowerModel BuyingPowerModel { get; }
            public string GetUserFriendlyName(IPositionGroup @group) { throw new NotImplementedException(); }
            public IPositionGroup CreatePositionGroup(IReadOnlyCollection<IPosition> positions) { throw new NotImplementedException(); }
            public IPosition CreatePosition(Symbol symbol, decimal quantity, decimal unitQuantity) { throw new NotImplementedException(); }
            public IEnumerable<IPositionGroup> GetImpactedGroups(PositionGroupCollection groups, Symbol symbol) { throw new NotImplementedException(); }
        }
    }
}
