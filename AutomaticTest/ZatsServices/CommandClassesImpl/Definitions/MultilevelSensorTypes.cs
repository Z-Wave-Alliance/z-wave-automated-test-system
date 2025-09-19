// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Z-Wave Alliance https://z-wavealliance.org
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using ZatsServices_netcore.CommandClassesImpl.Definitions.Interfaces;

namespace Zats.Services
{
    public class MultilevelSensorType : INestedItem, INodeItem
    {
        private static string sensorTypesJsonFile { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
            Path.Combine("CommandClassesImpl\\Definitions", "MultilevelSensorTypes.json"));

        public string Name { get; set; }
        public int NestedId { get; set; }
        public int MinVersion;

        public List<MultilevelSensorScale> Scales { get; set; }

        [JsonIgnore]
        public IEnumerable<INestedItem> ChildItems => Scales;
        [JsonIgnore]
        public string HexId => $"0x{NestedId:X2}";

        public MultilevelSensorType(string name, int id, List<MultilevelSensorScale> scales, int minVersion = 0)
        {
            Name = name;
            NestedId = id;
            MinVersion = minVersion;
            Scales = scales;
        }

        public static List<MultilevelSensorType> GetAllSupportedTypesAndScales()
        {
            List<MultilevelSensorType> allSupportedTypesAndScales = null;
            try
            {
                allSupportedTypesAndScales = JsonUtils.Load<List<MultilevelSensorType>>(sensorTypesJsonFile);
            }
            catch
            {
                allSupportedTypesAndScales = new List<MultilevelSensorType>();
            }
            return allSupportedTypesAndScales;
        }
    }

    public class MultilevelSensorScale : INestedItem
    {
        public string Name { get; set; }
        public int NestedId { get; set; }
        public int MinVersion;
        public double Value { get; set; }
        public byte Precision { get; set; }

        [JsonIgnore]
        public string HexId => $"0x{NestedId:X2}";

        public MultilevelSensorScale(string name, int id, int minVersion = 0)
        {
            Name = name;
            NestedId = id;
            MinVersion = minVersion;
        }
    }

}
