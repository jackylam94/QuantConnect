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
using System.Linq;
using Moq;
using NUnit.Framework;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using QuantConnect.Securities.Positions;
using QuantConnect.Util;
using Option = QuantConnect.Tests.Common.Securities.Options.StrategyMatcher.Option;

namespace QuantConnect.Tests.Common.Securities.Positions
{
    public class PositionGroups
    {
        public readonly Security SPY;
        public readonly Security SPY_C100;
        public readonly IPositionGroup SPY_C100_CoveredCall;
        public readonly SecurityPositionGroup SPY_DefaultGroup;
        public readonly SecurityPositionGroup SPY_C100_DefaultGroup;

        public PositionGroups()
        {
            SPY = CreateSecurity(Symbols.SPY);
            SPY_C100 = CreateSecurity(Option.Call[Symbols.SPY, 100]);
            SPY_DefaultGroup = new SecurityPositionGroup(SPY);
            SPY_C100_DefaultGroup = new SecurityPositionGroup(SPY_C100);
            SPY_C100_CoveredCall = PositionGroup.Create(
                SecurityPositionGroupDescriptor.Instance,
                new Position(SPY.Symbol, 500, 100),
                new Position(SPY_C100.Symbol, -5, 1)
            );
        }

        private static Security CreateSecurity(Symbol symbol)
        {
            if (symbol.SecurityType == SecurityType.Equity)
            {
                return new QuantConnect.Securities.Equity.Equity(symbol,
                    SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                    new Cash("USD", 0m, 1m), SymbolProperties.GetDefault("USD"),
                    ErrorCurrencyConverter.Instance,
                    new RegisteredSecurityDataTypesProvider(), new SecurityCache()
                );
            }

            if (symbol.SecurityType == SecurityType.Option)
            {
                return new QuantConnect.Securities.Option.Option(symbol,
                    SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                    new Cash("USD", 0m, 1m), new OptionSymbolProperties(SymbolProperties.GetDefault("USD")),
                    ErrorCurrencyConverter.Instance,
                    new RegisteredSecurityDataTypesProvider(), new SecurityCache()
                );
            }

            throw new NotImplementedException($"Not implemented: {symbol.SecurityType}");
        }
    }

    [TestFixture]
    public class PositionGroupTests
    {
        private Security SPY;
        private Security SPY_C100;
        private IPositionGroup _coveredCall;
        private SecurityPositionGroup _spyDefaultGroup;
        private SecurityPositionGroup _spy_c100DefaultGroup;

        [SetUp]
        public void Setup()
        {
            SPY = CreateSecurity(Symbols.SPY);
            SPY_C100 = CreateSecurity(Option.Call[Symbols.SPY, 100]);

            _spyDefaultGroup = new SecurityPositionGroup(new SecurityPosition(SPY, TODO));
            _spy_c100DefaultGroup = new SecurityPositionGroup(new SecurityPosition(SPY_C100, TODO));

            // TODO : Update to use OptionStrategyPositionGroupDescriptor following options integration
            _coveredCall = PositionGroup.Create(
                SecurityPositionGroupDescriptor.Instance,
                new Position(SPY.Symbol, 500, 100),
                new Position(Option.Call[100], -5, -1)
            );
        }

        [Test]
        public void IsEmpty_ReturnsTrue_WhenPositionsHaveZeroQuantity()
        {
            Assert.IsTrue(_spyDefaultGroup.IsEmpty());
            Assert.IsTrue(_spy_c100DefaultGroup.IsEmpty());
        }

        [Test]
        public void IsEmpty_ReturnsFalse_WhenPositionsHaveQuantity()
        {
            Assert.IsFalse(_coveredCall.IsEmpty());
        }

        [Test]
        public void Empty_ReturnsNewEmptyGroup_WhenPositionsHaveQuantity()
        {
            var empty = _coveredCall.Empty();
            Assert.IsTrue(empty.IsEmpty());
            Assert.IsTrue(empty.All(p => PositionExtensions.IsEmpty(p)));
        }

        [Test]
        public void Empty_ReturnsSameGroup_WhenAllPositionsAreEmpty()
        {
            var empty = _coveredCall.Empty();
            Assert.IsTrue(empty.IsEmpty());
            Assert.AreSame(empty, empty.Empty());
        }

