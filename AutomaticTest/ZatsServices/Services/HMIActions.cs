// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using Zats.Services.SampleApplication;
using ZWave.BasicApplication.Devices;
using ZWave.Enums;
using ZWave.TextApplication;

namespace Zats.Services
{
    /// <summary>
    /// INS14278 How to Use Certified Apps in Z-Wave 700
    /// </summary>
    public class HMIActions
    {
        private DeviceManager _deviceManager;
        internal HMIActions(DeviceManager deviceManager)
        {
            _deviceManager = deviceManager;
        }
        public bool UseCLI { get; set; }

        private ISampleApplicationProviderBase GetSampleCommandsProvider(Alias deviceNumber)
        {
            if (UseCLI == true && _deviceManager.IsZW080x(deviceNumber))
            {
                return GetCLICommandsProvider(deviceNumber);
            }
            else
            {
                return GetIOCommandProvider(deviceNumber); // Provide backward compatibility with manipulator boards (using button action)
            }
        }

        private ISampleApplicationProviderBase GetIOCommandProvider(Alias deviceNumber)
        {
            var deviceHostClient = _deviceManager.GetDeviceHostClient(deviceNumber);
            if (deviceHostClient?.PClient != null && deviceHostClient.PClient is ITestInterfaceDevice) // see DeviceManager.FillDeviceHostClient
            {
                switch (deviceHostClient.Library)
                {
                    case Libraries.DoorLockKeyPad:
                        return new SampleApplication.SiliconLabs.IOProviders.DoorLockKeyPad(deviceHostClient);
                    case Libraries.LEDBulb:
                        return new SampleApplication.SiliconLabs.IOProviders.LEDBulb(deviceHostClient);
                    case Libraries.PowerStrip:
                        return new SampleApplication.SiliconLabs.IOProviders.PowerStrip(deviceHostClient);
                    case Libraries.SensorPIR:
                        return new SampleApplication.SiliconLabs.IOProviders.SensorPIR(deviceHostClient);
                    case Libraries.SwitchOnOff:
                        return new SampleApplication.SiliconLabs.IOProviders.SwitchOnOff(deviceHostClient);
                    case Libraries.WallController:
                        return new SampleApplication.SiliconLabs.IOProviders.WallController(deviceHostClient);
                    case Libraries.MultilevelSensor:
                        return new SampleApplication.SiliconLabs.IOProviders.MultilevelSensor(deviceHostClient);
                    default:
                        return null;
                }
            }
            return null;
        }

        private ISampleApplicationProviderBase GetCLICommandsProvider(Alias deviceNumber)
        {
            var deviceHostClient = _deviceManager.GetDeviceHostClient(deviceNumber);
            var adminClient = _deviceManager.SilinkManager.GetSilinkAdminDevice(deviceNumber);
            if (deviceHostClient?.SClient != null && deviceHostClient.SClient is TextDevice device)
            {
                //see SilinkManager.Init for SClient 
                switch (deviceHostClient.Library)
                {
                    case Libraries.DoorLockKeyPad:
                        //return new SampleApplication.SiliconLabs.CLIProviders.DoorLockKeyPad(device);
                        return new SampleApplication.SiliconLabs.SilinkProviders.DoorLockKeyPad(adminClient);
                    case Libraries.LEDBulb:
                        return new SampleApplication.SiliconLabs.CLIProviders.LEDBulb(device);
                    case Libraries.PowerStrip:
                        return new SampleApplication.SiliconLabs.CLIProviders.PowerStrip(device);
                    case Libraries.SensorPIR:
                        //return new SampleApplication.SiliconLabs.CLIProviders.SensorPIR(device);
                        return new SampleApplication.SiliconLabs.SilinkProviders.SensorPIR(adminClient);
                    case Libraries.SwitchOnOff:
                        return new SampleApplication.SiliconLabs.CLIProviders.SwitchOnOff(device);
                    case Libraries.WallController:
                        return new SampleApplication.SiliconLabs.CLIProviders.WallController(device);
                    case Libraries.MultilevelSensor:
                        //return new SampleApplication.SiliconLabs.CLIProviders.MultilevelSensor(device);
                        return new SampleApplication.SiliconLabs.SilinkProviders.MultilevelSensor(adminClient);
                    default:
                        return null;
                }
            }
            return null;
        }

        /// <summary>
        /// Starts learn mode
        /// </summary>
        /// <param name="deviceNumber"></param>
        public void Sample_LearnMode(Alias deviceNumber)
        {
            GetSampleCommandsProvider(deviceNumber).LearnMode();
        }

        /// <summary>
        /// Device Hw-reset
        /// </summary>
        /// <param name="deviceNumber"></param>
        public void Sample_Reset(Alias deviceNumber)
        {
            GetSampleCommandsProvider(deviceNumber).Reset();
        }

        /// <summary>
        /// Button down sets the outside door handle mode active/inactive
        /// </summary>
        public void DoorLookKeypad_ToggleDoorHandle(Alias deviceNumber)
        {
            var doorLookKeypad = (IDoorLookKeypadProvider)GetSampleCommandsProvider(deviceNumber);
            doorLookKeypad.ToggleDoorHandle();
        }

