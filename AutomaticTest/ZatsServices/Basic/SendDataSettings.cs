using ZWave.BasicApplication;
using ZWave.BasicApplication.TransportService;

namespace Zats.Services
{
    public class SendDataSettings : ISendDataSettings
    {
        DeviceManager _deviceManager;

        public SendDataSettings(DeviceManager deviceManager)
        {
            _deviceManager = deviceManager;
        }

        public void Clear(Alias controllerNumber)
        {
            GetItem(controllerNumber).Clear();
        }

        private SendDataDeviceSettings GetItem(Alias controllerNumber)
        {
            SendDataDeviceSettings ret = new SendDataDeviceSettings();
            var device = _deviceManager.GetDevice(controllerNumber);
            var settingsSM = device.SessionClient.GetSubstituteManager(typeof(SettingsSubstituteManager));
            if (settingsSM != null)
            {
                ret = ((SettingsSubstituteManager)settingsSM).SendDataDeviceSettings;
            }
            return ret;
        }

        public void SetAutoIncrement(Alias controllerNumber, bool enabled)
        {
            GetItem(controllerNumber).IsAutoIncrementEnabled = enabled;
        }

        public void SetBitAdress(Alias controllerNumber, bool enabled)
        {
            GetItem(controllerNumber).IsBitAdress = enabled;
        }

        public void SetCRC16(Alias controllerNumber, bool enabled)
        {
            GetItem(controllerNumber).IsCRC16Enabled = enabled;
        }

        public void SetDstEndPoint(Alias controllerNumber, byte enabled)
        {
            GetItem(controllerNumber).DstEndPoint = enabled;
        }

        public void SetForceMultiCast(Alias controllerNumber, bool enabled)
        {
            GetItem(controllerNumber).IsForceMultiCastEnabled = enabled;
        }

        public void SetSkipWaitingSendCallback(Alias controllerNumber, bool enabled)
        {
            GetItem(controllerNumber).IsSkipWaitingSendCallbackEnabled = enabled;
        }

        public void SetMaxBytesPerFrameSize(Alias controllerNumber, int value)
        {
            GetItem(controllerNumber).MaxBytesPerFrameSize = value;
        }

        public void SetMultiChannel(Alias controllerNumber, bool enabled)
        {
            GetItem(controllerNumber).IsMultiChannelEnabled = enabled;
        }

        public void SetSecure(Alias controllerNumber, bool? enabled)
        {
            if (enabled == true)
            {
                GetItem(controllerNumber).IsSecureEnabled = true;
                GetItem(controllerNumber).IsSecureEnabledSpecified = true;
            }
            else if (enabled == false)
            {
                GetItem(controllerNumber).IsSecureEnabled = false;
                GetItem(controllerNumber).IsSecureEnabledSpecified = true;
            }
            else
            {
                GetItem(controllerNumber).IsSecureEnabledSpecified = false;
            }
        }

        public void SetSessionID(Alias controllerNumber, byte value)
        {
            GetItem(controllerNumber).SessionID = value;
        }

        public void SetSrcEndPoint(Alias controllerNumber, byte enabled)
        {
            GetItem(controllerNumber).SrcEndPoint = enabled;
        }

        public void SetStatusUpdates(Alias controllerNumber, bool enabled)
        {
            GetItem(controllerNumber).IsStatusUpdateEnabled = enabled;
        }

        public void SetSupervisionGet(Alias controllerNumber, bool enabled)
        {
            GetItem(controllerNumber).IsSupervisionGetEnabled = enabled;
        }

        public void SetSupressMulticastFollowUp(Alias controllerNumber, bool enabled)
        {
            GetItem(controllerNumber).IsSupressMulticastFollowUpEnabled = enabled;
        }

        public void SetTransportServiceMaxSize(Alias controllerNumber, int value)
        {
            GetItem(controllerNumber).TransportServiceMaxSize = value;
            var device = _deviceManager.GetDevice(controllerNumber);
            device.Network.TransportServiceMaxSegmentSize = value;
        }

        public void SetCallbackWaitTimeout(Alias deviceNumber, int timeoutMs)
        {
            GetItem(deviceNumber).CallbackWaitTimeoutMs = timeoutMs;
        }

        public void SetResponseDelayMs(Alias controllerNumber, int value)
        {
            GetItem(controllerNumber).ResponseDelayMs = value;
        }

        public bool GetCRC16(Alias controllerNumber)
        {
            return GetItem(controllerNumber).IsCRC16Enabled;
        }

        public bool GetSupressMulticastFollowUp(Alias controllerNumber)
        {
            return GetItem(controllerNumber).IsSupressMulticastFollowUpEnabled;
        }

        public bool GetForceMultiCast(Alias controllerNumber)
        {
            return GetItem(controllerNumber).IsForceMultiCastEnabled;
        }

        public bool GetSkipWaitingSendCallback(Alias controllerNumber)
        {
            return GetItem(controllerNumber).IsSkipWaitingSendCallbackEnabled;
        }

        public bool GetSupervisionGet(Alias controllerNumber)
        {
            return GetItem(controllerNumber).IsSupervisionGetEnabled;
        }

        public bool GetStatusUpdates(Alias controllerNumber)
        {
            return GetItem(controllerNumber).IsStatusUpdateEnabled;
        }

        public byte GetSessionID(Alias controllerNumber)
        {
            return GetItem(controllerNumber).SessionID;
        }

        public bool GetAutoIncrement(Alias controllerNumber)
        {
            return GetItem(controllerNumber).IsAutoIncrementEnabled;
        }

        public bool GetMultiChannel(Alias controllerNumber)
        {
            return GetItem(controllerNumber).IsMultiChannelEnabled;
        }

        public byte GetSrcEndPoint(Alias controllerNumber)
        {
            return GetItem(controllerNumber).SrcEndPoint;
        }

        public byte GetDstEndPoint(Alias controllerNumber)
        {
            return GetItem(controllerNumber).DstEndPoint;
        }

        public bool GetBitAdress(Alias controllerNumber)
        {
            return GetItem(controllerNumber).IsBitAdress;
        }

        public bool? GetSecure(Alias controllerNumber)
        {
            bool? ret = null;
            if (GetItem(controllerNumber).IsSecureEnabledSpecified)
            {
                ret = GetItem(controllerNumber).IsSecureEnabled;
            }
            return ret;
        }

        public int GetTransportServiceMaxSize(Alias controllerNumber)
        {
            return GetItem(controllerNumber).TransportServiceMaxSize;
        }

        public int GetMaxBytesPerFrameSize(Alias controllerNumber)
        {
            return GetItem(controllerNumber).MaxBytesPerFrameSize;
        }

        public int GetResponseDelayMs(Alias controllerNumber)
        {
            return GetItem(controllerNumber).ResponseDelayMs;
        }

        public int GetCallbackWaitTimeoutMs(Alias deviceNumber)
        {
            return GetItem(deviceNumber).CallbackWaitTimeoutMs;
        }
    }
}