        [Test]
        public void Empty_CreatesNewEmptyPositionGroup()
        {
            var positions = new []{_spyDefaultGroup.Position};
            var empty = PositionGroup.Empty(
                PositionGroupKey.Create(SecurityPositionGroupDescriptor.Instance, positions)
            );

            Assert.IsTrue(empty.IsEmpty());
            Assert.IsTrue(empty.All(p => p.IsEmpty()));
        }

        [Test]
        public void Negate_CreatesNewPositionGroup_WithNegativeQuantities()
        {
            var negated = _coveredCall.Negate();
            foreach (var position in _coveredCall)
            {
                var negatedPosition = negated.GetPosition(position.Symbol);
                Assert.AreEqual(-position.Quantity, negatedPosition.Quantity);
                Assert.AreEqual(position.UnitQuantity, negatedPosition.UnitQuantity);
            }
        }

        [Test]
        public void Negate_ReturnsSameGroup_WhenQuantityIsZero()
        {
            Assert.AreSame(_spyDefaultGroup, _spyDefaultGroup.Negate());
        }

        [Test]
        [TestCase(1)]
        [TestCase(0)]
        [TestCase(-1)]
        public void GetPositionSide_ReturnsBasedOnTheSignOfGroupQuantity(decimal groupQuantity)
        {
            var side = (PositionSide) Math.Sign(groupQuantity);
            Assert.AreEqual(side, _coveredCall.WithQuantity(groupQuantity).GetPositionSide());
        }

        [Test]
        public void IsUnit_ReturnsTrue_WhenAllPositionQuantitiesEqualItsUnitQuantity()
        {
            var unit = PositionGroup.Create(_coveredCall.Descriptor,
                _coveredCall.ToArray(p => PositionExtensions.WithUnitQuantity(p))
            );

            Assert.IsTrue(unit.IsUnit());
        }

        [Test]
        public void IsUnit_ReturnsFalse_WhenPositionQuantitiesDoNotEqualItsUnitQuantity()
        {
            Assert.IsFalse(_coveredCall.IsUnit());
        }

        [Test]
        public void WithUnitQuantities_ReturnsPositionGroupUnit()
        {
            var unit = _coveredCall.WithUnitQuantities();
            Assert.IsTrue(unit.IsUnit());
        }

        [Test]
        public void WithUnitQuantities_ReturnsSameGroup_WhenGroupIsAlreadyUnit()
        {
            var unit = _coveredCall.WithUnitQuantities();
            Assert.AreSame(unit, unit.WithUnitQuantities());
        }

        [Test]
        public void WithQuantity_ReturnsNewGroup_WithPositionQuantity_EqualGroupQuantityTimesUnitQuantity()
        {
            var groupQuantity = 10m;
            var resized = _coveredCall.WithQuantity(groupQuantity);
            foreach (var position in resized)
            {
                Assert.AreEqual(groupQuantity * position.UnitQuantity, position.Quantity);
            }
        }

        [Test]
        public void WithQuantity_ReturnsSameGroup_WhenGroupQuantityAlreadyEqualsRequestedGroupQuantity()
        {
            Assert.AreSame(_coveredCall, _coveredCall.WithQuantity(_coveredCall.Quantity));
        }

        [Test]
        public void GetUserFriendlyName_Delegates_ToDescriptor()
        {
            const string userFriendlyName = "user-friendly-name";
            var descriptor = new Mock<IPositionGroupDescriptor>();
            descriptor.Setup(d => d.GetUserFriendlyName(It.IsAny<IPositionGroup>()))
                .Returns(userFriendlyName);

            var group = PositionGroup.Create(descriptor.Object, _spyDefaultGroup.Position);
            Assert.AreEqual(userFriendlyName, group.GetUserFriendlyName());
        }

        private static Security CreateSecurity(Symbol symbol)
        {
            return new Security(symbol, SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork), new Cash("USD", 0m, 1m), SymbolProperties.GetDefault("USD"),
                ErrorCurrencyConverter.Instance, new RegisteredSecurityDataTypesProvider(), new SecurityCache());
        }
    }
}
