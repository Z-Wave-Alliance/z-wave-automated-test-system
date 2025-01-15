// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZWave.BasicApplication;

namespace Zats.Services
{
    public class ApplicationToken
    {
        public class Supervision : _ActionToken
        {
            SupervisionReportTask _task;
            public Supervision(SupervisionReportTask task)
            {
                _task = task;
            }

            public void SetSupervisionReportCallbacks(Dictionary<byte, Action<COMMAND_CLASS_SUPERVISION.SUPERVISION_GET, COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT>> callbacks)
            {
                if (_task != null && callbacks != null)
                {
                    _task.ReceiveAchDataCallback = (x) =>
                    {
                        COMMAND_CLASS_SUPERVISION.SUPERVISION_GET cmd = x.Command;
                        var data = new COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT()
                        {
                            duration = 0,
                            properties1 = new COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT.Tproperties1()
                            {
                                sessionId = cmd.properties1.sessionId
                            },
                        };

                        if (callbacks.ContainsKey(cmd.encapsulatedCommand[0]))
                        {
                            callbacks[cmd.encapsulatedCommand[0]](cmd, data);
                        }
                        else
                        {
                            data.status = 0x00;          // NO_SUPPORT
                        }
                        return data;
                    };
                }
                else
                {
                    _task.ReceiveAchDataCallback = null;
                }
            }
        }
    }
}
