// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Linq;
using System.Threading.Tasks;
using ZWave.Layers.Transport;
using System.Collections.Generic;

namespace ZWave.SerialPortTransport
{
    public class CommPort
    {
        private SerialPortProvider _serialPortProvider;
        private Task _workerTask;
        private Queue<byte[]> _dataQueue = new Queue<byte[]>();
        private readonly object _locker = new object();

        public int BytesToRead
        {
            get
            {
                return _dataQueue.Sum(data => data.Length);
            }
        }

        
        private readonly string _portName =  string.Empty;
        public CommPort(string portName)
        {
            _portName = portName;
        }

        public event Action DataReceived;

        public bool IsOpen => _serialPortProvider != null && _serialPortProvider.IsOpen;

        public string PortName => _serialPortProvider != null ? _serialPortProvider.PortName : string.Empty;

        public void Open()
        {
            lock (_locker)
            {
                if (!IsOpen)
                {
                    _serialPortProvider = new SerialPortProvider();
                    if (_serialPortProvider.Open(_portName, 115200, PInvokeParity.None, 8, PInvokeStopBits.One))
                    {
                        _serialPortProvider.ReadExisting();
                        _dataQueue.Clear();
                        _workerTask = Task.Factory.StartNew(() => DoWork());
                    }
                }
            }
        }

        public void Close()
        {
            lock (_locker)
            {
                if (IsOpen)
                {
                    _serialPortProvider.Close();
                    _workerTask.Wait();

                    _serialPortProvider = null;
                    _workerTask = null;
                }
            }
        }

        private void DoWork()
        {
            while (IsOpen)
            {
                var buff = new byte[512];
                var bytesToRead = _serialPortProvider.Read(buff, buff.Length);
                if (bytesToRead > 0)
                {
                    lock (_locker)
                    {
                        _dataQueue.Enqueue(buff.Take(bytesToRead).ToArray());
                    }
                    if (bytesToRead > 0)
                        DataReceived?.Invoke();
                }
                else
                    break;
            }
        }

        internal void Read(byte[] data, int bytesToRead)
        {
            lock (_locker)
            {
                int pos = 0;
                do
                {
                    var peek = _dataQueue.Peek();
                    if (bytesToRead >= peek.Length)
                    {
                        Array.Copy(_dataQueue.Dequeue(), 0, data, pos, peek.Length);
                        pos += peek.Length;
                        bytesToRead -= peek.Length;
                    }
                    else
                        break;
                }
                while (bytesToRead > 0 && _dataQueue.Count > 0);
            }
        }

        internal void Write(byte[] buffer, int count)
        {
            lock (_locker)
            {
                _serialPortProvider.Write(buffer, count);
            }
        }
    }
}
