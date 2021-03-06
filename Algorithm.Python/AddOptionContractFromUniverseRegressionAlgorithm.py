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
from datetime import *

### <summary>
### We add an option contract using 'QCAlgorithm.AddOptionContract' and place a trade, the underlying
### gets deselected from the universe selection but should still be present since we manually added the option contract.
### Later we call 'QCAlgorithm.RemoveOptionContract' and expect both option and underlying to be removed.
### </summary>
class AddOptionContractFromUniverseRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2014, 6, 5)
        self.SetEndDate(2014, 6, 9)

        self._expiration = datetime(2014, 6, 21)
        self._securityChanges = None
        self._option = None
        self._traded = False

        self._twx = Symbol.Create("TWX", SecurityType.Equity, Market.USA)
        self._aapl = Symbol.Create("AAPL", SecurityType.Equity, Market.USA)
        self.UniverseSettings.Resolution = Resolution.Minute
        self.UniverseSettings.DataNormalizationMode = DataNormalizationMode.Raw

        self.AddUniverse(self.Selector, self.Selector)

    def Selector(self, fundamental):
        if self.Time <= datetime(2014, 6, 5):
            return  [ self._twx ]
        return [ self._aapl ]

    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.

        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        if self._option != None and self.Securities[self._option].Price != 0 and not self._traded:
            self._traded = True;
            self.Buy(self._option, 1);

        if self.Time == datetime(2014, 6, 6, 14, 0, 0):
            # liquidate & remove the option
            self.RemoveOptionContract(self._option)

    def OnSecuritiesChanged(self, changes):
        # keep track of all removed and added securities
        if self._securityChanges == None:
            self._securityChanges = changes
        else:
            self._securityChanges.op_Addition(self._securityChanges, changes)

        if any(security.Symbol.SecurityType == SecurityType.Option for security in changes.AddedSecurities):
            return

        for addedSecurity in changes.AddedSecurities:
            options = self.OptionChainProvider.GetOptionContractList(addedSecurity.Symbol, self.Time)
            options = sorted(options, key=lambda x: x.ID.Symbol)

            option = next((option for option in options if option.ID.Date == self._expiration and option.ID.OptionRight == OptionRight.Call and option.ID.OptionStyle == OptionStyle.American), None)

            self.AddOptionContract(option)

            # just keep the first we got
            if self._option == None:
                self._option = option