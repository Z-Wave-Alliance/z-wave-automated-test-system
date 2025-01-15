// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils.UI.Enums;
using ZWave.TextApplication;

namespace Zats.Services
{
    public class PsuE364xInstrumentService : ServiceBase
    {
        private const float _voltageTolerance = 0.02F;
        private readonly InstrumentsManager _instrumentsManager;

        public PsuE364xInstrumentService(InstrumentsManager instrumentsManager)
        {
            _instrumentsManager = instrumentsManager;
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            _instrumentsManager.Log(text, logLevel);
        }

        public TextResult.Request SetVoltage(Alias instrument, int outputLine, float level)
        {
            Assert.IsTrue(level > 1.0F && level < 4.0F, "Voltage level must be in range 1..4 V");
            Assert.IsTrue(outputLine == 1 || outputLine == 2, "Output line could be 1 or 2");
            var td = _instrumentsManager.GetInstrument(instrument) as TextDevice;
            td.Connect();
            td.Request($"INSTrument:NSELect {outputLine}");
            td.Request($"VOLTage {level}");
            var res = td.RequestLine("MEASure:VOLTage?");
            Assert.IsTrue(res && !string.IsNullOrWhiteSpace(res.ReceivedText));
            Assert.IsTrue(float.TryParse(res.ReceivedText.Trim(), out float f));
            Assert.IsTrue(Math.Abs(level - f) < _voltageTolerance);
            res.ReceivedValue = f;
            var ret = new TextActionResult.Request(res);
            td.Disconnect();
            return ret;
        }

        public TextResult.Request GetVoltage(Alias instrument, int outputLine)
        {
            Assert.IsTrue(outputLine == 1 || outputLine == 2, "Output line could be 1 or 2");
            var td = _instrumentsManager.GetInstrument(instrument) as TextDevice;
            td.Connect();
            td.Request($"INSTrument:NSELect {outputLine}");
            var res = td.RequestLine("MEASure:VOLTage?");
            Assert.IsTrue(res && !string.IsNullOrWhiteSpace(res.ReceivedText));
            Assert.IsTrue(float.TryParse(res.ReceivedText.Trim(), out float f));
            res.ReceivedValue = f;
            var ret = new TextActionResult.Request(res);
            td.Disconnect();
            return ret;
        }
    }
}
