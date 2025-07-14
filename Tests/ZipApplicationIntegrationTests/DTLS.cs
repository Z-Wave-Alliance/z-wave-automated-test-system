// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using NUnit.Framework;
using ZWave.CommandClasses;
using ZWave.ZipApplication;

namespace ZipApplicationIntegrationTests
{
    [TestFixture]
    public class DTLS : ZipTestsBase
    {
        [Test]
        public void TestMethod1()
        {
            IDtlsClient client = new DtlsClient();
            client.Connect(PSK, ZIP_ADDR, ZIP_DTLS_PORT);

            COMMAND_CLASS_ZIP_ND.ZIP_INV_NODE_SOLICITATION cmd = new COMMAND_CLASS_ZIP_ND.ZIP_INV_NODE_SOLICITATION();
            cmd.nodeId = 0x02;
            byte[] cmdData = cmd;
            client.Send(cmdData);
            client.Close();
        }
    }
}

