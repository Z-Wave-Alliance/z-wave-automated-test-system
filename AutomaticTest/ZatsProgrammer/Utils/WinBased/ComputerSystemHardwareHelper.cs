// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;
using System.Management;
using System.Reflection;
using System.Collections;

namespace ZWave.Framework
{
    public class ComputerSystemHardwareHelper
    {
        #region Static Methods
        static ManagementEventWatcher eventWatcher = null;

        public static void InitEventWatcher()
        {
            if (eventWatcher != null)
                CloseAndDisposeEventWatcher();

            //this watcher will check situations when devices added or removed
            eventWatcher = new ManagementEventWatcher(@"SELECT * FROM Win32_DeviceChangeEvent");
            try
            {
                eventWatcher.Start();
                eventWatcher.EventArrived += new EventArrivedEventHandler(eventWatcher_EventArrived);
            }
            catch (Exception ex)
            {
                Tools._writeDebugDiagnosticMessage(string.Format("InitEventWatcher Error: {0}", ex.Message));
                CloseAndDisposeEventWatcher();
            }
        }

        private static void eventWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            Tools._writeDebugDiagnosticMessage("============================");
            foreach (PropertyData pData in e.NewEvent.Properties)
            {
                if (pData.Value != null)
                {
                    Tools._writeDebugDiagnosticMessage(pData.Name.ToString() + " - " + pData.Value.ToString());
                }
                else
                {
                    Tools._writeDebugDiagnosticMessage(pData.Name.ToString() + " - null");
                }
            }
            Tools._writeDebugDiagnosticMessage("============================");
        }

        private static void CloseAndDisposeEventWatcher()
        {
            if (eventWatcher == null)
                return;

            try
            {
                eventWatcher.Stop();
                eventWatcher.Dispose();
                eventWatcher = null;
            }
            catch (Exception ex)
            {
                Tools._writeDebugDiagnosticMessage(string.Format("CloseAndDisposeEventWatcher Error {0} {1}", ex.Message, ex.StackTrace));

                if (eventWatcher != null)
                    eventWatcher.Dispose();
            }
        }

        public static List<Win32SerialPortClass> GetWin32SerialPortClassDevices()
        {
            List<Win32SerialPortClass> result = new List<Win32SerialPortClass>();
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_SerialPort"))
            {
                foreach (ManagementObject managementObject in searcher.Get())
                {
                    Win32SerialPortClass portInfo = new Win32SerialPortClass();
                    MapObject2Win32SerialPortClass(managementObject, portInfo);
                    if (!portInfo.Caption.Contains("LPT"))
                    {
                        result.Add(portInfo);
                    }
                }
            }
            return result;
        }

        public static Win32SerialPortClass GetWin32SerialPortClassDevice(string deviceId)
        {
            List<Win32SerialPortClass> list = GetWin32SerialPortClassDevices("DeviceID = \"" + deviceId + "\"");
            if (list.Count == 1)
            {
                return list[0];
            }
            return null;
        }
        private static List<Win32SerialPortClass> GetWin32SerialPortClassDevices(string condition)
        {
            List<Win32SerialPortClass> result = new List<Win32SerialPortClass>();
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_SerialPort where " + condition))
            {
                foreach (ManagementObject managementObject in searcher.Get())
                {
                    Win32SerialPortClass portInfo = new Win32SerialPortClass();
                    MapObject2Win32SerialPortClass(managementObject, portInfo);
                    if (!portInfo.Caption.Contains("LPT"))
                    {
                        result.Add(portInfo);
                    }
                }
            }
            return result;
        }

        // Method to prepare the WMI query connection options.
        private static ConnectionOptions PrepareOptions()
        {
            ConnectionOptions options = new ConnectionOptions();
            options.Impersonation = ImpersonationLevel.Impersonate;
            options.Authentication = AuthenticationLevel.Default;
            options.EnablePrivileges = true;
            return options;
        }

        // Method to prepare WMI query management scope.
        private static ManagementScope PrepareScope(string machineName, ConnectionOptions options, string path)
        {
            ManagementScope scope = new ManagementScope();
            scope.Path = new ManagementPath(@"\\" + machineName + path);
            scope.Options = options;
            scope.Connect();
            return scope;
        }

