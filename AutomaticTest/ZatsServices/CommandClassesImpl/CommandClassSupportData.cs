// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: 2025 Z-Wave Alliance https://z-wavealliance.org
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zats.Services
{
    [JsonConverter(typeof(SupportDataConverter))]
    public class CommandClassSupportData
    {
        static readonly Dictionary<Type, string> typeToSubType;
        static readonly Dictionary<string, Type> subTypeToType;

        static CommandClassSupportData()
        {
            typeToSubType = new Dictionary<Type, string>();

            List<Type> typeList = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(domainAssembly => domainAssembly.GetTypes())
                    .Where(type => typeof(CommandClassSupportData).IsAssignableFrom(type)
                    ).ToList();

            foreach (Type type in typeList)
            {
                if (!typeToSubType.ContainsKey(type))
                {
                    typeToSubType.Add(type, type.Name);
                }
            }
            subTypeToType = typeToSubType.ToDictionary(pair => pair.Value, pair => pair.Key);
        }

        public static Type GetType(string subType)
        {
            return subTypeToType[subType];
        }

        public string Type { get { return typeToSubType[GetType()]; } }
        public int LogLevel { get; set; } = 0;

    }

    public class SupportDataConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(CommandClassSupportData);
        }

        public override bool CanWrite { get { return false; } }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            var typeToken = token["Type"];
            if (typeToken == null)
                throw new InvalidOperationException("invalid object");
            var actualType = CommandClassSupportData.GetType(typeToken.ToObject<string>(serializer));
            if (existingValue == null || existingValue.GetType() != actualType)
            {
                var contract = serializer.ContractResolver.ResolveContract(actualType);
                existingValue = contract.DefaultCreator();
            }
            using (var subReader = token.CreateReader())
            {
                // Using "populate" avoids infinite recursion.
                serializer.Populate(subReader, existingValue);
            }
            return existingValue;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Delegate for registering a cancel-method of a transient operation.
    /// It is used to skip pending operations of SupportImpls which are using the same data class. (mapped EPs)
    /// </summary>
    public delegate void CancelPendingOperationDelegate();
}
