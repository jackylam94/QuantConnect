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

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Defines a base class for <see cref="PositionCollection"/> event arguments
    /// </summary>
    public abstract class PositionCollectionEventArgs : EventArgs
    {
        /// <summary>
        /// The <see cref="PositionCollection"/> raising the event
        /// </summary>
        public PositionCollection Collection { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionCollectionEventArgs"/> class
        /// </summary>
        protected PositionCollectionEventArgs(PositionCollection collection)
        {
            Collection = collection;
        }
    }

    /// <summary>
    /// Event arguments for the <see cref="PositionCollection.SecurityPositionAdded"/> event
    /// </summary>
    public class SecurityPositionAddedEventArgs : PositionCollectionEventArgs
    {
        /// <summary>
        /// The <see cref="SecurityPosition"/> that was added
        /// </summary>
        public SecurityPosition SecurityPosition { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityPositionAddedEventArgs"/> class
        /// </summary>
        public SecurityPositionAddedEventArgs(PositionCollection collection, SecurityPosition securityPosition)
            : base(collection)
        {
            SecurityPosition = securityPosition;
        }
    }

    /// <summary>
    /// Event arguments for the <see cref="PositionCollection.PositionAdded"/> event
    /// </summary>
    public class PositionAddedEventArgs : PositionCollectionEventArgs
    {
        /// <summary>
        /// The <see cref="IPosition"/> that was added
        /// </summary>
        public IPosition Position { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionAddedEventArgs"/> class
        /// </summary>
        public PositionAddedEventArgs(PositionCollection collection, IPosition position)
            : base(collection)
        {
            Position = position;
        }
    }

    /// <summary>
    /// Event arguments for the <see cref="PositionCollection.SecurityPositionRemoved"/> event
    /// </summary>
    public class SecurityPositionRemovedEventArgs : PositionCollectionEventArgs
    {
        /// <summary>
        /// The <see cref="SecurityPosition"/> that was removed
        /// </summary>
        public SecurityPosition SecurityPosition { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityPositionRemovedEventArgs"/> class
        /// </summary>
        public SecurityPositionRemovedEventArgs(PositionCollection collection, SecurityPosition securityPosition)
            : base(collection)
        {
            SecurityPosition = securityPosition;
        }
    }

    /// <summary>
    /// Event arguments for the <see cref="PositionCollection.PositionRemoved"/> event
    /// </summary>
    public class PositionRemovedEventArgs : PositionCollectionEventArgs
    {
        /// <summary>
        /// The <see cref="IPosition"/> that was removed
        /// </summary>
        public IPosition Position { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionRemovedEventArgs"/> class
        /// </summary>
        public PositionRemovedEventArgs(PositionCollection collection, IPosition position)
            : base(collection)
        {
            Position = position;
        }
    }
}