        // Method to retrieve the list of all COM ports.
        private static List<Win32PnPEntityClass> FindComPorts()
        {
            List<Win32PnPEntityClass> portInfoList = new List<Win32PnPEntityClass>();
            ConnectionOptions options = PrepareOptions();
            ManagementScope scope = PrepareScope(Environment.MachineName, options, @"\root\CIMV2");

            // Prepare the query and searcher objects.
            ObjectQuery objectQuery = new ObjectQuery("SELECT * FROM Win32_PnPEntity WHERE ClassGuid = '{4D36E978-E325-11CE-BFC1-08002BE10318}' and Service <> 'Parport'");
            ManagementObjectSearcher portSearcher = new ManagementObjectSearcher(scope, objectQuery);

            using (portSearcher)
            {
                string caption = null;
                // Invoke the searcher and search through each management object for a COM port.
                foreach (ManagementObject currentObject in portSearcher.Get())
                {
                    if (currentObject != null)
                    {
                        object currentObjectCaption = currentObject["Caption"];
                        if (currentObjectCaption != null)
                        {
                            caption = currentObjectCaption.ToString();
                            if (caption.Contains("(COM"))
                            {
                                Win32PnPEntityClass portInfo = new Win32PnPEntityClass();

                                MapObject2Win32PnPEntityClass(currentObject, portInfo);
                                
                                portInfoList.Add(portInfo);
                            }
                        }
                    }
                }
            }
            return portInfoList;
        }

        private static List<Win32PnPEntityClass> GetWin32PnPEntityClassSerialPortDevices()
        {
            List<Win32PnPEntityClass> result = new List<Win32PnPEntityClass>();
            serialPortNames = new List<string>(Layers.Transport.SerialPortProvider.GetPortNames());
            return FindComPorts();
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_PnPEntity where ClassGuid = '{4D36E978-E325-11CE-BFC1-08002BE10318}' and Service <> 'Parport' "))
            {
                foreach (ManagementObject managementObject in searcher.Get())
                {
                    Win32PnPEntityClass portInfo = new Win32PnPEntityClass();

                    MapObject2Win32PnPEntityClass(managementObject, portInfo);
                    if (portInfo.Caption != null && !portInfo.Caption.Contains("LPT"))
                    {
                        result.Add(portInfo);
                    }
                    else
                    {
                    }
                }
            }
            //foreach (string sPortName in serialPortNames)
            //{
            //    bool isCreated = false;
            //    foreach (Win32PnPEntityClass pInfo in result)
            //    {
            //        if (pInfo.DeviceID == sPortName)
            //        {
            //            isCreated = true;
            //            break;
            //        }  
            //    }
            //    if (!isCreated)
            //    {
            //        Win32PnPEntityClass _pInfo = new Win32PnPEntityClass();
            //        _pInfo.DeviceID = sPortName;
            //        _pInfo.Caption = sPortName;

            //        result.Add(_pInfo);
            //    }
            //}
            return result;
        }

        public static Win32PnPEntityClass GetWin32PnPEntityClassSerialPortDevice(string deviceId)
        {
            Win32PnPEntityClass result = null;
            List<Win32PnPEntityClass> list = GetWin32PnPEntityClassSerialPortDevices();
            foreach (Win32PnPEntityClass device in list)
            {
                if (device.DeviceID.ToUpper() == deviceId.ToUpper())
                {
                    result = device;
                    break;
                }
            }
            return result;
        }

        private static List<Win32PnPEntityClass> GetWin32PnPEntityClassSerialPortDevices(string condition)
        {
            List<Win32PnPEntityClass> result = new List<Win32PnPEntityClass>();

            if (!condition.Trim().ToUpper().StartsWith("AND"))
            {
                condition = " and " + condition;
            }
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_PnPEntity where ClassGuid = '{4D36E978-E325-11CE-BFC1-08002BE10318}' and Service <> 'Parport' " + condition))
            {
                foreach (ManagementObject managementObject in searcher.Get())
                {
                    Win32PnPEntityClass portInfo = new Win32PnPEntityClass();
                    MapObject2Win32PnPEntityClass(managementObject, portInfo);
                    if (!portInfo.Caption.Contains("LPT"))
                    {
                        result.Add(portInfo);
                    }
                }
            }
            return result;
        }

        private static List<Win32PnPEntityClass> GetWin32PnPEntityClassDevices()
        {
            List<Win32PnPEntityClass> result = new List<Win32PnPEntityClass>();
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_PnPEntity"))
            {
                foreach (ManagementObject managementObject in searcher.Get())
                {
                    Win32PnPEntityClass portInfo = new Win32PnPEntityClass();
                    MapObject2Win32PnPEntityClass(managementObject, portInfo);
                    if (!portInfo.Caption.Contains("LPT"))
                    {
                        result.Add(portInfo);
                    }
                }
            }
            return result;
        }

