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
using NUnit.Framework;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.Positions;

namespace QuantConnect.Tests.Common.Securities.Positions
{
    /// <summary>
    /// Provides an implementation of <see cref="IBuyingPowerModel"/> to be used in all <see cref="IBuyingPowerModel"/>
    /// unit tests that will also evaluate and verify results provided by the <see cref="SecurityPositionGroupBuyingPowerModel"/>.
    /// This provides a simple mechanism for parlaying all of the existing extensive buying power model test coverage to
    /// the <see cref="SecurityPositionGroupBuyingPowerModel"/> which MUST return the same results as the security's model.
    /// </summary>
    public class BuyingPowerModelComparator : IBuyingPowerModel
    {
        public IAlgorithm Algorithm { get; }
        public IBuyingPowerModel SecurityModel { get; }
        public IPositionGroupBuyingPowerModel PositionGroupModel { get; }

        public BuyingPowerModelComparator(
            IBuyingPowerModel securityModel,
            IPositionGroupBuyingPowerModel positionGroupModel,
            IAlgorithm algorithm
            )
        {
            Algorithm = algorithm;
            SecurityModel = securityModel;
            PositionGroupModel = positionGroupModel;
        }

        public decimal GetLeverage(Security security)
        {
            return SecurityModel.GetLeverage(security);
        }

        public void SetLeverage(Security security, decimal leverage)
        {
            SecurityModel.SetLeverage(security, leverage);
        }

        public MaintenanceMargin GetMaintenanceMargin(MaintenanceMarginParameters parameters)
        {
            var expected = SecurityModel.GetMaintenanceMargin(parameters);
            var actual = PositionGroupModel.GetMaintenanceMargin(new PositionGroupMaintenanceMarginParameters(
                Algorithm.Portfolio,
                Algorithm.Portfolio.Positions.GetDefaultPositionGroup(parameters.Security.Symbol),
                true
            ));

            Assert.AreEqual(expected.Value, actual.Value,
                $"{PositionGroupModel.GetType().Name}:{nameof(GetMaintenanceMargin)}"
            );

            return expected;
        }

        public InitialMargin GetInitialMarginRequirement(InitialMarginParameters parameters)
        {
            var expected = SecurityModel.GetInitialMarginRequirement(parameters);
            var actual = PositionGroupModel.GetInitialMarginRequirement(new PositionGroupInitialMarginParameters(
                Algorithm.Portfolio,
                Algorithm.Portfolio.Positions.GetDefaultPositionGroup(parameters.Security.Symbol)
            ));

            Assert.AreEqual(expected.Value, actual.Value,
                $"{PositionGroupModel.GetType().Name}:{nameof(GetInitialMarginRequirement)}"
            );

            return expected;
        }

        public decimal GetInitialMarginRequiredForOrder(InitialMarginRequiredForOrderParameters parameters)
        {
            var expected = SecurityModel.GetInitialMarginRequiredForOrder(parameters);
            var actual = PositionGroupModel.GetInitialMarginRequiredForOrder(new PositionGroupInitialMarginForOrderParameters(
                Algorithm.Portfolio,
                Algorithm.Portfolio.Positions.GetDefaultPositionGroup(parameters.Security.Symbol),
                parameters.Order
            ));

            Assert.AreEqual(expected.Value, actual.Value,
                $"{PositionGroupModel.GetType().Name}:{nameof(GetInitialMarginRequiredForOrder)}"
            );

            return expected;
        }

        InitialMargin IBuyingPowerModel.GetInitialMarginRequiredForOrder(InitialMarginRequiredForOrderParameters parameters)
        {
            return GetInitialMarginRequiredForOrder(parameters);
        }

        public HasSufficientBuyingPowerForOrderResult HasSufficientBuyingPowerForOrder(
            HasSufficientBuyingPowerForOrderParameters parameters
            )
        {
            var expected = SecurityModel.HasSufficientBuyingPowerForOrder(parameters);
            var actual = PositionGroupModel.HasSufficientBuyingPowerForOrder(
                new HasSufficientPositionGroupBuyingPowerForOrderParameters(
                    Algorithm.Portfolio,
                    Algorithm.Portfolio.Positions.CreatePositionGroup(parameters.Order),
                    parameters.Order
                )
            );

            Assert.AreEqual(expected.IsSufficient, actual.IsSufficient,
                $"{PositionGroupModel.GetType().Name}:{nameof(HasSufficientBuyingPowerForOrder)}: " +
                $"ExpectedReason: {expected.Reason}{Environment.NewLine}" +
                $"ActualReason: {actual.Reason}"
            );

            Assert.AreEqual(expected.Reason, actual.Reason,
                $"{PositionGroupModel.GetType().Name}:{nameof(HasSufficientBuyingPowerForOrder)}"
            );

            return expected;
        }

