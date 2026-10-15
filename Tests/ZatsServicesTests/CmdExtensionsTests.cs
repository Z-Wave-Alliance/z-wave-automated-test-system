// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ZatsServicesTests
{
    [TestFixture]
    public class CmdExtensionsTests
    {
        [Test]
        public void NODE_INFO_CACHED_REPORT_set_commandClass_nonsecure_valid()
        {
            var cmd = new Zats.Services.COMMAND_CLASS_NETWORK_MANAGEMENT_PROXY.NODE_INFO_CACHED_REPORT();
            Assert.AreEqual(0, cmd.commandClass.Count);
            Assert.AreEqual(0, cmd.nonSecureCommandClass.Count);
            Assert.AreEqual(0, cmd.securityScheme0CommandClass.Count);

            cmd.commandClass = new List<byte>(new byte[] { 0x00, 0x02, 0x00, 0xF1 });
            Assert.AreEqual(4, cmd.commandClass.Count);
            Assert.AreEqual(4, cmd.nonSecureCommandClass.Count);
            Assert.AreEqual(0, cmd.securityScheme0CommandClass.Count);

            cmd.commandClass = new List<byte>(new byte[] { 0x00, 0x02, 0x00, 0xF1, 0x00 });
            Assert.AreEqual(5, cmd.commandClass.Count);
            Assert.AreEqual(3, cmd.nonSecureCommandClass.Count);
            Assert.AreEqual(0, cmd.securityScheme0CommandClass.Count);

            cmd.commandClass = new List<byte>(new byte[] { 0x00, 0x02, 0x00, 0xF1, 0xF1, 0x00 });
            Assert.AreEqual(6, cmd.commandClass.Count);
            Assert.AreEqual(4, cmd.nonSecureCommandClass.Count);
            Assert.AreEqual(0, cmd.securityScheme0CommandClass.Count);

            cmd.commandClass = new List<byte>(new byte[] { 0x00, 0x02, 0x00, 0xF1, 0xF1, 0x00, 0x03, 0x04 });
            Assert.AreEqual(8, cmd.commandClass.Count);
            Assert.AreEqual(4, cmd.nonSecureCommandClass.Count);
            Assert.AreEqual(2, cmd.securityScheme0CommandClass.Count);

            cmd.commandClass = new List<byte>(new byte[] { 0x00, 0xEF, 0x00, 0xF1, 0xF1, 0x00, 0xEF, 0x04 });
            Assert.AreEqual(8, cmd.commandClass.Count);
            Assert.AreEqual(3, cmd.nonSecureCommandClass.Count);
            Assert.AreEqual(1, cmd.securityScheme0CommandClass.Count);

        }
    }
}
