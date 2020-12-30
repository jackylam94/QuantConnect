using NUnit.Framework;
using QuantConnect.Securities;
using QuantConnect.Securities.Positions;

namespace QuantConnect.Tests.Common.Securities.Positions
{
    [TestFixture]
    public class SecurityPositionTests
    {
        private Security _security;
        private SecurityPosition _securityPosition;
        private SecurityPositionGroup _securityPositionGroup;

        [SetUp]
        public void Setup()
        {
            _security = CreateSecurity(Symbols.AAPL);
            _securityPosition = new SecurityPosition(_security, TODO);
            _securityPositionGroup = _securityPosition.DefaultGroup;
        }

        private static Security CreateSecurity(Symbol symbol)
        {
            return new Security(symbol, SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork), new Cash("USD", 0m, 1m), SymbolProperties.GetDefault("USD"),
                ErrorCurrencyConverter.Instance, new RegisteredSecurityDataTypesProvider(), new SecurityCache());
        }
    }
}
