using System;
using Utils.UI.Enums;
using ZWave.TextApplication;

namespace Zats.Services
{
    public class Dmm7510InstrumentService : ServiceBase
    {
        private readonly InstrumentsManager _instrumentsManager;

        public Dmm7510InstrumentService(InstrumentsManager instrumentsManager)
        {
            _instrumentsManager = instrumentsManager;
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            _instrumentsManager.Log(text, logLevel);
        }

        private const string bufferName = "defbuffer2";
        public CaptureTraceObject CaptureTrace(Alias instrument, Dmm7510Modes mode, out DmmTraceLog trace)
        {
            trace = new DmmTraceLog(bufferName);
            var td = _instrumentsManager.GetInstrument(instrument) as TextDevice;
            CaptureTraceObject ret = new CaptureTraceObject(td, trace);
            td.Connect();

            var res = td.Request($"SYSTem:ERRor:COUNt?");
            float.TryParse(res.ReceivedText.Trim(), out float errors);
            if (errors > 0)
            {
                for (int i = 0; i < errors; i++)
                {
                    res = td.Request($"SYSTem:ERRor?");
                }
            }
            res = td.RequestLine($"TRACe:CLEar \"{bufferName}\"");
            res = td.RequestLine($"TRACe:POINts 1000000, \"{bufferName}\"");
            res = td.RequestLine($"TRACe:POINts? \"{bufferName}\"");
            res = td.RequestLine($"TRACe:ACTual? \"{bufferName}\"");
            //res = td.RequestLine($"TRACe:FILL:MODE CONT, \"{bufferName}\"");
            //res = td.RequestLine($"MEASure:DIGitize:CURRent? \"{bufferName}\", TSTamp, UNIT");

            td.Disconnect();
            return ret;
        }
    }

    public class CaptureTraceObject : IDisposable
    {
        private TextDevice _td;
        private DmmTraceLog _trace;

        public CaptureTraceObject(TextDevice td, DmmTraceLog trace)
        {
            _td = td;
            _trace = trace;
        }

        public void Dispose()
        {
            if (_td != null)
            {
                _td.Connect();
                var res = _td.RequestLine($"TRACe:ACTual? \"{_trace.BufferName}\"", 10000);
                res = _td.RequestLine($"TRACe:ACTual:STARt? \"{_trace.BufferName}\"", 10000);
                res = _td.RequestLine($"TRACe:ACTual:END? \"{_trace.BufferName}\"", 10000);
                float.TryParse(res.ReceivedText.Trim(), out float endIndex);
                if (endIndex > 1)
                {
                    res = _td.RequestLine($"TRACe:DATA? 1, {endIndex}, \"{_trace.BufferName}\"", 10000);
                }
                _td.Disconnect();
            }
        }
    }

    public class DmmTraceLog
    {
        public DmmTraceLog(string bufferName)
        {
            BufferName = bufferName;
        }

        public string BufferName { get; set; }

        public float Integral()
        {
            return 0F;
        }
    }

    public enum Dmm7510Modes
    {
        Current100mV
    }
}
