using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zats.Services
{
    public interface ILibzwaveipService
    {
        IProvisioningList ProvisioningList { get; }
        ReciveDataPacket SetDefaultGw(Alias ZipController);
        ReciveDataPacket AddNodeWithDSK(Alias ZipController, Alias JoinNode);
        ReciveDataPacket AddNodeNonSecurity(Alias ZipController, Alias JoinNode);
        ReciveDataPacket AddNodeWithGrantkeys(Alias ZipController, Alias JoinNode, GrantKeyType[] grantkeys);
        ReciveDataPacket AddNodeStop(Alias ZipController);
        ReciveDataPacket RemoveNode(Alias ZipController, Alias Device);
        ReciveDataPacket RemoveNodeStop(Alias ZipController);
        void Learnmode(Alias ZipController);
        void LearnmodeStop(Alias ZipController);
        List<string> List(Alias ZipContoller);
        List<string> SendCommand(Alias ZipController, byte TargetNodeId, string CommandClass, string Command);
        List<string> SendCommand(string Data);
        void Hexsend(Alias ZipController, byte TargetNodeId, byte[] SendDataPacket);
        void Pl_Reset(Alias ZipController);
        List<string> Pl_List(Alias ZipController);
        void Pl_Add_DSK(Alias ZipController, byte[] JoinDsk);
        void Pl_Add_DSK(Alias ZipController, byte[] JoinDsk, string name, string location, string bootmode, string smartstart);
        void Pl_Remove_DSK(Alias ZipController, byte[] JoinDsk);
        void NodeInfo(Alias ZipController, byte ControllerNodeId, byte TargetNodeId);
        ReciveDataPacket ReciveData(byte[] ExpectData);
        void StartReferenceListener(Alias ZipController, int TimeOut);
        void StopReferenceListener();
        List<byte[]> GetListenerData();
        void LifeLine(Alias ZipController, byte ReportDeviceNodeId, byte ReciveDeviceNodeId);
        string SetUnsoliToGwCfg(Alias ZipController);
    }
}
