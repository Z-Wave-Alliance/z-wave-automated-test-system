// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Mesh Route")]
    [DeviceMapping(1, LibTypes.Controller)]
    [DeviceMapping(2, LibTypes.EndDeviceAny)]
    [DeviceMapping(3, LibTypes.EndDeviceAny)]
    [DeviceMapping(4, LibTypes.EndDeviceAny)]
    [MeshNetworkRouterMapping(5, "r1")]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class appmesh_RouteSendData : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias NodeA = new Alias(2);
            Alias NodeB = new Alias(3);
            Alias NodeC = new Alias(4);
            Alias Router = new Alias(5);

            Service.SetDefault(Primary);
            Service.SetDefault(NodeA);
            Service.SetDefault(NodeB);
            Service.SetDefault(NodeC);
            NetworkRouterService.SetChannels(Router, DbLevels.db0);

            Service.AddNode(Primary, NodeA);
            Service.AddNode(Primary, NodeB);
            Service.AddNode(Primary, NodeC);

            BasicResult.SendData res = null;
            ReportService.LogTitle("Enable All channels");

            res = Service.SendData(NodeB, NodeA, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            ReportService.LogInfo($"A-B send data: status:{res.TransmitStatus} route schema:{res.RouteScheme} repeaters:{res.Repeaters.ToHexString()}");
            res = Service.SendData(NodeC, NodeB, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            ReportService.LogInfo($"A-C send data: status:{res.TransmitStatus} route schema:{res.RouteScheme} repeaters:{res.Repeaters.ToHexString()}");
            res = Service.SendData(NodeC, NodeA, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            ReportService.LogInfo($"B-C send data: status:{res.TransmitStatus} route schema:{res.RouteScheme} repeaters:{res.Repeaters.ToHexString()}");

            ReportService.LogTitle("Enable channels A-B");

            NetworkRouterService.SetChannels(Router, DbLevels.db95);
            NetworkRouterService.SetChannel(Router, NodeA, NodeB, DbLevels.db0);
            res = Service.SendData(NodeA, NodeB, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            ReportService.LogInfo($"A-B send data: status:{res.TransmitStatus} route schema:{res.RouteScheme} repeaters:{res.Repeaters.ToHexString()}");
            res = Service.SendData(NodeA, NodeC, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            ReportService.LogInfo($"A-C send data: status:{res.TransmitStatus} route schema:{res.RouteScheme} repeaters:{res.Repeaters.ToHexString()}");
            res = Service.SendData(NodeB, NodeC, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            ReportService.LogInfo($"B-C send data: status:{res.TransmitStatus} route schema:{res.RouteScheme} repeaters:{res.Repeaters.ToHexString()}");

            ReportService.LogTitle("Enable channels A-C");

            NetworkRouterService.SetChannels(Router, DbLevels.db95);
            NetworkRouterService.SetChannel(Router, NodeA, NodeC, DbLevels.db0);
            res = Service.SendData(NodeA, NodeB, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            ReportService.LogInfo($"A-B send data: status:{res.TransmitStatus} route schema:{res.RouteScheme} repeaters:{res.Repeaters.ToHexString()}");
            res = Service.SendData(NodeA, NodeC, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            ReportService.LogInfo($"A-C send data: status:{res.TransmitStatus} route schema:{res.RouteScheme} repeaters:{res.Repeaters.ToHexString()}");
            res = Service.SendData(NodeB, NodeC, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            ReportService.LogInfo($"B-C send data: status:{res.TransmitStatus} route schema:{res.RouteScheme} repeaters:{res.Repeaters.ToHexString()}");

            ReportService.LogTitle("Enable channels B-C");

            NetworkRouterService.SetChannels(Router, DbLevels.db95);
            NetworkRouterService.SetChannel(Router, NodeB, NodeC, DbLevels.db0);
            res = Service.SendData(NodeA, NodeB, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            ReportService.LogInfo($"A-B send data: status:{res.TransmitStatus} route schema:{res.RouteScheme} repeaters:{res.Repeaters.ToHexString()}");
            res = Service.SendData(NodeA, NodeC, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            ReportService.LogInfo($"A-C send data: status:{res.TransmitStatus} route schema:{res.RouteScheme} repeaters:{res.Repeaters.ToHexString()}");
            res = Service.SendData(NodeB, NodeC, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            ReportService.LogInfo($"B-C send data: status:{res.TransmitStatus} route schema:{res.RouteScheme} repeaters:{res.Repeaters.ToHexString()}");

            ReportService.LogTitle("Make Route");
            var dbs = new[]
            {
                DbLevels.db20, DbLevels.db25, DbLevels.db30, DbLevels.db35,
                DbLevels.db40, DbLevels.db43, DbLevels.db46, DbLevels.db49,
                DbLevels.db50, DbLevels.db53, DbLevels.db56, DbLevels.db59,
                DbLevels.db60, DbLevels.db63, DbLevels.db66, DbLevels.db66,
                DbLevels.db70, DbLevels.db73, DbLevels.db76, DbLevels.db79
            };

            foreach (var db in dbs)
            {
                NetworkRouterService.SetChannels(Router, DbLevels.db95);
                NetworkRouterService.SetChannel(Router, NodeA, NodeC, db);
                NetworkRouterService.SetChannel(Router, NodeB, NodeC, db);
                res = Service.SendData(NodeB, NodeC, new COMMAND_CLASS_BASIC.BASIC_REPORT());
                res = Service.SendData(NodeC, NodeA, new COMMAND_CLASS_BASIC.BASIC_REPORT());
                ReportService.LogText($"B-A at {db} level");
                for (int i = 0; i < 2; i++)
                {
                    res = Service.SendData(NodeB, NodeA, new COMMAND_CLASS_BASIC.BASIC_REPORT());
                    ReportService.LogInfo($"send data: status:{res.TransmitStatus} route schema:{res.RouteScheme} repeaters:{res.Repeaters.ToHexString()}");
                }
            }

            foreach (var db in dbs)
            {
                NetworkRouterService.SetChannels(Router, DbLevels.db95);
                NetworkRouterService.SetChannel(Router, NodeB, NodeA, db);
                NetworkRouterService.SetChannel(Router, NodeC, NodeA, db);
                res = Service.SendData(NodeC, NodeA, new COMMAND_CLASS_BASIC.BASIC_REPORT());
                res = Service.SendData(NodeA, NodeB, new COMMAND_CLASS_BASIC.BASIC_REPORT());
                ReportService.LogText($"C-B at {db} level");
                for (int i = 0; i < 3; i++)
                {
                    res = Service.SendData(NodeC, NodeB, new COMMAND_CLASS_BASIC.BASIC_REPORT());
                    ReportService.LogInfo($"send data: status:{res.TransmitStatus} route schema:{res.RouteScheme} repeaters:{res.Repeaters.ToHexString()}");
                }
            }

            //NetworkRouterService.SetChannels(Router, DbLevels.db95);
            //NetworkRouterService.SetChannel(Router, NodeA, NodeB, DbLevels.db0);
            //NetworkRouterService.SetChannel(Router, NodeC, NodeB, DbLevels.db0);
            //res = Service.SendData(NodeA, NodeC, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            //ReportService.LogInfo($"A-C send data: status:{res.TransmitStatus} route schema:{res.RouteScheme} repeaters:{res.Repeaters.ToHexString()}");


            //NetworkRouterService.SetChannels(Router, DbLevels.db95);
            //NetworkRouterService.SetChannel(Router, NodeB, NodeA, DbLevels.db0);
            //NetworkRouterService.SetChannel(Router, NodeC, NodeA, DbLevels.db0);
            //res = Service.SendData(NodeB, NodeC, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            //ReportService.LogInfo($"B-C send data: status:{res.TransmitStatus} route schema:{res.RouteScheme} repeaters:{res.Repeaters.ToHexString()}");

        }
    }
}