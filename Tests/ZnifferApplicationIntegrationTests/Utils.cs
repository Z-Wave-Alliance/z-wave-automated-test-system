// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
// SPDX-FileCopyrightText: Z-Wave Alliance <https://z-wavealliance.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZWave.ZnifferApplication;
using ZWave.Xml.FrameHeader;
using System.IO;

namespace ZnifferApplicationIntegrationTests
{
    public static class Utils
    {
        private const string ZWAVE_DEFINITION_FILE = global::Utils.Configuration.ZWAVE_DEFINITION_FILE_NAME;
        private const string FRAME_DEFINITION_FILE = global::Utils.Configuration.FRAME_DEFINITION_FILE_NAME;

        public static FrameDefinition FrameDefinition { get; set; }

        private static FrameDefinition LoadFrameDefinition()
        {
            FrameDefinition ret = null;
            string zWaveXmlRoot = FindDirectory2(AppDomain.CurrentDomain.BaseDirectory);
            if (zWaveXmlRoot != null &&
                File.Exists(zWaveXmlRoot + FRAME_DEFINITION_FILE))
            {
                ret = FrameDefinition.Load(zWaveXmlRoot + FRAME_DEFINITION_FILE);
            }
            return ret;
        }

        private static string FindDirectory2(string childLeaf)
        {
            string zatsXmlRoot = childLeaf;
            if (!zatsXmlRoot.EndsWith(@"\") && !zatsXmlRoot.EndsWith(@"/"))
            {
                zatsXmlRoot += @"\";
            }

            if (!Directory.Exists(zatsXmlRoot + @"z-wave-tools-core\XmlFiles\"))
            {
                try
                {
                    zatsXmlRoot = Directory.GetParent(childLeaf).FullName;
                    return FindDirectory2(zatsXmlRoot);
                }
                catch (Exception)
                {
                    return null;
                }
            }
            else
            {
                return zatsXmlRoot + @"z-wave-tools-core\XmlFiles\";
            }
        }

        public static FrameDefinition InitFrameDefinition()
        {
            if (FrameDefinition == null)
            {
                FrameDefinition = LoadFrameDefinition();
                HeaderStore.Headers = new Dictionary<byte, Header>();
                foreach (var item in FrameDefinition.Header)
                {
                    HeaderStore.Headers.Add(item.Key, item);
                }
            }
            return FrameDefinition;
        }
    }
}
