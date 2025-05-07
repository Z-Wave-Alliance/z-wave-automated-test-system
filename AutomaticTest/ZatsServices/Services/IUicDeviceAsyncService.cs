using System;
using System.Collections.Generic;
using ZWave.UicApplication.PayloadWrappers.Enums;
using static Zats.Services.UicService;

namespace Zats.Services
{
    public interface IUicDeviceAsyncService
    {
        void SetDefault(Alias uicDeviceNumber, int timeoutMs);
        void SetLearnMode(Alias uicEndDeviceNumber, int timeoutMs);
        string GetDsk(Alias uicEndDeviceNumber, int timeoutMs);
        void TestFunction(Alias uicEndDeviceNumber, int timeoutMs);
    }
}
