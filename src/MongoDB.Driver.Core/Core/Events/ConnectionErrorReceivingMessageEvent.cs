﻿/* Copyright 2013-2014 MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.WireProtocol.Messages;

namespace MongoDB.Driver.Core.Events
{
    public struct ConnectionErrorReceivingMessageEvent
    {
        private readonly ConnectionId _connectionId;
        private readonly Exception _exception;
        private readonly int _responseTo;

        public ConnectionErrorReceivingMessageEvent(ConnectionId connectionId, int responseTo, Exception exception)
        {
            _connectionId = connectionId;
            _responseTo = responseTo;
            _exception = exception;
        }

        public ConnectionId ConnectionId
        {
            get { return _connectionId; }
        }

        public Exception Exception
        {
            get { return _exception; }
        }

        public int ResponseTo
        {
            get { return _responseTo; }
        }
    }
}
