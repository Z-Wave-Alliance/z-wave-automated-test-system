// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utils.UI.Enums;
using ZWave.TextApplication;

namespace Zats.Services
{
    public class AttenuatorRudatInstrumentService : ServiceBase
    {
        private const float _attenualtionTolerance = 0.26F;
        private readonly InstrumentsManager _instrumentsManager;

        public AttenuatorRudatInstrumentService(InstrumentsManager instrumentsManager)
        {
            _instrumentsManager = instrumentsManager;
        }

        public TextResult.Request SetDbLevel(Alias instrument, float level)
        {
            var td = _instrumentsManager.GetInstrument(instrument);
            td.Connect();
            var res = td.RequestLine("B" + level.ToString() + "E", 1000);
            Assert.IsTrue(res);
            var ret = new TextActionResult.Request(res);
            res = td.RequestLine("A", 1000);
            Assert.IsTrue(res && !string.IsNullOrWhiteSpace(res.ReceivedText));
            Assert.IsTrue(float.TryParse(res.ReceivedText.Trim(), out float f));
            Assert.IsTrue(Math.Abs(level - f) < _attenualtionTolerance);
            td.Disconnect();
            return ret;
        }

        public TextResult.Request GetDbLevel(Alias instrument)
        {
            var td = _instrumentsManager.GetInstrument(instrument);
            td.Connect();
            var res = td.RequestLine("A", 1000);
            Assert.IsTrue(res && !string.IsNullOrWhiteSpace(res.ReceivedText));
            string retValue = new string(res.ReceivedText.Trim().Where(c => char.IsDigit(c) || c == '.').ToArray());
            Assert.IsTrue(float.TryParse(retValue, out float f));
            res.ReceivedValue = f;
            var ret = new TextActionResult.Request(res);
            td.Disconnect();
            return ret;
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            _instrumentsManager.Log(text, logLevel);
        }
    }
}
