// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using ZWave;
using Zats.Services.Constants;
using ZWave.BasicApplication.Devices;
using System.Linq;
using ZWave.Enums;
using System.Threading;
using System;

namespace Zats.Services
{
    /// <summary>
    /// INS14278 How to Use Certified Apps in Z-Wave 700
    /// </summary>
    public class ButtonActions
    {
        private DeviceManager _deviceManager;
        internal ButtonActions(DeviceManager deviceManager)
        {
            _deviceManager = deviceManager;
        }

        private SampleCommandsProvider GetSampleCommandsProvider(Alias deviceNumber)
        {
            var deviceHostClient = _deviceManager.GetDeviceHostClient(deviceNumber);
            if (deviceHostClient?.PClient != null && deviceHostClient.PClient is ITestInterfaceDevice)
            {
                ITestInterfaceDevice device = (ITestInterfaceDevice)deviceHostClient.PClient;
                bool is700 = deviceHostClient.HasJName;
                string chipSeries = deviceHostClient.Host.ChipSeries;
                switch (device.Library)
                {
                    case Libraries.DoorLockKeyPad:
                        return new DoorLookKeypadCommandsProvider(device, is700, deviceHostClient.DevKit, deviceHostClient.Host.ChipSeries);
                    case Libraries.LEDBulb:
                        return new LEDBulbCommandsProvider(device, is700, deviceHostClient.DevKit, deviceHostClient.Host.ChipSeries);
                    case Libraries.PowerStrip:
                        return new PowerStripCommandsProvider(device, is700, deviceHostClient.DevKit, deviceHostClient.Host.ChipSeries);
                    case Libraries.SensorPIR:
                        return new SensorPirCommandsProvider(device, is700, deviceHostClient.DevKit, deviceHostClient.Host.ChipSeries);
                    case Libraries.SwitchOnOff:
                        return new SwithOnOffCommandsProvider(device, is700, deviceHostClient.DevKit, deviceHostClient.Host.ChipSeries);
                    case Libraries.WallController:
                        return new WallControllerCommandsProvider(device, is700, deviceHostClient.DevKit, deviceHostClient.Host.ChipSeries);
                    case Libraries.MultilevelSensor:
                        return new MultilevelSensorCommandsProvider(device, is700, deviceHostClient.DevKit, deviceHostClient.Host.ChipSeries);
                    case Libraries.KeyFob:
                        return new KeyFobCommandsProvider(device, is700, deviceHostClient.DevKit, deviceHostClient.Host.ChipSeries);
                    default:
                        return null;
                }
            }
            else
            {
                throw new ServiceException($"device {deviceNumber} {deviceHostClient.Host.SName}.{deviceHostClient.Host.JName} doesn't have test interface connected");
            }
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
            var doorLookKeypad = (DoorLookKeypadCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            doorLookKeypad.ToggleDoorHandle();
        }

        /// <summary>
        /// Button down sets the outside door handle mode active/inactive
        /// </summary>
        public void DoorLookKeypad_ToggleDoorHandle(Alias deviceNumber, byte duration)
        {
            var doorLookKeypad = (DoorLookKeypadCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            doorLookKeypad.ToggleDoorHandle(duration);
        }

        /// <summary>
        /// Button down sets hard coded User Code '1234' on KeyPad
        /// </summary>
        public void DoorLookKeypad_ToggleKeyPad(Alias deviceNumber)
        {
            var doorLookKeypad = (DoorLookKeypadCommandsProvider)GetSampleCommandsProvider(deviceNumber);
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
                var doorLookKeypad = (DoorLookKeypadCommandsProvider)GetSampleCommandsProvider(deviceNumber);
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
            var doorLookKeypad = (DoorLookKeypadCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            var ret = doorLookKeypad.IsLatchOpen();
            return ret ?? false;
        }

        /// <summary>
        /// Gets LED Bolt locked/unlocked status
        /// </summary>
        /// <returns>Bolt state</returns>
        public bool? DoorLookKeypad_IsBoltLocked(Alias deviceNumber)
        {
            var doorLookKeypad = (DoorLookKeypadCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            var ret = doorLookKeypad.IsBoltLocked();
            return ret ?? false;
        }

        /// <summary>
        /// Dimming on/off endpoint 2
        /// </summary>
        /// <param name="duration">Dimming duration</param>
        public void PowerStrip_DimHold(Alias deviceNumber, int duration)
        {
            var sampPowerStrip = (PowerStripCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            sampPowerStrip.DimHold(duration);
        }

        /// <summary>
        /// Switch on/off endpoint 2
        /// </summary>
        public void PowerStrip_DimOnOff(Alias deviceNumber)
        {
            var sampPowerStrip = (PowerStripCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            sampPowerStrip.DimOnOff();
        }

        /// <summary>
        /// Toggles the transmission of an “Overload detected” notification. One notification will be transmitted: 700s series - every 30 second, 500s series - every 3rd second.
        /// </summary>
        public void PowerStrip_ToggleNotify(Alias deviceNumber)
        {
            var sampPowerStrip = (PowerStripCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            sampPowerStrip.ToggleNotify();
        }

        /// <summary>
        /// Switch on/off endpoint 1
        /// </summary>
        public void PowerStrip_OnOff(Alias deviceNumber)
        {
            var sampPowerStrip = (PowerStripCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            sampPowerStrip.OnOff();
        }

        /// <summary>
        /// Gets LED status of Power Strip endpoint 1
        /// </summary>
        /// <returns></returns>
        public bool? PowerStrip_IsStatusOn(Alias deviceNumber)
        {
            var doorLookKeypad = (PowerStripCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            var ret = doorLookKeypad.IsStatusOn();
            return ret ?? false;
        }

        /// <summary>
        /// Gets LED status of Power Strip endpoint 2. Is supported for 500s series only.
        /// </summary>
        /// <returns></returns>
        public bool? PowerStrip_IsDimStatusOn(Alias deviceNumber)
        {
            var doorLookKeypad = (PowerStripCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            var ret = doorLookKeypad.IsDimStatusOn();
            return ret ?? false;
        }

        /// <summary>
        /// Simulates a “Motion detected”.
        /// </summary>
        public void SensorPir_ActivatePirEvent(Alias deviceNumber)
        {
            var sampSensorPir = (SensorPirCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            sampSensorPir.ActivatePirEvent();
        }

        /// <summary>
        /// Sends Battery Report (only if device is not sleeping) 
        /// </summary>
        public void SensorPir_BatteryReport(Alias deviceNumber)
        {
            var sampSensorPir = (SensorPirCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            sampSensorPir.BatteryReport();
        }

        /// <summary>
        /// Toggle On/Off indicating LED
        /// </summary>
        public void SwithOnOff_OnOff(Alias deviceNumber)
        {
            var switchOnOff = (SwithOnOffCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            switchOnOff.OnOff();
        }

        /// <summary>
        /// Scene corresponding Key 01 Key Attribute “Key Held down”
        /// </summary>
        public void SwithOnOff_HoldKey01(Alias deviceNumber, byte durationMs = 3)
        {
            var switchOnOff = (SwithOnOffCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            switchOnOff.HoldKey01(durationMs);
        }

        /// <summary>
        /// Gets LED On/Off status
        /// </summary>
        /// <returns>Is On status</returns>
        public bool? SwithOnOff_IsStatusOn(Alias deviceNumber)
        {
            var sampSwithOnOff = (SwithOnOffCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            return sampSwithOnOff.IsStatusOn();
        }

        /// <summary>
        /// Lifeline with Central Scene Key Attribute “Key Pressed 1 Time”
        /// Trigger association group 2: Basic Set 
        /// Trigger association group 3: Multilevel Switch Set
        /// </summary>
        public void WallController_Key01(Alias deviceNumber)
        {
            var sampWallController = (WallControllerCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            sampWallController.Key01();
        }

        /// <summary>
        /// Scene corresponding Key 01 Key Attribute “Key Held down”
        /// Trigger association group 2: Basic Set 
        /// Trigger association group 3: Multilevel Switch Set
        /// </summary>
        public void WallController_Key01(Alias deviceNumber, byte duration)
        {
            var sampWallController = (WallControllerCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            sampWallController.HoldKey01(duration);
        }

        /// <summary>
        /// Lifeline with Central Scene Key Attribute “Key Pressed 1 Time”
        /// Trigger association group 4: Basic Set 
        /// Trigger association group 5: Multilevel Switch Set
        /// </summary>
        public void WallController_Key02(Alias deviceNumber)
        {
            var sampWallController = (WallControllerCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            sampWallController.Key02();
        }
        /// <summary>
        /// Scene corresponding Key 02 Key Attribute “Key Held down”
        /// Trigger association group 2: Basic Set 
        /// Trigger association group 3: Multilevel Switch Set
        /// </summary>
        public void WallController_Key02(Alias deviceNumber, byte duration)
        {
            var sampWallController = (WallControllerCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            sampWallController.HoldKey02(duration);
        }

        /// <summary>
        /// Lifeline with Central Scene Key Attribute “Key Pressed 1 Time”
        /// Trigger association group 6: Basic Set 
        /// Trigger association group 7: Multilevel Switch Set
        /// </summary>
        public void WallController_Key03(Alias deviceNumber)
        {
            var sampWallController = (WallControllerCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            sampWallController.Key03();
        }

        /// <summary>
        /// Scene corresponding Key 03 Key Attribute “Key Held down”
        /// Trigger association group 2: Basic Set 
        /// Trigger association group 3: Multilevel Switch Set
        /// </summary>
        public void WallController_Key03(Alias deviceNumber, byte duration)
        {
            var sampWallController = (WallControllerCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            sampWallController.HoldKey03(duration);
        }

        /// <summary>
        /// Trigger a movement detection (simulated by button BTN2).
        /// Hold for at least 1 second and release
        /// </summary>
        public void MultilevelSensor_ActivateEvent(Alias deviceNumber)
        {
            var sampMultilevelSensor = (MultilevelSensorCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            sampMultilevelSensor.ActivateEvent();
        }

        /// <summary>
        /// Sends Battery Report (only if the device is not sleeping)
        /// </summary>
        public void MultilevelSensor_BatteryReport(Alias deviceNumber)
        {
            var sampMultilevelSensor = (MultilevelSensorCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            sampMultilevelSensor.BatteryReport();
        }

        /// <summary>
        /// Activate Add Node on the Key Fob Application
        /// </summary>
        public void KeyFob_StartAddNode(Alias deviceNumber)
        {
            var commandsProvider = (KeyFobCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            commandsProvider.AddNode();
        }

        /// <summary>
        /// Activate Remove Node on the Key Fob Application
        /// </summary>
        public void KeyFob_StartRemoveNode(Alias deviceNumber)
        {
            var commandsProvider = (KeyFobCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            commandsProvider.RemoveNode();
        }

        /// <summary>
        /// Activate Add To Group on the Key Fob Application
        /// </summary>
        public void KeyFob_AddToGroup(Alias deviceNumber)
        {
            var commandsProvider = (KeyFobCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            commandsProvider.AddToGroup();
        }

        /// <summary>
        /// Send Basic Set On from the Key Fob Application to the group
        /// </summary>
        public void KeyFob_BasicSetOn(Alias deviceNumber)
        {
            var commandsProvider = (KeyFobCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            commandsProvider.BasicSetOn();
        }

        /// <summary>
        /// Send Basic Set Off from the Key Fob Application to the group
        /// </summary>
        public void KeyFob_BasicSetOff(Alias deviceNumber)
        {
            var commandsProvider = (KeyFobCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            commandsProvider.BasicSetOff();
        }

        /// <summary>
        /// Reset the Key Fob Application to Default device
        /// </summary>
        public void KeyFob_ResetToDefault(Alias deviceNumber)
        {
            var commandsProvider = (KeyFobCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            commandsProvider.ResetToDefault();
            //delay 5..10s?
        }

        /// <summary>
        /// Activate Remove From Group on the Key Fob Application
        /// </summary>
        public void KeyFob_RemoveFromGroup(Alias deviceNumber)
        {
            var commandsProvider = (KeyFobCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            commandsProvider.RemoveFromGroup();
        }

        /// <summary>
        /// Enter Network Wide Inclusion (NWI) learn-mode so the KeyFob can 
        /// be included as a secondary controller – LED3 is on while in NWI.
        /// </summary>
        public void KeyFob_LearnModeNWI(Alias deviceNumber)
        {
            var commandsProvider = (KeyFobCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            commandsProvider.StartNWI();
        }

        /// <summary>
        /// Enter Network Wide Exclusion (NWE) learn-mode so the KeyFob can 
        /// be excluded from being a secondary controller.
        /// </summary>
        public void KeyFob_LearnModeNWE(Alias deviceNumber)
        {
            var commandsProvider = (KeyFobCommandsProvider)GetSampleCommandsProvider(deviceNumber);
            commandsProvider.StartNWE();
        }

        /// <summary>
        /// Checks LED1 is blinked during 30sec on learn mode active
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <returns>Is LED1 Blinked</returns>
        public bool IsLearnModeActive(Alias deviceNumber)
        {
            var sample = GetSampleCommandsProvider(deviceNumber);
            return sample.IsLearnModeActive();
        }
    }

    internal abstract class SampleCommandsProvider
    {
        protected ITestInterfaceDevice _device;
        protected bool _is700;
        private DevKits _devKit;
        protected string _chipSeries;
        protected int GetNumberFromDevKitVersion(string version)
        {
            int ret = 0;
            if (!string.IsNullOrEmpty(version))
            {
                var verNums = string.Concat(version.Where(char.IsDigit));
                if (!string.IsNullOrEmpty(verNums))
                {
                    int.TryParse(verNums, out ret);
                }
            }
            return ret;
        }

        public SampleCommandsProvider(ITestInterfaceDevice device, bool is700, DevKits devKit, string chipSeries)
        {
            _device = device;
            _is700 = is700;
            _devKit = devKit;
            _chipSeries = chipSeries;
        }

        /// <summary>
        ///  Button's Ids start from 1
        /// </summary>
        /// <param name="buttonNumber">Number starts from '01' for 7 series it means button BTN0, for 5 series it corresponds to button S1</param>
        /// <returns></returns>
        protected ReturnValueResult ToggleButton(byte buttonNumber)
        {
            int timeoutMs = TestInterfaceConstants.ButtonOperations.DefaultTimeoutMs;
            return SendToTestInterface(TestInterfaceConstants.ButtonActionTemplates.ToggleButton(buttonNumber), timeoutMs);
        }

        /// <summary>
        ///  Button's Ids start from 1
        /// </summary>
        /// <param name="buttonNumber">Number starts from '01' for 7 series it means button BTN0, for 5 series it corresponds to button S1</param>
        /// <param name="times">count</param>
        /// <returns></returns>
        protected ReturnValueResult PressButton(byte buttonNumber, byte times)
        {
            int timeoutMs = TestInterfaceConstants.ButtonOperations.DefaultTimeoutMs;
            return SendToTestInterface(TestInterfaceConstants.ButtonActionTemplates.PressButton(buttonNumber, times), timeoutMs);
        }

        /// <summary>
        /// Button's Ids start from 1
        /// </summary>
        /// <param name="buttonNumber">Number starts from '01' for 7 series it means button BTN0, for 5 series it corresponds to button S1</param>
        /// <param name="duration">seconds</param>
        /// <returns></returns>
        protected ReturnValueResult HoldButton(byte buttonNumber, byte duration)
        {
            int timeoutMs = 2 * duration * 1000 + 500;
            return SendToTestInterface(TestInterfaceConstants.ButtonActionTemplates.HoldButton(buttonNumber, duration), timeoutMs);
        }

        protected ReturnValueResult SendToTestInterface(byte[] command, int timeoutMs)
        {
            var ret = _device.TestInterfaceSendData2(command, timeoutMs);
            Thread.Sleep(200);
            return ret.SendData;
        }

        public virtual void LearnMode()
        {
            if (_is700)
            {
                ToggleButton(0x02);
            }
            else
            {
                if (_devKit != DevKits.Default && _devKit < DevKits.v6_81)
                {
                    PressButton(0x01, 0x03);
                }
                else
                {
                    ToggleButton(0x01);
                }
            }
        }

        public virtual void Reset()
        {
            if (_is700)
            {
                HoldButton(0x02, 0x07);
            }
            else
            {
                HoldButton(0x01, 0x0C);
            }
        }

        protected bool? GetLedState(byte? ledsMask)
        {
            var ret = SendToTestInterface(new byte[] { (byte)ProgrammerCommandTypes.GetLedState }, 1000);
            if (ret && ret.ByteArray != null && ret.ByteArray.Length > 0)
            {
                ret.ByteArray[0] = (byte)~ret.ByteArray[0];
                if (ledsMask != null)
                {
                    ret.ByteArray[0] = (byte)(ret.ByteArray[0] & ledsMask.Value);
                }
                return ret.ByteValue == ledsMask;
            }
            return null;
        }

        public virtual bool IsLearnModeActive(int durationMs = 30000)
        {
            //byte antiMask = 0xFE; // - check concrete state
            int catchedBlinksCount = 0;
            var data = new byte[] { (byte)ProgrammerCommandTypes.GetLedState };
            var bitLedNumber = 1;
            var tkn = _device.TestInterfaceNoiseData(data,
                (x) =>
                {
                    if (x && x.ByteArray != null && x.ByteArray.Length > 0)
                    {
                        byte b = x.ByteValue;
                        bool isOn = (b & (1 << bitLedNumber)) != 0; //x.ByteValue == antiMask
                        if (isOn)
                        {
                            catchedBlinksCount++;
                        }
                    }
                }, 100, 200);
            Thread.Sleep(durationMs);
            _device.Cancel(tkn);
            var res = tkn.WaitCompletedSignal();
            return catchedBlinksCount > 0;
        }
    }

    internal class DoorLookKeypadCommandsProvider : SampleCommandsProvider
    {
        public DoorLookKeypadCommandsProvider(ITestInterfaceDevice device, bool is700, DevKits devKit, string chipSeries)
            : base(device, is700, devKit, chipSeries)
        {
        }

        public void ToggleDoorHandle()
        {
            if (_is700)
            {
                ToggleButton(0x01);
            }
            else
            {
                ToggleButton(0x04);
            }
        }

        public void ToggleDoorHandle(byte duration)
        {
            if (_is700)
            {
                HoldButton(0x01, duration);
            }
            else
            {
                HoldButton(0x04, duration);
            }
        }

        public void ToggleKeyPad()
        {
            if (_is700)
            {
                ToggleButton(0x04);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public void BatteryReport()
        {
            if (_is700)
            {
                ToggleButton(0x03);
            }
            else
            {
                Reset();
            }
        }

        public bool? IsLatchOpen()
        {
            return !GetLedState(0x01);
        }

        public bool? IsBoltLocked()
        {
            return _is700 ? !GetLedState(0x04) : !GetLedState(0x02);
        }
    }

    internal class LEDBulbCommandsProvider : SampleCommandsProvider
    {
        public LEDBulbCommandsProvider(ITestInterfaceDevice device, bool is700, DevKits devKit, string chipSeries)
            : base(device, is700, devKit, chipSeries)
        {
        }

        public override void LearnMode()
        {
            if (_chipSeries == ChipSeries.ZG13L_BRD4201C.ToString())
            {
                ToggleButton(0x03);
            }
            else
            {
                base.LearnMode();
            }
        }

        public void DimHold(int duration)
        {
            if (_is700)
            {
                HoldButton(0x03, (byte)duration);
            }
            else
            {
                HoldButton(0x04, (byte)duration);
            }
        }

        public void DimOnOff()
        {
            if (_is700)
            {
                ToggleButton(0x03);
            }
            else
            {
                ToggleButton(0x04);
            }
        }

        public void ToggleNotify()
        {
            if (_is700)
            {
                ToggleButton(0x04);
            }
            else
            {
                ToggleButton(0x06);
            }
        }

        public void OnOff()
        {
            if (_is700)
            {
                ToggleButton(0x01);
            }
            else
            {
                ToggleButton(0x02);
            }
        }

        public bool? IsStatusOn()
        {
            return GetLedState(0x01);
        }

        public bool? IsDimStatusOn()
        {
            return !_is700 ? GetLedState(0x02) : throw new NotSupportedException("Not supported LED interface for 700s series");
        }
    }

    internal class PowerStripCommandsProvider : SampleCommandsProvider
    {
        public PowerStripCommandsProvider(ITestInterfaceDevice device, bool is700, DevKits devKit, string chipSeries)
            : base(device, is700, devKit, chipSeries)
        {
        }

        public void DimHold(int duration)
        {
            if (_is700)
            {
                HoldButton(0x03, (byte)duration);
            }
            else
            {
                HoldButton(0x04, (byte)duration);
            }
        }

        public void DimOnOff()
        {
            if (_is700)
            {
                ToggleButton(0x03);
            }
            else
            {
                ToggleButton(0x04);
            }
        }

        public void ToggleNotify()
        {
            if (_is700)
            {
                ToggleButton(0x04);
            }
            else
            {
                ToggleButton(0x06);
            }
        }

        public void OnOff()
        {
            if (_is700)
            {
                ToggleButton(0x01);
            }
            else
            {
                ToggleButton(0x02);
            }
        }

        public bool? IsStatusOn()
        {
            return GetLedState(0x01);
        }

        public bool? IsDimStatusOn()
        {
            return !_is700 ? GetLedState(0x02) : throw new NotSupportedException("Not supported LED interface for 700s series");
        }
    }

    internal class SensorPirCommandsProvider : SampleCommandsProvider
    {
        public SensorPirCommandsProvider(ITestInterfaceDevice device, bool is700, DevKits devKit, string chipSeries)
            : base(device, is700, devKit, chipSeries)
        {
        }

        public void ActivatePirEvent()
        {
            if (_is700)
            {
                if (_chipSeries == ChipSeries.RZ13_BRD4201B.ToString())
                {
                    HoldButton(0x03, 0x03);
                }
                else if (_chipSeries == ChipSeries.ZG13S_BRD4201D.ToString())
                {
                    HoldButton(0x03, 0x03);
                }
                else
                {
                    HoldButton(0x03, 0x05);
                }
            }
            else
            {
                ToggleButton(0x02);
            }
        }

        public void BatteryReport()
        {
            if (_is700)
            {
                if (_chipSeries == ChipSeries.RZ13_BRD4201B.ToString())
                {
                    ToggleButton(0x04);
                }
                else if (_chipSeries == ChipSeries.ZG13S_BRD4201D.ToString())
                {
                    ToggleButton(0x04);
                }
                else
                {
                    ToggleButton(0x01);
                }
            }
            else
            {
                ToggleButton(0x05);
            }
        }

        public override void Reset()
        {
            if (_chipSeries == ChipSeries.RZ13_BRD4201B.ToString())
            {
                HoldButton(0x03, 0x07);
            }
            else if (_chipSeries == ChipSeries.ZG13S_BRD4201D.ToString())
            {
                HoldButton(0x03, 0x07);
            }
            else
            {
                base.Reset();
            }
        }

        public override void LearnMode()
        {
            if (_chipSeries == ChipSeries.RZ13_BRD4201B.ToString())
            {
                ToggleButton(0x03);
            }
            else if (_chipSeries == ChipSeries.ZG13S_BRD4201D.ToString())
            {
                ToggleButton(0x03);
            }
            else
            {
                base.LearnMode();
            }
        }
    }

    internal class SwithOnOffCommandsProvider : SampleCommandsProvider
    {
        public SwithOnOffCommandsProvider(ITestInterfaceDevice device, bool is700, DevKits devKit, string chipSeries)
            : base(device, is700, devKit, chipSeries)
        {
        }

        public void OnOff()
        {
            if (_is700)
            {
                ToggleButton(0x01);
            }
            else
            {
                ToggleButton(0x02);
            }
        }

        public void HoldKey01(byte duration)
        {
            if (_is700)
            {
                HoldButton(0x01, duration);
            }
            else
            {
                HoldButton(0x02, duration);
            }
        }

        public bool? IsStatusOn()
        {
            return _is700 ? GetLedState(0x01) : GetLedState(0x02);
        }
    }

    internal class WallControllerCommandsProvider : SampleCommandsProvider
    {
        public WallControllerCommandsProvider(ITestInterfaceDevice device, bool is700, DevKits devKit, string chipSeries)
            : base(device, is700, devKit, chipSeries)
        {
        }

        public void Key01()
        {
            if (_is700)
            {
                ToggleButton(0x01);
            }
            else
            {
                ToggleButton(0x02);
            }
        }

        public void HoldKey01(byte duration)
        {
            if (_is700)
            {
                HoldButton(0x01, duration);
            }
            else
            {
                HoldButton(0x02, duration);
            }
        }

        public void Key02()
        {
            if (_is700)
            {
                ToggleButton(0x03);
            }
            else
            {
                ToggleButton(0x04);
            }
        }

        public void HoldKey02(byte duration)
        {
            if (_is700)
            {
                HoldButton(0x03, duration);
            }
            else
            {
                HoldButton(0x04, duration);
            }
        }

        public void Key03()
        {
            if (_is700)
            {
                ToggleButton(0x04);
            }
            else
            {
                ToggleButton(0x06);
            }
        }

        //HoldButton
        public void HoldKey03(byte duration)
        {
            if (_is700)
            {
                HoldButton(0x04, duration);
            }
            else
            {
                HoldButton(0x06, duration);
            }
        }
    }

    internal class MultilevelSensorCommandsProvider : SampleCommandsProvider
    {
        public MultilevelSensorCommandsProvider(ITestInterfaceDevice device, bool is700, DevKits devKit, string chipSeries)
            : base(device, is700, devKit, chipSeries)
        {
        }

        /// <summary>
        ///Trigger a movement detection (simulated by button BTN2).
        ///Hold for at least 1 second and release
        /// </summary>
        public void ActivateEvent()
        {
            if (_is700)
            {

                if (_chipSeries == ChipSeries.ZGM230S_BRD4205B.ToString())
                {
                    HoldButton(0x01, 0x01);
                }
                else
                {
                    HoldButton(0x03, 0x03);
                }
            }
            else
            {
                //ToggleButton(0x02);
                throw new NotImplementedException("Unsupported by 500s");
            }
        }

        /// <summary>
        /// Sends Battery Report (only if the device is not sleeping)
        /// </summary>
        public void BatteryReport()
        {
            if (_is700)
            {
                ToggleButton(0x01);
            }
            else
            {
                ToggleButton(0x05);
            }
        }

        /// <summary>
        /// LED1.
        /// Blinks with 1 Hz when learn mode is active.
        /// Used for Indicator Command Class.
        /// </summary>
        /// <returns></returns>
        public bool? IsStatusOn()
        {
            return _is700 ? GetLedState(0x01) : GetLedState(0x02);
        }

    }

    internal class KeyFobCommandsProvider : SampleCommandsProvider
    {
        public KeyFobCommandsProvider(ITestInterfaceDevice device, bool is700, DevKits devKit, string chipSeries)
            : base(device, is700, devKit, chipSeries)
        {
        }

        public override void LearnMode()
        {
            StartNWI();
        }

        /// <summary>
        ///Trigger Add Node
        ///Short Press Button 0
        /// </summary>
        public void AddNode()
        {
            if (_is700)
            {
                ToggleButton(0x01);
            }
            else
            {
                throw new NotImplementedException("Unsupported by 500s or less");
            }
        }


        /// <summary>
        ///Trigger Remove Node
        ///Long Press (1s) Button 0
        /// </summary>
        public void RemoveNode()
        {
            if (_is700)
            {
                HoldButton(0x01, 0x01);
            }
            else
            {
                throw new NotImplementedException("Unsupported by 500s or less");
            }
        }

        /// <summary>
        ///Trigger Reset to Default
        ///Hold (5s) Button 0
        /// </summary>
        public void ResetToDefault()
        {
            if (_is700)
            {
                HoldButton(0x01, 0x05);
            }
            else
            {
                throw new NotImplementedException("Unsupported by 500s or less");
            }
        }


        /// <summary>
        ///Trigger Basic Set 'On'
        ///Short Press Button 1
        /// </summary>
        public void BasicSetOn()
        {
            if (_is700)
            {
                ToggleButton(0x02);
            }
            else
            {
                throw new NotImplementedException("Unsupported by 500s or less");
            }
        }


        /// <summary>
        ///Trigger Add to Group
        ///Long Press (1s) Button 1
        /// </summary>
        public void AddToGroup()
        {
            if (_is700)
            {
                HoldButton(0x02, 0x01);
            }
            else
            {
                throw new NotImplementedException("Unsupported by 500s or less");
            }
        }

        /// <summary>
        ///Trigger Basic Set 'Off'
        ///Short Press Button 2
        /// </summary>
        public void BasicSetOff()
        {
            if (_is700)
            {
                ToggleButton(0x03);
            }
            else
            {
                throw new NotImplementedException("Unsupported by 500s or less");
            }
        }


        /// <summary>
        ///Trigger Remove from Group
        ///Long Press (1s) Button 2
        /// </summary>
        public void RemoveFromGroup()
        {
            if (_is700)
            {
                HoldButton(0x03, 0x01);
            }
            else
            {
                throw new NotImplementedException("Unsupported by 500s or less");
            }
        }

        /// <summary>
        ///Trigger NWI
        ///Short press Button 3
        /// </summary>
        public void StartNWI()
        {
            if (_is700)
            {
                ToggleButton(0x04);
            }
            else
            {
                throw new NotImplementedException("Unsupported by 500s or less");
            }
        }

        /// <summary>
        ///Trigger NWE
        ///Long Press (1s) Button 3
        /// </summary>
        public void StartNWE()
        {
            if (_is700)
            {
                HoldButton(0x04, 0x01);
            }
            else
            {
                throw new NotImplementedException("Unsupported by 500s or less");
            }
        }

        /// <summary>
        /// Add to group indicator
        /// LED0.
        /// </summary>
        /// <returns>Statically On/Off</returns>
        public bool? IsAddToGroupOn()
        {
            return _is700 ? GetLedState(0x01) : throw new NotImplementedException("Unsupported by 500s or less");
        }
    }
}
