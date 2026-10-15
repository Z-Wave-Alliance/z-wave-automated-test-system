// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using NUnit.Framework;
using Zats.Services;

namespace ZatsServicesTests
{
    [TestFixture]
    public class MappingTests
    {
        [Test]
        public void CreateDeviceMapping_DevkitAsString_Valid()
        {
            // Arrange
            DeviceMappingAttribute a = new DeviceMappingAttribute(
                1, 
                ChipSeries.ZW050x,
                DevKits.v6_51,
                LibTypes.ControllerBridgeLib)
            { FileNameFilter = "_01" };
            // Act
            // Assert
            NUnit.Framework.Assert.AreEqual(DevKits.v6_51, a.DevKit);
        }
    }
}
