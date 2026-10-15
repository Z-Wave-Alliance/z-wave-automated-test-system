// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Zats.Services
{
    public class ReceivedDataCollection : IEnumerable<AchData>
    {
        public List<AchData> _receivedData = new List<AchData>();
        public IList<AchData> ReceivedData { get { return _receivedData; } }

        public int Count { get { return _receivedData.Count; } }

        private ReceivedDataCollection(IList<AchData> receivedData)
        {
            Append(receivedData);
        }

        private void Append(IList<AchData> data)
        {
            foreach (var val in data)
            {
                _receivedData.Add(val);
            }
        }

        private IEnumerator<AchData> _commandsEnumerator;
        public AchData FirstCommand()
        {
            if (_receivedData.Count > 0)
            {
                _commandsEnumerator = _receivedData.GetEnumerator();
                return NextCommand();
            }
            return null;
        }

        public AchData NextCommand()
        {
            if (_commandsEnumerator == null)
            {
                throw new InvalidOperationException("commands collection is empty");
            }
            if (_commandsEnumerator.MoveNext())
            {
                return _commandsEnumerator.Current;
            }
            throw new InvalidOperationException("no more commands in collection");
        }

        public byte[] FindCommand(byte[] cmd)
        {
            foreach (var recData in _receivedData)
            {
                if (recData.Command.Take(2).SequenceEqual(cmd.Take(2)))
                {
                    return recData.Command;
                }
            }
            return null;
        }

        public IEnumerator<AchData> GetEnumerator()
        {
            return _receivedData.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _receivedData.GetEnumerator();
        }

        public static ReceivedDataCollection FromListenResult(BasicResult.ListenData listenData)
        {
            return new ReceivedDataCollection(listenData.ReceivedData);
        }

        public static ReceivedDataCollection FromCombinedListenResults(BasicResult.ListenData listenData1, BasicResult.ListenData listenData2)
        {
            var ret = new ReceivedDataCollection(listenData1.ReceivedData);
            ret.Append(listenData2.ReceivedData);
            return ret;
        }
    }
}