        private static Win32PnPEntityClass GetWin32PnPEntityClassDevice(string deviceId)
        {
            List<Win32PnPEntityClass> list = GetWin32PnPEntityClassDevices("DeviceID = \"" + deviceId + "\"");
            if (list.Count == 1)
            {
                return list[0];
            }
            return null;
        }

        private static List<Win32PnPEntityClass> GetWin32PnPEntityClassDevices(string condition)
        {
            List<Win32PnPEntityClass> result = new List<Win32PnPEntityClass>();
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_PnPEntity where " + condition))
            {
                foreach (ManagementObject managementObject in searcher.Get())
                {
                    Win32PnPEntityClass portInfo = new Win32PnPEntityClass();
                    MapObject2Win32PnPEntityClass(managementObject, portInfo);
                    if (!portInfo.Caption.Contains("LPT"))
                    {
                        result.Add(portInfo);
                    }
                }
            }
            return result;
        }

        #endregion

        #region Private Methods

        private static List<string> serialPortNames = new List<string>();

        private static void MapObject2BaseWin32DeviceClass(ManagementObject managementObject, BaseWin32DeviceClass device)
        {
            try
            {
                device.HardwareID = "";// managementObject.Properties["HardwareID"].Value;
                object hIdValue = FindPropertyValue(managementObject.Properties, "HardwareID");
                if (hIdValue != null && hIdValue is string[])
                {
                    string[] hardwareID = (string[])hIdValue;
                    if (hardwareID.Length >= 1)
                    {
                        if (hardwareID[0].ToUpper().Contains("REV_"))
                        {
                            device.HardwareID = hardwareID[0].ToUpper().Substring(hardwareID[0].ToUpper().IndexOf("REV_") + 4);
                        }
                    }
                }
            }
            catch
            {

            }

            if (managementObject.Properties["Availability"] != null && managementObject.Properties["Availability"].Value != null)
                device.Availability = (ushort?)managementObject.Properties["Availability"].Value;

            if (managementObject.Properties["Caption"] != null && managementObject.Properties["Caption"].Value != null)
            {
                device.Caption = (string)managementObject.Properties["Caption"].Value;
            }

            if (managementObject.Properties["ConfigManagerErrorCode"] != null && managementObject.Properties["ConfigManagerErrorCode"].Value != null)
                device.ConfigManagerErrorCode = (uint?)managementObject.Properties["ConfigManagerErrorCode"].Value;

            if (managementObject.Properties["ConfigManagerUserConfig"] != null && managementObject.Properties["ConfigManagerUserConfig"].Value != null)
                device.ConfigManagerUserConfig = (bool?)managementObject.Properties["ConfigManagerUserConfig"].Value;

            if (managementObject.Properties["CreationClassName"] != null && managementObject.Properties["CreationClassName"].Value != null)
                device.CreationClassName = (string)managementObject.Properties["CreationClassName"].Value;

            if (managementObject.Properties["Description"] != null && managementObject.Properties["Description"].Value != null)
                device.Description = (string)managementObject.Properties["Description"].Value;

            if (managementObject.Properties["DeviceID"] != null && managementObject.Properties["DeviceID"].Value != null)
                device.DeviceID = (string)managementObject.Properties["DeviceID"].Value;

            if (managementObject.Properties["Name"] != null && managementObject.Properties["Name"].Value != null)
                device.Name = (string)managementObject.Properties["Name"].Value;

            if (managementObject.Properties["PNPDeviceID"] != null && managementObject.Properties["PNPDeviceID"].Value != null)
                device.PNPDeviceID = (string)managementObject.Properties["PNPDeviceID"].Value;

            if (managementObject.Properties["PowerManagementCapabilities"] != null && managementObject.Properties["PowerManagementCapabilities"].Value != null)
                device.PowerManagementCapabilities = (UInt16[])managementObject.Properties["PowerManagementCapabilities"].Value;

            if (managementObject.Properties["PowerManagementSupported"] != null && managementObject.Properties["PowerManagementSupported"].Value != null)
                device.PowerManagementSupported = (bool?)managementObject.Properties["PowerManagementSupported"].Value;

            if (managementObject.Properties["Status"] != null && managementObject.Properties["Status"].Value != null)
                device.Status = (string)managementObject.Properties["Status"].Value;

            if (managementObject.Properties["StatusInfo"] != null && managementObject.Properties["StatusInfo"].Value != null)
                device.StatusInfo = (ushort?)managementObject.Properties["StatusInfo"].Value;

            if (managementObject.Properties["SystemCreationClassName"] != null && managementObject.Properties["SystemCreationClassName"].Value != null)
                device.SystemCreationClassName = (string)managementObject.Properties["SystemCreationClassName"].Value;

            if (managementObject.Properties["SystemName"] != null && managementObject.Properties["SystemName"].Value != null)
                device.SystemName = (string)managementObject.Properties["SystemName"].Value;

            if (string.IsNullOrEmpty(device.HardwareID) && device.DeviceID.ToUpper().Contains("Vid_0658&Pid_0280".ToUpper()))
            {
                try
                {
                    object hId = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Enum\USB\Vid_0658&Pid_0280\5&25039244&0&1", "HardwareID", null);
                    if (hId != null && hId is string[])
                    {
                        string[] hardwareID = (string[])hId;
                        if (hardwareID.Length >= 1)
                        {
                            if (hardwareID[0].ToUpper().Contains("REV_"))
                            {
                                device.HardwareID = hardwareID[0].ToUpper().Substring(hardwareID[0].ToUpper().IndexOf("REV_") + 4);
                            }
                        }

                    }
                }
                catch
                {

                }
            }
        }

