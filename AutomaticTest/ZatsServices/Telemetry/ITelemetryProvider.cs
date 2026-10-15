// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
namespace Zats.Services.Telemetry
{
    public interface ITelemetryProvider
    {
        void BuildProvider(string rootActivityName, string[] sourceNames);
        ITelemetryClient CreateTelemetryClient(string name);
    }

    public interface ITelemetryClient
    {
        void StartActivity(int actionId, string name);
        void StopActivity(int actionId);
        void StopErrorActivity(int actionId, string message);
    }
}
