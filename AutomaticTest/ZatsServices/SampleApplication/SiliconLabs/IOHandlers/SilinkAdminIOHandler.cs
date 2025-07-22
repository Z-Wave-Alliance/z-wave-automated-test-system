using System.Threading;
using ZWave;
using ZWave.TextApplication;

namespace Zats.Services.SampleApplication
{
    public class SilinkAdminIOHandler : IIOHandler
    {
        private readonly TextDevice _device;
        public SilinkAdminIOHandler(TextDevice textDevice)
        {
            _device = textDevice;
        }

        private string SendCommand(string command, int delayTimeoutMs)
        {
            string res = string.Empty;
            int maxRetries = 2;
            int retry = 0;
            while (retry++ < maxRetries)
            {
                var requestResult = _device.RequestLine(command);
                if (requestResult != null) res = requestResult.ReceivedText ?? "";
                if (res.Contains(">"))
                {
                    retry = maxRetries;
                }
                Thread.Sleep(delayTimeoutMs);
            }
            return res;
        }

        public bool? GetLedState(byte? ledsMask)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Hold button press duing duration
        /// </summary>
        /// <param name="buttonNumber"></param>
        /// <param name="duration">0 - medium click ></param>
        /// <returns></returns>
        public ReturnValueResult HoldButton(byte buttonNumber, byte duration)
        {
            string cmdPress = $"target button {ButtonCommands.Press} {buttonNumber}";
            SendCommand(cmdPress, 100);
            var btnPressReleaseMs = (duration == 0) ? (int)SilinkButtonPressTypes.Medium : duration * 1000;
            Thread.Sleep(btnPressReleaseMs);
            string cmdRelease = $"target button {ButtonCommands.Release} {buttonNumber}";
            SendCommand(cmdRelease, 100);
            return new ReturnValueResult();
        }

        public bool IsLearnModeActive(int durationMs = 30000)
        {
            throw new System.NotImplementedException();
        }

        public ReturnValueResult PressButton(byte buttonNumber, byte times)
        {
            var ret = new ReturnValueResult();
            for (int i = 0; i < times; i++)
            {
                ret = HoldButton(buttonNumber, 0);
            }
            return ret;
        }

        /// <summary>
        /// Short button press, no more than 250ms
        /// </summary>
        /// <param name="buttonNumber">Call short click on a <paramref name="buttonNumber"/></param>
        public ReturnValueResult ToggleButton(byte buttonNumber)
        {
            string cmdPress = $"target button {ButtonCommands.Press} {buttonNumber}";
            SendCommand(cmdPress, (int)SilinkButtonPressTypes.Short);
            string cmdRelease = $"target button {ButtonCommands.Release} {buttonNumber}";
            SendCommand(cmdRelease, (int)SilinkButtonPressTypes.Short);
            return new ReturnValueResult();
        }

        internal void Ping()
        {
            var delay = 200;
            for (int i = 0; i < 2; i++)
            {
                //Toglle admin page x2
                Thread.Sleep(delay);
                _device.RequestLine(string.Empty);
            }
            Thread.Sleep(delay);
        }


        internal string EnableButtons()
        {
            string cmd = $"target button {ButtonCommands.Enable}";
            return SendCommand(cmd, 100);
        }

        internal string DisableButtons()
        {
            string cmd = $"target button {ButtonCommands.Disable}";
            return SendCommand(cmd, 100);
        }
    }
}
