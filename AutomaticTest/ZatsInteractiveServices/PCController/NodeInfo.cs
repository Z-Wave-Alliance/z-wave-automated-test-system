// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
namespace Zats.Services
{
    public class NodeInfo
    {
        public byte Id { get; private set; } //TODO: review to type Node 
        public byte EndPointId { get; private set; }
        public string Name { get; private set; }
        public bool? IsListening { get; private set; }
        public bool? IsVirtual { get; private set; }
        public bool? IsQO { get; private set; }
        public AddRemoveNodeStatuses AddRemoveNodeStatus { get; private set; }

        public NodeInfo(byte id, string name, bool? isListening, bool? isVirtual, bool? isQO, AddRemoveNodeStatuses addRemoveNodeStatus, byte endPointId = 0)
        {
            Id = id;
            EndPointId = endPointId;
            Name = name;
            IsListening = isListening;
            IsVirtual = isVirtual;
            IsQO = isQO;
            AddRemoveNodeStatus = addRemoveNodeStatus;
        }
    }
}
