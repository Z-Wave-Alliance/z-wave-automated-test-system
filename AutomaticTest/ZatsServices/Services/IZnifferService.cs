// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
// SPDX-FileCopyrightText: Z-Wave Alliance <https://z-wavealliance.org>
using ZWave.ZnifferApplication.Enums;

namespace Zats.Services
{
    public interface IZnifferService
    {
        ZnifferToken.CollectData CollectData();
        ZnifferToken.CollectData CollectData(Alias znifferDevice);
        ZnifferToken.CollectData CollectData(int timeoutMs);
        ZnifferToken.CollectData CollectData(Alias znifferDevice, int timeoutMs);
        ZnifferToken.ExpectData ExpectData(int timeoutMs);
        ZnifferToken.ExpectData ExpectData(Alias znifferDevice, int timeoutMs);
        void SetSecurityNetworkKey(byte[] networkKey);
        void SetSecurityNetworkKey(byte[] networkKey, bool isTemp);
        void SetSecurityNetworkKeyS2(byte[] networkKeyS2);
        bool IsConnected();
        bool SetLRChannelConfig(ZnifferChannelConfig channelConfig);
        void SetSecuritySupport(bool isSecure);
        ServiceSettings Settings { get; set; }
        void CancelToken(_ActionToken token);
        void RestartListeners();
    }
}
