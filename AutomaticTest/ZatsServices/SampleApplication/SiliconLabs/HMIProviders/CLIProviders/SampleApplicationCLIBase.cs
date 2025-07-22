using System;
using System.Text.RegularExpressions;
using ZWave.TextApplication;
using ZWave.TextApplication.Operations;

namespace Zats.Services.SampleApplication.SiliconLabs.CLIProviders
{
    class SampleApplicationCLIBase : ISampleApplicationProviderBase
    {
        public const int TIMEOUT_MS = 100;

        private readonly TextDevice _textDevice;

        public SampleApplicationCLIBase(TextDevice device)
        {
            _textDevice = device;
            _textDevice.ResponseEndOfLine = Environment.NewLine + ">";
            _textDevice.TransportClient.ApiType = ZWave.Enums.ApiTypes.SampleCLI; //settings for output handling and displaying
        }

        public void LearnMode()
        {
            SendCommand("set_learn_mode", TIMEOUT_MS);
        }

        public void Reset()
        {
            SendCommand("factory_reset", TIMEOUT_MS);
        }

        public RequestResult SendCommand(string command, int timeoutMs)
        {
            var ret = _textDevice.RequestWithDelay(command, timeoutMs);
            return ret;
        }

        public Match SendToTestInterfaceAndMatchRegex(string command, Regex regex)
        {
            var ret = SendCommand(command, TIMEOUT_MS);
            if (ret && string.IsNullOrEmpty(ret.ReceivedText))
                return null;

            string str = ret.ReceivedText;
            Match match = regex.Match(str);
            return match.Success ? match : null;
        }
    }
}