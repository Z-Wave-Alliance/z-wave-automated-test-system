using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZWave.Enums;

namespace Zats.Services
{
    public interface IZipVerificationService
    {
        List<byte> PanNIF_SecCC();
        List<byte> PanNIF_NoSecCC();
        List<byte> S2ModeLanNIF_SecCC();
        List<byte> S2ModeLanNIF_NoSecCC();
        List<byte> NoSecModeLanNIF_SecCC();
        List<byte> NoSecModeLanNIF_NoSecCC();
        List<byte> NoSecAppendedCC();
        List<byte> SecAppendedCC();
        List<byte> NoSecMode_NoSecAppendedCC();
        List<byte> NoSecMode_SecAppendedCC();
        List<byte> S0ModeLanNIF_SecCC();
        List<byte> S0ModeLanNIF_NoSecCC();
        void GwInitVerificationValues(COMMAND_CLASS_VERSION.VERSION_REPORT GwVersionReport);
    }
}
