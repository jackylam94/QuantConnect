using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Securities;
using QuantConnect.Securities.Positions;

namespace QuantConnect.Tests.Common.Securities.Positions
{
    [TestFixture]
    public class PositionCollectionTests
    {
        private PositionCollection _positions;
        private IReadOnlyDictionary<Symbol, Security> _securities;

        [SetUp]
        public void Setup()
        {
            _securities = new[]
            {
                CreateSecurity(Symbols.AAPL),
                CreateSecurity(Symbols.SPY),
                CreateSecurity(Symbols.IBM),
                CreateSecurity(Symbols.MSFT),
                CreateSecurity(Symbols.GOOG)
            }.ToDictionary(s => s.Symbol);

            _positions = PositionCollection.Create(_securities.Values);
        }

        [Test]
        public void Create_FromSecurities_InitializesWithSecurityPositionFromSecurityObject()
        {
            foreach (var position in _positions)
            {
                Assert.IsInstanceOf<SecurityPosition>(position);
                var securityPosition = (SecurityPosition) position;
                Assert.IsNotNull(securityPosition.DefaultGroup);
            }
        }

        [Test]
        public void SecurityPosition_Quantity_IsUpdatedBasedOnSecurityHoldings()
        {
            var aapl = _securities[Symbols.AAPL];

            var aaplPosition = _positions.GetSecurityPosition(Symbols.AAPL);
            Assert.AreEqual(0, aaplPosition.Quantity);

            aapl.Holdings.SetHoldings(200m, 100);
            Assert.AreEqual(100, aaplPosition.Quantity);
        }

        [Test]
        public void GetSecurityPosition_Throws_KeyNotFoundException_WhenSymbolIsNotInCollection()
        {
            var positions = PositionCollection.Create(Enumerable.Empty<Security>());
            Assert.Throws<KeyNotFoundException>(
                () => positions.GetSecurityPosition(Symbols.AAPL)
            );
        }

        [Test]
        public void GetSecurityPosition_Returns_SecurityPosition_ForRequestedSymbol()
        {
            var securityPosition = _positions.GetSecurityPosition(Symbols.AAPL);
            Assert.IsNotNull(securityPosition);
        }

        [Test]
        public void CreatePosition_Throws_InvalidOperationException_WhenSecurityHasInsufficientQuantityForAdditionalPosition()
        {
            var aapl = _securities[Symbols.AAPL];
            Assert.AreEqual(0, aapl.Holdings.Quantity);
            Assert.Throws<InvalidOperationException>(
                () => _positions.CreatePosition(Symbols.AAPL, 1)
            );
        }

        [Test]
        public void CreatePosition_DeductsQuantity_FromSecurityPosition_AfterPositionGroupAddedToSecurityPosition()
        {
            var aapl = _securities[Symbols.AAPL];
            aapl.Holdings.SetHoldings(200m, 100);
            var securityPosition = _positions.GetSecurityPosition(Symbols.AAPL);
            Assert.AreEqual(aapl.Holdings.Quantity, securityPosition.Quantity);

            var position = _positions.CreatePosition(Symbols.AAPL, 75);

            // until position is added we don't deduct the quantity
            Assert.AreEqual(75, position.Quantity);
            Assert.AreEqual(100, securityPosition.Quantity);

            securityPosition.AddGroup(PositionGroup.Create(position));

            Assert.AreEqual(75, position.Quantity);
            Assert.AreEqual(25, securityPosition.Quantity);
        }

        private static Security CreateSecurity(Symbol symbol)
        {
            return new Security(symbol, SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork), new Cash("USD", 0m, 1m), SymbolProperties.GetDefault("USD"),
                ErrorCurrencyConverter.Instance, new RegisteredSecurityDataTypesProvider(), new SecurityCache());
        }
    }
}