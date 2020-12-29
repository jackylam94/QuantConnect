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

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Contains the information returned by <see cref="IPositionGroupBuyingPowerModel.GetMaximumPositionGroupOrderQuantityForTargetBuyingPower"/>
    /// and  <see cref="IPositionGroupBuyingPowerModel.GetMaximumPositionGroupOrderQuantityForDeltaBuyingPower"/>
    /// </summary>
    public class GetMaximumPositionGroupOrderQuantityResult
    {
        /// <summary>
        /// Returns the maximum quantity for the order
        /// </summary>
        public decimal Quantity { get; }

        /// <summary>
        /// Returns the reason for which the maximum order quantity is zero
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// Returns true if the zero order quantity is an error condition and will be shown to the user.
        /// </summary>
        public bool IsError { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetMaximumPositionGroupOrderQuantityResult"/> class
        /// </summary>
        /// <param name="quantity">Returns the maximum quantity for the order</param>
        /// <param name="reason">The reason for which the maximum order quantity is zero</param>
        /// <param name="isError">True if the zero order quantity is an error condition</param>
        public GetMaximumPositionGroupOrderQuantityResult(decimal quantity, string reason, bool isError)
        {
            Reason = reason;
            IsError = isError;
            Quantity = quantity;
        }

        /// <summary>
        /// Creates a new <see cref="GetMaximumPositionGroupOrderQuantityResult"/> with zero quantity and an error message.
        /// </summary>
        public static GetMaximumPositionGroupOrderQuantityResult Error(string reason)
        {
            return new GetMaximumPositionGroupOrderQuantityResult(0, reason, true);
        }

        /// <summary>
        /// Creates a new <see cref="GetMaximumPositionGroupOrderQuantityResult"/> with zero quantity and no message.
        /// </summary>
        public static GetMaximumPositionGroupOrderQuantityResult Zero()
        {
            return new GetMaximumPositionGroupOrderQuantityResult(0, string.Empty, false);
        }

        /// <summary>
        /// Creates a new <see cref="GetMaximumPositionGroupOrderQuantityResult"/> with zero quantity and an info message.
        /// </summary>
        public static GetMaximumPositionGroupOrderQuantityResult Zero(string reason)
        {
            return new GetMaximumPositionGroupOrderQuantityResult(0, reason, false);
        }

        /// <summary>
        /// Creates a new <see cref="GetMaximumPositionGroupOrderQuantityResult"/> for the specified quantity and no message.
        /// </summary>
        public static GetMaximumPositionGroupOrderQuantityResult MaximumQuantity(decimal quantity)
        {
            return new GetMaximumPositionGroupOrderQuantityResult(quantity, string.Empty, false);
        }
    }
}
