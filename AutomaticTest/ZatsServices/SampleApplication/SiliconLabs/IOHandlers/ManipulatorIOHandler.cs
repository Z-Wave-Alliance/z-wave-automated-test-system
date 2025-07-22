using System.Threading;
using Zats.Services.Constants;
using ZWave;
using ZWave.BasicApplication.Devices;

namespace Zats.Services.SampleApplication
{
    public class ManipulatorIOHandler : IIOHandler
    {
        protected ITestInterfaceDevice _device;

        public ManipulatorIOHandler(ITestInterfaceDevice device)
        {
            _device = device;
        }

        /// <summary>
        ///  Button's Ids start from 1
        /// </summary>
        /// <param name="buttonNumber">Number starts from '01' for 7 series it means button BTN0, for 5 series it corresponds to button S1</param>
        /// <returns></returns>
        public ReturnValueResult ToggleButton(byte buttonNumber)
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
        public ReturnValueResult PressButton(byte buttonNumber, byte times)
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
        public ReturnValueResult HoldButton(byte buttonNumber, byte duration)
        {
            int timeoutMs = 2 * duration * 1000 + 500;
            return SendToTestInterface(TestInterfaceConstants.ButtonActionTemplates.HoldButton(buttonNumber, duration), timeoutMs);
        }

        public ReturnValueResult SendToTestInterface(byte[] command, int timeoutMs)
        {
            var ret = _device.TestInterfaceSendData2(command, timeoutMs);
            Thread.Sleep(200);
            return ret.SendData;
        }

        public bool? GetLedState(byte? ledsMask)
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
}
