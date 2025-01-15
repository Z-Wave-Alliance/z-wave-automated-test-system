// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zats.Services
{
    public partial class NodeItem
    {
        public NodeItem()
        {

        }

        public NodeItem(byte nodeId, byte capability, byte basic, byte generic, byte specific, byte[] commandClasses, byte[] secureCommandClasses)
        {

            NodeId = nodeId;
            Capability = capability;
            Basic = basic;
            Generic = generic;
            Specific = specific;
            CommandClasses = commandClasses;
            SecureCommandClasses = secureCommandClasses;

            //GenericDevice gd = ControllerManager.Instance.ZWaveDefinition.GenericDevices.FirstOrDefault(p => p.KeyId == Generic);
            //if (gd != null)
            //{
            //    NodeType = (String.IsNullOrEmpty(gd.Text)) ? gd.Name : gd.Text;
            //}
            //else
            //{
            //    NodeType = "Generic Device:" + Tools.GetHex(generic);
            //}
        }

        public override string ToString()
        {
            if (NodeId != 0)
            {
                if (!String.IsNullOrEmpty(NodeType))
                {
                    return String.Format("{0} - {1}", NodeId.ToString(), NodeType);
                }
                else
                {
                    return String.Format("Node {0}", NodeId.ToString());
                }
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