        /// <summary>
        /// Button down sets the outside door handle mode active/inactive
        /// </summary>
        public void DoorLookKeypad_ToggleDoorHandle(Alias deviceNumber, byte duration)
        {
            var doorLookKeypad = (IDoorLookKeypadProvider)GetSampleCommandsProvider(deviceNumber);
            doorLookKeypad.ToggleDoorHandle(duration);
        }

        /// <summary>
        /// Button down sets hard coded User Code '1234' on KeyPad
        /// </summary>
        public void DoorLookKeypad_ToggleKeyPad(Alias deviceNumber)
        {
            var doorLookKeypad = (IDoorLookKeypadProvider)GetSampleCommandsProvider(deviceNumber);
            doorLookKeypad.ToggleKeyPad();
        }

        /// <summary>
        /// Sends Battery Report
        /// </summary>
        public void DoorLookKeypad_BatteryReport(Alias deviceNumber)
        {
            var deviceHostClient = _deviceManager.GetDeviceHostClient(deviceNumber);
            if (deviceHostClient.HasJName)
            {
                var doorLookKeypad = (IDoorLookKeypadProvider)GetSampleCommandsProvider(deviceNumber);
                doorLookKeypad.BatteryReport();
            }
            else
            {
                //Thread.Sleep(1000);//DL should go to sleep and save last sequence number
                _deviceManager.ResetSample(deviceHostClient);
            }

        }

        /// <summary>
        /// Gets LED Latch open/closed status
        /// </summary>
        /// <returns>Latch state</returns>
        public bool DoorLookKeypad_IsLatchOpen(Alias deviceNumber)
        {
            var doorLookKeypad = (IDoorLookKeypadProvider)GetSampleCommandsProvider(deviceNumber);
            var ret = doorLookKeypad.IsLatchOpen();
            return ret ?? false;
        }

        /// <summary>
        /// Gets LED Bolt locked/unlocked status
        /// </summary>
        /// <returns>Bolt state</returns>
        public bool? DoorLookKeypad_IsBoltLocked(Alias deviceNumber)
        {
            var doorLookKeypad = (IDoorLookKeypadProvider)GetSampleCommandsProvider(deviceNumber);
            var ret = doorLookKeypad.IsBoltLocked();
            return ret ?? false;
        }

        /// <summary>
        /// Dimming on/off endpoint 2
        /// </summary>
        /// <param name="duration">Dimming duration</param>
        public void PowerStrip_DimHold(Alias deviceNumber, int duration)
        {
            var sampPowerStrip = (IPowerStripProvider)GetSampleCommandsProvider(deviceNumber);
            sampPowerStrip.DimHold(duration);
        }

        /// <summary>
        /// Switch on/off endpoint 2
        /// </summary>
        public void PowerStrip_DimOnOff(Alias deviceNumber)
        {
            var sampPowerStrip = (IPowerStripProvider)GetSampleCommandsProvider(deviceNumber);
            sampPowerStrip.DimOnOff();
        }

        /// <summary>
        /// Toggles the transmission of an “Overload detected” notification. One notification will be transmitted: 700s series - every 30 second, 500s series - every 3rd second.
        /// </summary>
        public void PowerStrip_ToggleNotify(Alias deviceNumber)
        {
            var sampPowerStrip = (IPowerStripProvider)GetSampleCommandsProvider(deviceNumber);
            sampPowerStrip.ToggleNotify();
        }

        /// <summary>
        /// Switch on/off endpoint 1
        /// </summary>
        public void PowerStrip_OnOff(Alias deviceNumber)
        {
            var sampPowerStrip = (IPowerStripProvider)GetSampleCommandsProvider(deviceNumber);
            sampPowerStrip.OnOff();
        }

        /// <summary>
        /// Gets LED status of Power Strip endpoint 1
        /// </summary>
        /// <returns></returns>
        public bool? PowerStrip_IsStatusOn(Alias deviceNumber)
        {
            var doorLookKeypad = (IPowerStripProvider)GetSampleCommandsProvider(deviceNumber);
            var ret = doorLookKeypad.IsStatusOn();
            return ret ?? false;
        }

        /// <summary>
        /// Gets LED status of Power Strip endpoint 2. Is supported for 500s series only.
        /// </summary>
        /// <returns></returns>
        public bool? PowerStrip_IsDimStatusOn(Alias deviceNumber)
        {
            var doorLookKeypad = (IPowerStripProvider)GetSampleCommandsProvider(deviceNumber);
            var ret = doorLookKeypad.IsDimStatusOn();
            return ret ?? false;
        }

        /// <summary>
        /// Simulates a “Motion detected”.
        /// </summary>
        public void SensorPir_ActivatePirEvent(Alias deviceNumber)
        {
            var sampSensorPir = (ISensorPirProvider)GetSampleCommandsProvider(deviceNumber);
            sampSensorPir.ActivatePirEvent();
        }

        /// <summary>
        /// Sends Battery Report (only if device is not sleeping) 
        /// </summary>
        public void SensorPir_BatteryReport(Alias deviceNumber)
        {
            var sampSensorPir = (ISensorPirProvider)GetSampleCommandsProvider(deviceNumber);
            sampSensorPir.BatteryReport();
        }

