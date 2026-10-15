// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Utils;
using NUnit.Framework;

namespace ZnifferApplicationIntegrationTests
{
    [SetUpFixture]
    public class ConfigLoader
    {
        private static SourcesProfile _sourcesProfile;
        public static SourcesProfile SourcesProfile { get { return _sourcesProfile; } }

        [OneTimeSetUp]
        public void LoadConfig()
        {
            _sourcesProfile = DevicesSourceConfig.LoadIntegrationTestsConfig();
        }
    }
}
