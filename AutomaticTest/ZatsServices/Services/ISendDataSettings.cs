// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>

namespace Zats.Services
{
    public interface ISendDataSettings
    {
        void SetCRC16(Alias controllerNumber, bool enabled);
        void SetSupressMulticastFollowUp(Alias controllerNumber, bool enabled);
        void SetForceMultiCast(Alias controllerNumber, bool enabled);
        void SetSkipWaitingSendCallback(Alias controllerNumber, bool enabled);
        void SetSupervisionGet(Alias controllerNumber, bool enabled);
        void SetStatusUpdates(Alias controllerNumber, bool enabled);
        void SetSessionID(Alias controllerNumber, byte value);
        void SetAutoIncrement(Alias controllerNumber, bool enabled);
        void SetMultiChannel(Alias controllerNumber, bool enabled);
        void SetSrcEndPoint(Alias controllerNumber, byte enabled);
        void SetDstEndPoint(Alias controllerNumber, byte enabled);
        void SetBitAdress(Alias controllerNumber, bool enabled);
        void SetSecure(Alias controllerNumber, bool? enabled);
        void SetCallbackWaitTimeout(Alias deviceNumber, int timeoutMs);
        void SetTransportServiceMaxSize(Alias controllerNumber, int value);
        void SetMaxBytesPerFrameSize(Alias controllerNumber, int value);
        void SetResponseDelayMs(Alias controllerNumber, int timeoutMs);
        void SetRequestTimeoutMs(Alias controllerNumber, int value);

        bool GetCRC16(Alias controllerNumber);
        bool GetSupressMulticastFollowUp(Alias controllerNumber);
        bool GetForceMultiCast(Alias controllerNumber);
        bool GetSkipWaitingSendCallback(Alias controllerNumber);
        bool GetSupervisionGet(Alias controllerNumber);
        bool GetStatusUpdates(Alias controllerNumber);
        byte GetSessionID(Alias controllerNumber);
        bool GetAutoIncrement(Alias controllerNumber);
        bool GetMultiChannel(Alias controllerNumber);
        byte GetSrcEndPoint(Alias controllerNumber);
        byte GetDstEndPoint(Alias controllerNumber);
        bool GetBitAdress(Alias controllerNumber);
        bool? GetSecure(Alias controllerNumber);
        int GetTransportServiceMaxSize(Alias controllerNumber);
        int GetMaxBytesPerFrameSize(Alias controllerNumber);
        int GetResponseDelayMs(Alias controllerNumber);
        int GetRequestTimeoutMs(Alias controllerNumber);

        void Clear(Alias controllerNumber);
        int GetCallbackWaitTimeoutMs(Alias senderNumber);
    }
}
