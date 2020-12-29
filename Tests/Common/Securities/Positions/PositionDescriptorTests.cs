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

using NUnit.Framework;
using QuantConnect.Securities.Positions;

namespace QuantConnect.Tests.Common.Securities.Positions
{
    [TestFixture]
    public class PositionTests
    {
        private IPosition _position;

        [SetUp]
        public void Setup()
        {
            // for an equity in the default group UnitQuantity=1
            // for an equity in an options group UnitQuantity=100 (the contract multiplier of the grouped contracts)
            _position = new Position(Symbols.AAPL, 75m, 1m);
        }

        [Test]
        public void InitializesProperties()
        {
            Assert.AreEqual(1m, _position.UnitQuantity);
            Assert.AreEqual(75m, _position.Quantity);
            Assert.AreEqual(Symbols.AAPL, _position.Symbol);
        }
    }

    [TestFixture]
    public class PositionExtensionsTests
    {
        private IPosition _position;

        [SetUp]
        public void Setup()
        {
            // typical equity position in an options grouping w/ contracts having the normal ContractMultiplier=100
            _position = new Position(Symbols.AAPL, 500m, 100m);
        }

        [Test]
        public void Empty_ReturnsPositionWithZeroQuantity()
        {
            var empty = _position.Empty();
            Assert.AreEqual(0, empty.Quantity);
            Assert.AreEqual(_position.Symbol, empty.Symbol);
            Assert.AreEqual(_position.UnitQuantity, empty.UnitQuantity);
        }

        [Test]
        public void IsEmpty_ReturnsTrue_WhenPositionQuantityIsZero()
        {
            Assert.IsFalse(_position.IsEmpty());
            Assert.IsTrue(_position.Empty().IsEmpty());
        }

        [Test]
        public void Negate_ReturnsNewPosition_WithNegativeQuantity()
        {
            var negated = _position.Negate();
            Assert.AreEqual(_position.Quantity, -negated.Quantity);
            Assert.AreEqual(_position.Symbol, negated.Symbol);
            Assert.AreEqual(_position.UnitQuantity, negated.UnitQuantity);
        }

        [Test]
        public void Negate_ReturnsSamePosition_WhenQuantityEqualsZero()
        {
            var empty = _position.Empty();
            Assert.AreSame(empty, empty.Negate());
        }

        [Test]
        public void WithUnitQuantity_ReturnsNewPosition_WithQuantityEqualToUnitQuantity()
        {
            var unit = _position.WithUnitQuantity();
            Assert.AreEqual(_position.UnitQuantity, unit.Quantity);
            Assert.AreEqual(_position.Symbol, unit.Symbol);
            Assert.AreEqual(_position.UnitQuantity, unit.UnitQuantity);
        }

        [Test]
        public void WithUnitQuantity_ReturnsSamePosition_WhenQuantityEqualsUnitQuantity()
        {
            var unit = _position.WithUnitQuantity();
            Assert.AreSame(unit, unit.WithUnitQuantity());
        }

        [Test]
        public void ForGroupQuantity_ReturnsNewPosition_WithQuantityEqualToUnitQuantityTimesGroupQuantity()
        {
            var doubled = _position.ForGroupQuantity(_position.GetImpliedGroupQuantity());
            Assert.AreEqual(2 * _position.Quantity, doubled.Quantity);
            Assert.AreEqual(_position.Symbol, doubled.Symbol);
            Assert.AreEqual(_position.UnitQuantity, doubled.UnitQuantity);
        }

        [Test]
        public void ForGroupQuantity_ReturnsSamePosition_WhenImpliedGroupQuantityEqualsRequestedGroupQuantity()
        {
            Assert.AreSame(_position, _position.ForGroupQuantity(_position.GetImpliedGroupQuantity()));
        }

        [Test]
        public void GetImpliedGroupQuantity_Returns_PositionQuantityDividedByUnitQuantity()
        {
            Assert.AreEqual(4, _position.GetImpliedGroupQuantity());
        }
    }
}
