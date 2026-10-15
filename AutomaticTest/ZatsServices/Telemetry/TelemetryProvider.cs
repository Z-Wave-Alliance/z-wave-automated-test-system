// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Reflection;
#if OTEL
using Utils;
using System.Collections.Concurrent;
using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
#endif
namespace Zats.Services.Telemetry
{
    public class TelemetryProvider : ITelemetryProvider, IDisposable
    {
        private readonly List<string> _sources = new List<string>();
#if OTEL
        private readonly List<ConcurrentDictionary<int, Activity>> _activitiesList = new List<ConcurrentDictionary<int, Activity>>();
        private readonly List<ActivitySource> _activitiySourcesList = new List<ActivitySource>();
        private TracerProvider _traceProvider;
        private readonly ActivitySource _rootActivitySource;
        private Activity _rootActivity;
#endif
        public TelemetryProvider()
        {
            var sourceName = Assembly.GetExecutingAssembly().GetName().Name;
            _sources.Add(sourceName);
#if OTEL
            _rootActivitySource = new ActivitySource(sourceName);
#endif
        }

        //private HttpClient CreateHttpClient()
        //{
        //    var handler = new HttpClientHandler();
        //    handler.ClientCertificateOptions = ClientCertificateOption.Manual;
        //    handler.SslProtocols = SslProtocols.Tls13;
        //    handler.CheckCertificateRevocationList = false;
        //    handler.ClientCertificates.Add(new X509Certificate2(@"C:\work\silabs_net.cer"));
        //    var client = new HttpClient(handler);
        //    return client;
        //}

        public ITelemetryClient CreateTelemetryClient(string name)
        {
#if OTEL
            ActivitySource activitySource = new ActivitySource(name);
            _activitiySourcesList.Add(activitySource);
            var activities = new ConcurrentDictionary<int, Activity>();
            _activitiesList.Add(activities);
            TelemetryClient client = new TelemetryClient(activitySource, _rootActivity, activities);
            return client;
#else
            return null;
#endif
        }

        public void BuildProvider(string rootActivityName, string[] sourceNames)
        {
            _sources.AddRange(sourceNames);
#if OTEL
            _traceProvider = Sdk.CreateTracerProviderBuilder()
                   .AddSource(_sources.ToArray())
                   .SetResourceBuilder(ResourceBuilder.CreateDefault().AddTelemetrySdk().AddEnvironmentVariableDetector())
                   .AddOtlpExporter()
                   .Build();

            _rootActivity = _rootActivitySource.StartActivity(rootActivityName);
#endif
        }

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
#if OTEL
                    foreach (var item in _activitiesList)
                    {
                        foreach (var itm in item)
                        {
                            itm.Value?.Dispose();
                        }
                        item.Clear();
                    }
                    _activitiesList.Clear();
                    _rootActivity?.Dispose();
                    foreach (var item in _activitiySourcesList.ToArray())
                    {
                        item?.Dispose();
                    }
                    _activitiySourcesList.Clear();
                    _rootActivitySource?.Dispose();
                    _traceProvider?.Dispose();
#endif
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }

    public class TelemetryClient : ITelemetryClient
    {
#if OTEL
        private readonly ConcurrentDictionary<int, Activity> _activities;
        private readonly ActivitySource _activitySource;
        private readonly Activity _rootActivity;
#endif
        private TelemetryClient()
        {

        }
#if OTEL
        internal TelemetryClient(ActivitySource activitySource, Activity rootActivity, ConcurrentDictionary<int, Activity> activities)
        {
            _activitySource = activitySource;
            _rootActivity = rootActivity;
            _activities = activities;
        }
#endif
        public void StartActivity(int actionId, string name)
        {
#if OTEL
            var activity = _activitySource.StartActivity(name);
            if (activity != null)
            {
                activity.AddEvent(new ActivityEvent("activity started"));
                activity.SetParentId(_rootActivity.TraceId, _rootActivity.SpanId, ActivityTraceFlags.Recorded);
                if (!_activities.TryAdd(actionId, activity))
                {
                    StopActivity(actionId);
                    if (!_activities.TryAdd(actionId, activity))
                    {
                        "unexpected behavior"._DLOG();
                    }
                }
            }
#endif
        }

        public void StopActivity(int actionId)
        {
#if OTEL
            _activities.TryRemove(actionId, out Activity activity);
            if (activity != null)
            {
                activity.AddEvent(new ActivityEvent("activity completed"));
                activity.SetStatus(ActivityStatusCode.Ok);
                activity.Dispose();
            }
#endif
        }

        public void StopErrorActivity(int actionId, string message)
        {
#if OTEL
            _activities.TryRemove(actionId, out Activity activity);
            if (activity != null)
            {
                activity.AddEvent(new ActivityEvent("activity expired"));
                activity.SetStatus(ActivityStatusCode.Error, message);
                activity.Dispose();
            }
#endif
        }
    }
}
