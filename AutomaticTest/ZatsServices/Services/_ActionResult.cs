// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System.Collections.Generic;
using ZWave;
using Utils.Threading;

namespace Zats.Services
{
    public class _ActionResult
    {
        public int ElapsedMs { get; set; }
        private bool IsStateCompleted { get; set; }
        public List<_TraceLogItem> TraceLog { get; private set; }

        public _ActionResult()
        {
            
        }

        public _ActionResult(bool isStateCompleted)
        {
            Init(isStateCompleted, null);
        }

        public _ActionResult(ActionResult res)
        {
            Init(res.IsStateCompleted, res.TraceLog);
        }

        public void Init(bool isStateCompleted, ConcurrentList<TraceLogItem> traceLog)
        {
            IsStateCompleted = isStateCompleted;
            TraceLog = new List<_TraceLogItem>();
            if (traceLog != null)
            {
                foreach (var item in traceLog.ToArray())
                {
                    TraceLog.Add(new _TraceLogItem(item));
                }
            }
        }

        public void Init(bool isStateCompleted, int elapsedMs, ConcurrentList<TraceLogItem> traceLog)
        {
            IsStateCompleted = isStateCompleted;
            ElapsedMs = elapsedMs;
            TraceLog = new List<_TraceLogItem>();
            if (traceLog != null)
            {
                foreach (var item in traceLog.ToArray())
                {
                    TraceLog.Add(new _TraceLogItem(item));
                }
            }
        }

        public static implicit operator bool(_ActionResult val)
        {
            return val.IsStateCompleted;
        }
    }
}