        private static object FindPropertyValue(PropertyDataCollection propertyDataCollection, string propertyName)
        {
            object result = null;
            foreach (PropertyData pData in propertyDataCollection)
            {
                if (pData.Name.ToUpper() == propertyName.ToUpper())
                {
                    result = pData.Value;
                    break;
                }
            }
            return result;
        }

        private static void MapObject2Win32SerialPortClass(ManagementObject managementObject, Win32SerialPortClass portInfo)
        {
            MapObject2BaseWin32DeviceClass(managementObject, portInfo);
            if (managementObject.Properties["Binary"] != null && managementObject.Properties["Binary"].Value != null)
                portInfo.Binary = (bool?)managementObject.Properties["Binary"].Value;

            if (managementObject.Properties["MaxBaudRate"] != null && managementObject.Properties["MaxBaudRate"].Value != null)
                portInfo.MaxBaudRate = (uint?)managementObject.Properties["MaxBaudRate"].Value;

            if (managementObject.Properties["MaximumInputBufferSize"] != null && managementObject.Properties["MaximumInputBufferSize"].Value != null)
                portInfo.MaximumInputBufferSize = (uint?)managementObject.Properties["MaximumInputBufferSize"].Value;

            if (managementObject.Properties["MaximumOutputBufferSize"] != null && managementObject.Properties["MaximumOutputBufferSize"].Value != null)
                portInfo.MaximumOutputBufferSize = (uint?)managementObject.Properties["MaximumOutputBufferSize"].Value;

            if (managementObject.Properties["OSAutoDiscovered"] != null && managementObject.Properties["OSAutoDiscovered"].Value != null)
                portInfo.OSAutoDiscovered = (bool?)managementObject.Properties["OSAutoDiscovered"].Value;

            if (managementObject.Properties["ProviderType"] != null && managementObject.Properties["ProviderType"].Value != null)
                portInfo.ProviderType = (string)managementObject.Properties["ProviderType"].Value;

            if (managementObject.Properties["SettableBaudRate"] != null && managementObject.Properties["SettableBaudRate"].Value != null)
                portInfo.SettableBaudRate = (bool?)managementObject.Properties["SettableBaudRate"].Value;

            if (managementObject.Properties["SettableDataBits"] != null && managementObject.Properties["SettableDataBits"].Value != null)
                portInfo.SettableDataBits = (bool?)managementObject.Properties["SettableDataBits"].Value;

            if (managementObject.Properties["SettableFlowControl"] != null && managementObject.Properties["SettableFlowControl"].Value != null)
                portInfo.SettableFlowControl = (bool?)managementObject.Properties["SettableFlowControl"].Value;

            if (managementObject.Properties["SettableParity"] != null && managementObject.Properties["SettableParity"].Value != null)
                portInfo.SettableParity = (bool?)managementObject.Properties["SettableParity"].Value;

            if (managementObject.Properties["SettableParityCheck"] != null && managementObject.Properties["SettableParityCheck"].Value != null)
                portInfo.SettableParityCheck = (bool?)managementObject.Properties["SettableParityCheck"].Value;

            if (managementObject.Properties["SettableRLSD"] != null && managementObject.Properties["SettableRLSD"].Value != null)
                portInfo.SettableRLSD = (bool?)managementObject.Properties["SettableRLSD"].Value;

            if (managementObject.Properties["SettableStopBits"] != null && managementObject.Properties["SettableStopBits"].Value != null)
                portInfo.SettableStopBits = (bool?)managementObject.Properties["SettableStopBits"].Value;

            if (managementObject.Properties["Supports16BitMode"] != null && managementObject.Properties["Supports16BitMode"].Value != null)
                portInfo.Supports16BitMode = (bool?)managementObject.Properties["Supports16BitMode"].Value;

            if (managementObject.Properties["SupportsDTRDSR"] != null && managementObject.Properties["SupportsDTRDSR"].Value != null)
                portInfo.SupportsDTRDSR = (bool?)managementObject.Properties["SupportsDTRDSR"].Value;

            if (managementObject.Properties["SupportsElapsedTimeouts"] != null && managementObject.Properties["SupportsElapsedTimeouts"].Value != null)
                portInfo.SupportsElapsedTimeouts = (bool?)managementObject.Properties["SupportsElapsedTimeouts"].Value;

            if (managementObject.Properties["SupportsIntTimeouts"] != null && managementObject.Properties["SupportsIntTimeouts"].Value != null)
                portInfo.SupportsIntTimeouts = (bool?)managementObject.Properties["SupportsIntTimeouts"].Value;

            if (managementObject.Properties["SupportsParityCheck"] != null && managementObject.Properties["SupportsParityCheck"].Value != null)
                portInfo.SupportsParityCheck = (bool?)managementObject.Properties["SupportsParityCheck"].Value;

            if (managementObject.Properties["SupportsRLSD"] != null && managementObject.Properties["SupportsRLSD"].Value != null)
                portInfo.SupportsRLSD = (bool?)managementObject.Properties["SupportsRLSD"].Value;

            if (managementObject.Properties["SupportsRTSCTS"] != null && managementObject.Properties["SupportsRTSCTS"].Value != null)
                portInfo.SupportsRTSCTS = (bool?)managementObject.Properties["SupportsRTSCTS"].Value;

            if (managementObject.Properties["SupportsSpecialCharacters"] != null && managementObject.Properties["SupportsSpecialCharacters"].Value != null)
                portInfo.SupportsSpecialCharacters = (bool?)managementObject.Properties["SupportsSpecialCharacters"].Value;

            if (managementObject.Properties["SupportsXOnXOff"] != null && managementObject.Properties["SupportsXOnXOff"].Value != null)
                portInfo.SupportsXOnXOff = (bool?)managementObject.Properties["SupportsXOnXOff"].Value;

            if (managementObject.Properties["SupportsXOnXOffSet"] != null && managementObject.Properties["SupportsXOnXOffSet"].Value != null)
                portInfo.SupportsXOnXOffSet = (bool?)managementObject.Properties["SupportsXOnXOffSet"].Value;

        }
        private static void MapObject2Win32PnPEntityClass(ManagementObject managementObject, Win32PnPEntityClass portInfo)
        {
            MapObject2BaseWin32DeviceClass(managementObject, portInfo);
            if (managementObject.Properties["ClassGuid"] != null && managementObject.Properties["ClassGuid"].Value != null)
                portInfo.ClassGuid = (string)managementObject.Properties["ClassGuid"].Value;
            if (managementObject.Properties["ErrorCleared"] != null && managementObject.Properties["ErrorCleared"].Value != null)
                portInfo.ErrorCleared = (bool?)managementObject.Properties["ErrorCleared"].Value;
            if (managementObject.Properties["ErrorDescription"] != null && managementObject.Properties["ErrorDescription"].Value != null)
                portInfo.ErrorDescription = (string)managementObject.Properties["ErrorDescription"].Value;
            if (managementObject.Properties["InstallDate"] != null && managementObject.Properties["InstallDate"].Value != null)
                portInfo.InstallDate = (DateTime?)managementObject.Properties["InstallDate"].Value;
            if (managementObject.Properties["LastErrorCode"] != null && managementObject.Properties["LastErrorCode"].Value != null)
                portInfo.LastErrorCode = (int?)managementObject.Properties["LastErrorCode"].Value;
            if (managementObject.Properties["Manufacturer"] != null && managementObject.Properties["Manufacturer"].Value != null)
                portInfo.Manufacturer = (string)managementObject.Properties["Manufacturer"].Value;
            if (managementObject.Properties["Service"] != null && managementObject.Properties["Service"].Value != null)
                portInfo.Service = (string)managementObject.Properties["Service"].Value;

            if (!string.IsNullOrEmpty(portInfo.Caption))
            {
                foreach (string s in serialPortNames)
                {
                    string validComPortName = "COM";
                    foreach (char c in s)
                    {
                        if (char.IsDigit(c))
                        {
                            validComPortName += c;
                        }
                    }
                    if (portInfo.Caption.Contains("(" + validComPortName + ")"))
                    {
                        portInfo.DeviceID = validComPortName;
                    }
                }
            }
        }
    
        #endregion
    }
}

