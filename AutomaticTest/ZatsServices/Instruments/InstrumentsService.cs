// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils.UI.Enums;

namespace Zats.Services
{
    public class InstrumentsService : ServiceBase
    {
        private InstrumentsManager _instrumentsManager; 
        public AttenuatorRudatInstrumentService AttenuatorRudat { get; set; }
        public PsuE364xInstrumentService PsuE364x { get; set; }
        public Dmm7510InstrumentService Dmm7510 { get; set; }

        public InstrumentsService(InstrumentsManager instrumentsManager)
        {
            _instrumentsManager = instrumentsManager;
            AttenuatorRudat = new AttenuatorRudatInstrumentService(instrumentsManager);
            PsuE364x = new PsuE364xInstrumentService(instrumentsManager);
            Dmm7510 = new Dmm7510InstrumentService(instrumentsManager);
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            _instrumentsManager.Log(text, logLevel);
        }
    }
}
