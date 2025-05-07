using System;

namespace Zats.Services
{
    public class PCControllerTransportServiceTestSettings : TestSettingsBase, ITransportServiceTestSettings
    {
        private readonly PCControllerDeviceManager _pccDeviceManager;
        public PCControllerTransportServiceTestSettings(PCControllerDeviceManager pccDeviceManager, DeviceManager deviceManager) : 
            base(deviceManager)
        {
            _pccDeviceManager = pccDeviceManager;
        }

        public void SetSubsequentFragmentCRC16(Alias deviceNumber, byte[] crc16, int offset, int timesUsed)
        {
            throw new NotImplementedException();
        }

        public void SetFirstFragmentCRC16(Alias deviceNumber, byte[] crc16, int timesUsed)
        {
            throw new NotImplementedException();
        }

        public void SetSegmentCompleteCmdSessionId(Alias deviceNumber, byte sessionId, int timesUsed)
        {
            throw new NotImplementedException();
        }

        public void SetIgnoreFirstSegment(Alias deviceNumber, int timesUsed)
        {
            throw new NotImplementedException();
        }

        public void SetIgnoreSubsequentSegment(Alias deviceNumber, int offset, int timesUsed)
        {
            throw new NotImplementedException();
        }
    }
}
