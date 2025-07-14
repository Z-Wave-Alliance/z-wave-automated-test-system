// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Utils;
using NUnit.Framework;

namespace ZipTransportIntegrationTests
{
    [SetUpFixture]
    public class ConfigLoader
    {
        private static SourcesProfile _sourcesProfile;
        public static SourcesProfile SourcesProfile { get { return _sourcesProfile; } }

        [SetUp]
        public void LoadConfig()
        {
            _sourcesProfile = DevicesSourceConfig.LoadIntegrationTestsConfig();
        }
    }
}