        public GetMaximumOrderQuantityResult GetMaximumOrderQuantityForTargetBuyingPower(
            GetMaximumOrderQuantityForTargetBuyingPowerParameters parameters
            )
        {
            var security = parameters.Security;
            var securities = parameters.Portfolio.Securities;
            var positionGroup = Algorithm.Portfolio.Positions.GetDefaultPositionGroup(security.Symbol);
            var expected = SecurityModel.GetMaximumOrderQuantityForTargetBuyingPower(parameters);
            var actual = PositionGroupModel.GetMaximumPositionGroupOrderQuantityForTargetBuyingPower(
                new GetMaximumPositionGroupOrderQuantityForTargetBuyingPowerParameters(
                    Algorithm.Portfolio,
                    positionGroup,
                    parameters.TargetBuyingPower,
                    parameters.SilenceNonErrorReasons
                )
            );

            Assert.AreEqual(expected.IsError, actual.IsError,
                $"{PositionGroupModel.GetType().Name}:{nameof(GetMaximumOrderQuantityForTargetBuyingPower)}: " +
                $"ExpectedQuantity: {expected.Quantity} ActualQuantity: {actual.Quantity} {Environment.NewLine}" +
                $"ExpectedReason: {expected.Reason}{Environment.NewLine}" +
                $"ActualReason: {actual.Reason}"
            );

            // we're not comparing group quantities, which is the number of position lots, but rather the implied
            // position quantities resulting from having that many lots.
            var resizedPositionGroup = positionGroup.WithQuantity(actual.Quantity);
            var position = resizedPositionGroup.GetPosition(security.Symbol);

            var bpmOrder = new MarketOrder(security.Symbol, expected.Quantity, securities.UtcTime);
            var pgbpmOrder = new MarketOrder(security.Symbol, position.Quantity, securities.UtcTime);

            var bpmOrderValue = bpmOrder.GetValue(security);
            var pgbpmOrderValue = pgbpmOrder.GetValue(security);

            var bpmOrderFees = security.FeeModel.GetOrderFee(new OrderFeeParameters(security, bpmOrder)).Value.Amount;
            var pgbpmOrderFees = security.FeeModel.GetOrderFee(new OrderFeeParameters(security, pgbpmOrder)).Value.Amount;

            var bpmMarginRequired = bpmOrderValue + bpmOrderFees;
            var pgbpmMarginRequired = pgbpmOrderValue + pgbpmOrderFees;

            Assert.AreEqual(expected.Quantity, position.Quantity,
                $"{PositionGroupModel.GetType().Name}:{nameof(GetMaximumOrderQuantityForTargetBuyingPower)}: " +
                $"ExpectedReason: {expected.Reason}{Environment.NewLine}" +
                $"ActualReason: {actual.Reason}"
            );

            Assert.AreEqual(expected.Reason, actual.Reason,
                $"{PositionGroupModel.GetType().Name}:{nameof(GetMaximumOrderQuantityForTargetBuyingPower)}: " +
                $"ExpectedReason: {expected.Reason}{Environment.NewLine}" +
                $"ActualReason: {actual.Reason}"
            );

            return expected;
        }

        public GetMaximumOrderQuantityResult GetMaximumOrderQuantityForDeltaBuyingPower(
            GetMaximumOrderQuantityForDeltaBuyingPowerParameters parameters
            )
        {
            var expected = SecurityModel.GetMaximumOrderQuantityForDeltaBuyingPower(parameters);
            var actual = PositionGroupModel.GetMaximumPositionGroupOrderQuantityForDeltaBuyingPower(
                new GetMaximumPositionGroupOrderQuantityForDeltaBuyingPowerParameters(
                    Algorithm.Portfolio,
                    Algorithm.Portfolio.Positions.GetDefaultPositionGroup(parameters.Security.Symbol),
                    parameters.DeltaBuyingPower,
                    parameters.SilenceNonErrorReasons
                )
            );

            Assert.AreEqual(expected.IsError, actual.IsError,
                $"{PositionGroupModel.GetType().Name}:{nameof(GetMaximumOrderQuantityForDeltaBuyingPower)}: " +
                $"ExpectedQuantity: {expected.Quantity} ActualQuantity: {actual.Quantity} {Environment.NewLine}" +
                $"ExpectedReason: {expected.Reason}{Environment.NewLine}" +
                $"ActualReason: {actual.Reason}"
            );

            Assert.AreEqual(expected.Quantity, actual.Quantity,
                $"{PositionGroupModel.GetType().Name}:{nameof(GetMaximumOrderQuantityForDeltaBuyingPower)}: " +
                $"ExpectedReason: {expected.Reason}{Environment.NewLine}" +
                $"ActualReason: {actual.Reason}"
            );

            Assert.AreEqual(expected.Reason, actual.Reason,
                $"{PositionGroupModel.GetType().Name}:{nameof(GetMaximumOrderQuantityForDeltaBuyingPower)}"
            );

            return expected;
        }

        public ReservedBuyingPowerForPosition GetReservedBuyingPowerForPosition(ReservedBuyingPowerForPositionParameters parameters)
        {
            var expected = SecurityModel.GetReservedBuyingPowerForPosition(parameters);
            var actual = PositionGroupModel.GetReservedBuyingPowerForPositionGroup(
                new ReservedBuyingPowerForPositionGroupParameters(
                    Algorithm.Portfolio,
                    Algorithm.Portfolio.Positions.GetDefaultPositionGroup(parameters.Security.Symbol)
                )
            );

            Assert.AreEqual(expected.AbsoluteUsedBuyingPower, actual.AbsoluteUsedBuyingPower,
                $"{PositionGroupModel.GetType().Name}:{nameof(GetReservedBuyingPowerForPosition)}"
            );

            return expected;
        }

        public BuyingPower GetBuyingPower(BuyingPowerParameters parameters)
        {
            var expected = SecurityModel.GetBuyingPower(parameters);
            var actual = PositionGroupModel.GetPositionGroupBuyingPower(
                new PositionGroupBuyingPowerParameters(
                    Algorithm.Portfolio,
                    Algorithm.Portfolio.Positions.GetDefaultPositionGroup(parameters.Security.Symbol),
                    parameters.Direction
                )
            );

            Assert.AreEqual(expected.Value, actual.Value,
                $"{PositionGroupModel.GetType().Name}:{nameof(GetBuyingPower)}"
            );

            return expected;
        }
    }
}
