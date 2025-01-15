// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Text;
using ZWave.Framework;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.Win32.SafeHandles;
using System.Threading;
using System.IO.Ports;

namespace ZWave.SerialPortTransport
{
    public class UsbPort : Win32Usb
    {
        public const string ZW400USB_VENDOR_ID = "0405";
        public const string ZW400USB_PRODUCT_ID = "0100";
        public const int BUFFER_SIZE = 4026;
        private readonly ManualResetEvent manualEvent = new ManualResetEvent(false);

        public event EventHandler<UsbDataReceivedEventArgs> DataReceived;

        public static Guid ZW400USB_GUID = new Guid(0xb5c66d0b, 0xf385, 0x481e, 0xb6, 0x12, 0xf8, 0x9e, 0xb9, 0xe1, 0xa4, 0x1b);

        private string m_DevicePath;
        public string DevicePath
        {
            get { return m_DevicePath; }
            set { m_DevicePath = value; }
        }
        private bool m_IsOpen;
        public bool IsOpen
        {
            get { return m_IsOpen; }
            set { m_IsOpen = value; }
        }

        private FileStream m_File;
        private SafeFileHandle m_Handle;
        public SafeFileHandle Handle
        {
            get { return m_Handle; }
        }

        public UsbPort()
        {

        }

        public UsbPort(string devicePath)
        {
            this.DevicePath = devicePath + "\\USBPORT1";
        }

        public void Open()
        {
            if (!string.IsNullOrEmpty(this.DevicePath))
            {
                if (m_File == null)
                {
                    m_Handle = CreateFile(
                        this.DevicePath,
                        GENERIC_READ | GENERIC_WRITE,
                        FILE_SHARE_WRITE | FILE_SHARE_READ,
                        IntPtr.Zero,
                        OPEN_EXISTING,
                        FILE_FLAG_OVERLAPPED,
                        IntPtr.Zero);


                    if (m_Handle != null && !m_Handle.IsInvalid)
                    {
                        try
                        {
                            m_File = new FileStream(m_Handle, FileAccess.Read | FileAccess.Write, BUFFER_SIZE, true);
                            //System.Diagnostics.Debug.WriteLine("BeginAsyncRead in Open");
                            BeginAsyncRead();
                            this.IsOpen = true;
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                    }
                }
            }
        }
        private bool startClose = false;
        private bool canClose = true;
        private void BeginAsyncRead()
        {
            if (startClose == true) return;
            if (m_File != null && m_File.CanRead)
            {
                //System.Diagnostics.Debug.WriteLine("BeginAsyncRead");
                byte[] data = new byte[BUFFER_SIZE];
                canClose = false;
                m_File.BeginRead(data, 0, BUFFER_SIZE, new AsyncCallback(ReadCompleted), data);
            }
        }
        protected void ReadCompleted(IAsyncResult iResult)
        {
            byte[] data = (byte[])iResult.AsyncState;
            try
            {
                if (m_File != null)
                {
                    try
                    {
                        //System.Diagnostics.Debug.WriteLine("ReadCompleted");
                        int byteCount = m_File.EndRead(iResult);
                        if (byteCount > 0)
                        {
                            HandleDataReceived(byteCount, data);
                        }
                        canClose = true;
                        Thread.Sleep(100);
                        BeginAsyncRead();
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
        protected void HandleDataReceived(int byteCount, byte[] data)
        {
            if (DataReceived != null)
            {
                byte[] buffer = new byte[byteCount];
                Array.Copy(data, buffer, byteCount);
                DataReceived(this, new UsbDataReceivedEventArgs(buffer));
            }

        }
        public void Write(byte[] buffer)
        {
            try
            {
                if (m_File != null && m_File.CanWrite)
                {
                    //System.Diagnostics.Debug.WriteLine("Write");
                    m_File.BeginWrite(buffer, 0, buffer.Length,
                        new AsyncCallback(WriteCompleted),
                        buffer);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private void WriteCompleted(IAsyncResult asyncResult)
        {
            if (m_File != null)
            {
                //System.Diagnostics.Debug.WriteLine("WriteCompleted");
                byte[] buffer = (byte[])asyncResult.AsyncState;
                m_File.EndWrite(asyncResult);
                m_File.Flush();
            }
        }

        public static string[] GetPortNames()
        {
            List<string> result = new List<string>();
            string strPath = string.Empty;
            string strSearch = string.Format("vid_{0:x4}&pid_{1:x4}", ZW400USB_VENDOR_ID, ZW400USB_PRODUCT_ID); // first, build the path search string

            IntPtr hInfoSet = SetupDiGetClassDevs(ref ZW400USB_GUID,
                null,
                IntPtr.Zero,
                DIGCF_DEVICEINTERFACE | DIGCF_PRESENT);

            try
            {
                DeviceInterfaceData oInterface = new DeviceInterfaceData();
                oInterface.Size = Marshal.SizeOf(oInterface);
                int nIndex = 0;
                while (SetupDiEnumDeviceInterfaces(hInfoSet, 0, ref ZW400USB_GUID, (uint)nIndex, ref oInterface))
                {
                    string strDevicePath = GetDevicePath(hInfoSet, ref oInterface);
                    if (strDevicePath.IndexOf(strSearch) >= 0)
                    {
                        result.Add(strDevicePath);
                    }
                    nIndex++;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(hInfoSet);
            }

            return result.ToArray();
        }
        private static string GetDevicePath(IntPtr hInfoSet, ref DeviceInterfaceData oInterface)
        {
            uint nRequiredSize = 0;
            // Get the device interface details
            if (!SetupDiGetDeviceInterfaceDetail(hInfoSet, ref oInterface, IntPtr.Zero, 0, ref nRequiredSize, IntPtr.Zero))
            {
                DeviceInterfaceDetailData oDetail = new DeviceInterfaceDetailData();
                oDetail.Size = 5;	// hardcoded 
                if (SetupDiGetDeviceInterfaceDetail(hInfoSet, ref oInterface, ref oDetail, nRequiredSize, ref nRequiredSize, IntPtr.Zero))
                {
                    return oDetail.DevicePath;
                }
            }
            return null;
        }


        public void Close()
        {
            startClose = true;
            while (canClose)
            {
                if (m_File != null)
                {
                    m_File.Flush();
                    Thread.Sleep(1000);
                    m_File.Close();
                    m_File = null;
                }
                if (m_Handle != null && !m_Handle.IsInvalid)
                {
                    m_Handle.Close();
                }
                canClose = false;
            }
            //System.Diagnostics.Debug.WriteLine("Closed");
            startClose = false;
        }
    }
    public class UsbDataReceivedEventArgs : EventArgs
    {
        public UsbDataReceivedEventArgs(byte[] data)
        {
            Data = data;
        }
        private byte[] mData;

        public byte[] Data
        {
            get { return mData; }
            set { mData = value; }
        }
    }
}
