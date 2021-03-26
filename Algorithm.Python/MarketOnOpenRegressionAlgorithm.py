# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
# Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

from clr import AddReference
AddReference("System")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Securities import *
from QuantConnect.Data.Market import *
from QuantConnect.Orders import *
from datetime import datetime

### <summary>
### Regression algorithm for Market On Open orders.
### </summary>
### <meta name="tag" content="trading and orders" />
### <meta name="tag" content="placing orders" />
class MarketOnOpenRegressionAlgorithm(QCAlgorithm):

    expectedFillPrices = [167.45, 165.82]

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2013, 10, 7)
        self.SetEndDate(2013, 10, 9)
        self.SetCash(100000)

        resolution = Resolution.Tick
        equity = self.AddEquity("SPY", resolution)
        symbol = equity.Symbol
        equity.SetDataNormalizationMode(DataNormalizationMode.Raw)

        self.Schedule.On(
                self.DateRules.EveryDay(symbol),
                self.TimeRules.Midnight if resolution == Resolution.Daily else self.TimeRules.At(15, 0),
                lambda: self.MarketOnOpenOrder(symbol, 100))

    def OnOrderEvent(self, orderEvent):
        if not orderEvent.Status == OrderStatus.Filled:
           return;

        expected = self.expectedFillPrices[orderEvent.OrderId - 1]
        if expected != orderEvent.FillPrice:
            raise Exception(f"Unexpected value for Fill Price of {orderEvent.FillPrice}. Expected: {expected}")
