// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using ZWave.Exceptions;
using ZWave.Events;
using ZWave.Framework;
using System.Threading;

namespace ZWave.SerialPortTransport
{
    class TransportLayer : ITransportLayer
    {
        //SerialPort mPort = null;
        CommPort mPort = null;
#if !NETCOREAPP
        UsbPort mUsbPort = null;
#endif

        #region ITransportLayer Members
        public event DataReceivedEventHandler DataReceived;
        /// <summary>
        /// Close()
        /// </summary>
        public void Close()
        {
            if (mPort != null)
            {
                Tools._writeDebugDiagnosticMessage("", true, true, 1);
                if (mPort.IsOpen)
                {
                    mPort.Close();
                    mPort.DataReceived -= mPort_DataReceived;
                }
                
                mPort = null;
            }
#if !NETCOREAPP
            if (mUsbPort != null)
            {
                Tools._writeDebugDiagnosticMessage("USB", true, true, 1);
                mUsbPort.Close();
                mUsbPort = null;

            }
#endif
        }

        /// <summary>
        /// Gets the device names.
        /// </summary>
        /// <returns></returns>
        public IList<string> GetDeviceNames()
        {
            List<string> ports = new List<string>(Layers.Transport.SerialPortProvider.GetPortNames());
#if !NETCOREAPP
            ports.AddRange(UsbPort.GetPortNames());
#endif
            ports.Sort();
            return ports;
        }

        /// <summary>
        /// Opens the specified device name.
        /// </summary>
        /// <param name="deviceName">Name of the device.</param>
        public void Open(string deviceName)
        {
#if !NETCOREAPP
            if (deviceName.StartsWith(@"\\?\usb#vid_"))
            {
                if (mUsbPort != null)
                {
                    //mUsbPort.Close();
                    //mUsbPort = null;
                }
                try
                {
                    Tools._writeDebugDiagnosticMessage("USB", true, true, 1);
                    if (mUsbPort == null)
                    {
                        mUsbPort = new UsbPort(deviceName);
                        mUsbPort.DataReceived += new EventHandler<UsbDataReceivedEventArgs>(mUsbPort_DataReceived);
                    }                    
                    mUsbPort.Open();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            else
            {
#endif
                if (mPort == null)
                {
                    Tools._writeDebugDiagnosticMessage("", true, true, 1);
                    //mPort = new SerialPort(deviceName, 115200, Parity.None, 8, StopBits.One);
                    //mPort.DataReceived += new SerialDataReceivedEventHandler(mPort_DataReceived);
                    mPort = new CommPort(deviceName);
                    mPort.DataReceived += mPort_DataReceived;
                    mPort.Open();
                }
                else if (mPort.PortName != deviceName)
                {
                    throw new TransportLayerException("Another port is already opened");
                }
                else if (mPort.IsOpen)
                {
                    throw new TransportLayerException("Port is already opened");
                }
                else if (!mPort.IsOpen)
                {
                    mPort.Open();
                }
#if !NETCOREAPP
            }
#endif
        }

#if !NETCOREAPP
        void mUsbPort_DataReceived(object sender, UsbDataReceivedEventArgs e)
        {
            if (mUsbPort != null && mUsbPort.IsOpen)
            {
                try
                {
                    Tools._writeDebugDiagnosticMessage($"{Thread.CurrentThread.ManagedThreadId:000}|{mUsbPort.DevicePath} << {Tools.GetHex(e.Data)}", true, false);
                    DataReceived?.Invoke(new ZWave.Events.DataReceivedEventArgs(e.Data));
                }
                catch (Exception ex)
                {
                    Tools._writeDebugDiagnosticMessage(ex.Message, true, true);
                }
            }
        }
#endif

        void mPort_DataReceived()
        //void mPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (mPort != null && mPort.IsOpen)
            {
                try
                {
                    int bytesToRead = mPort.BytesToRead;
                    byte[] data = new byte[bytesToRead];
                    mPort.Read(data, bytesToRead);
                    Tools._writeDebugDiagnosticMessage($"{Thread.CurrentThread.ManagedThreadId:000}|{mPort.PortName} << {Tools.GetHex(data)}", true, false);
                    DataReceived?.Invoke(new DataReceivedEventArgs(data));
                }
                catch (Exception ex)
                {
                    Tools._writeDebugDiagnosticMessage(ex.Message, true, true);
                }
            }
        }


        /// <summary>
        /// Write to buffer
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public int Write(byte[] buffer)
        {
            int ret = buffer.Length;
            if (mPort != null)
            {
                if (mPort.IsOpen)
                {
                    try
                    {
                        Tools._writeDebugDiagnosticMessage($"{Thread.CurrentThread.ManagedThreadId:000}|{mPort.PortName} >> {Tools.GetHex(buffer)}", true, false);
                        mPort.Write(buffer, ret);
                    }
                    catch (Exception ex)
                    {
                        Tools._writeDebugDiagnosticMessage(ex.Message, true, true);
                    }
                }
                else
                {
                    Tools._writeDebugDiagnosticMessage($"{Thread.CurrentThread.ManagedThreadId:000}|{mPort.PortName} >> [CLOSED] {Tools.GetHex(buffer)}", true, false);
                }
            }
#if !NETCOREAPP
            else if (mUsbPort != null && mUsbPort.IsOpen)
            {
                try
                {
                    Tools._writeDebugDiagnosticMessage($"{Thread.CurrentThread.ManagedThreadId:000}|{mUsbPort.DevicePath} >> {Tools.GetHex(buffer)}", true, false);
                    mUsbPort.Write(buffer);
                }
                catch (Exception ex)
                {
                    Tools._writeDebugDiagnosticMessage(ex.Message, true, true);
                }
            }
#endif
            else
                throw new TransportLayerException("port is not opened");
            return ret;

        }

        /// <summary>
        /// Is the port already Open ?
        /// </summary>
        /// <returns></returns>
        public bool IsOpen()
        {
            bool ret = false;
            if (mPort != null)
            {
                ret = mPort.IsOpen;
            }
#if !NETCOREAPP
            else if (mUsbPort != null)
            {
                ret = mUsbPort.IsOpen;
            }
#endif
            else
            {
                throw new TransportLayerException("port is not created");
            }
            return ret;
        }

#endregion
    }
}