        /// <summary>
        /// Toggle On/Off indicating LED
        /// </summary>
        public void SwitchOnOff_OnOff(Alias deviceNumber)
        {
            var switchOnOff = (ISwitchOnOffProvider)GetSampleCommandsProvider(deviceNumber);
            switchOnOff.OnOff();
        }

        /// <summary>
        /// Scene corresponding Key 01 Key Attribute “Key Held down”
        /// </summary>
        public void SwitchOnOff_HoldKey01(Alias deviceNumber, byte durationMs = 3)
        {
            var switchOnOff = (ISwitchOnOffProvider)GetSampleCommandsProvider(deviceNumber);
            switchOnOff.HoldKey01(durationMs);
        }

        /// <summary>
        /// Gets LED On/Off status
        /// </summary>
        /// <returns>Is On status</returns>
        public bool? SwitchOnOff_IsStatusOn(Alias deviceNumber)
        {
            var sampSwitchOnOff = (ISwitchOnOffProvider)GetSampleCommandsProvider(deviceNumber);
            return sampSwitchOnOff.IsStatusOn();
        }

        /// <summary>
        /// Lifeline with Central Scene Key Attribute “Key Pressed 1 Time”
        /// Trigger association group 2: Basic Set 
        /// Trigger association group 3: Multilevel Switch Set
        /// </summary>
        public void WallController_Key01(Alias deviceNumber)
        {
            var sampWallController = (IWallControllerProvider)GetSampleCommandsProvider(deviceNumber);
            sampWallController.Key01();
        }

        /// <summary>
        /// Scene corresponding Key 01 Key Attribute “Key Held down”
        /// Trigger association group 2: Basic Set 
        /// Trigger association group 3: Multilevel Switch Set
        /// </summary>
        public void WallController_Key01(Alias deviceNumber, byte duration)
        {
            var sampWallController = (IWallControllerProvider)GetSampleCommandsProvider(deviceNumber);
            sampWallController.HoldKey01(duration);
        }

        /// <summary>
        /// Lifeline with Central Scene Key Attribute “Key Pressed 1 Time”
        /// Trigger association group 4: Basic Set 
        /// Trigger association group 5: Multilevel Switch Set
        /// </summary>
        public void WallController_Key02(Alias deviceNumber)
        {
            var sampWallController = (IWallControllerProvider)GetSampleCommandsProvider(deviceNumber);
            sampWallController.Key02();
        }
        /// <summary>
        /// Scene corresponding Key 02 Key Attribute “Key Held down”
        /// Trigger association group 2: Basic Set 
        /// Trigger association group 3: Multilevel Switch Set
        /// </summary>
        public void WallController_Key02(Alias deviceNumber, byte duration)
        {
            var sampWallController = (IWallControllerProvider)GetSampleCommandsProvider(deviceNumber);
            sampWallController.HoldKey02(duration);
        }

        /// <summary>
        /// Lifeline with Central Scene Key Attribute “Key Pressed 1 Time”
        /// Trigger association group 6: Basic Set 
        /// Trigger association group 7: Multilevel Switch Set
        /// </summary>
        public void WallController_Key03(Alias deviceNumber)
        {
            var sampWallController = (IWallControllerProvider)GetSampleCommandsProvider(deviceNumber);
            sampWallController.Key03();
        }

        /// <summary>
        /// Scene corresponding Key 03 Key Attribute “Key Held down”
        /// Trigger association group 2: Basic Set 
        /// Trigger association group 3: Multilevel Switch Set
        /// </summary>
        public void WallController_Key03(Alias deviceNumber, byte duration)
        {
            var sampWallController = (IWallControllerProvider)GetSampleCommandsProvider(deviceNumber);
            sampWallController.HoldKey03(duration);
        }

        /// <summary>
        /// Trigger a movement detection (simulated by button BTN2).
        /// Hold for at least 1 second and release
        /// </summary>
        public void MultilevelSensor_ActivateEvent(Alias deviceNumber)
        {
            var sampMultilevelSensor = (IMultilevelSensorProvider)GetSampleCommandsProvider(deviceNumber);
            sampMultilevelSensor.ActivateEvent();
        }

        /// <summary>
        /// Sends Battery Report (only if the device is not sleeping)
        /// </summary>
        public void MultilevelSensor_BatteryReport(Alias deviceNumber)
        {
            var sampMultilevelSensor = (IMultilevelSensorProvider)GetSampleCommandsProvider(deviceNumber);
            sampMultilevelSensor.BatteryReport();
        }

        /// <summary>
        /// Checks LED1 is blinked during 30sec on learn mode active
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <returns>Is LED1 Blinked</returns>
        public bool IsLearnModeActive(Alias deviceNumber)
        {
            var sample = GetSampleCommandsProvider(deviceNumber) as SampleApplication.SiliconLabs.IOProviders.SampleApplicationIOBase;
            if (sample == null) throw new NotImplementedException("CLI is learn mode active");

            return sample.IOHandler.IsLearnModeActive();
        }
    }
}
