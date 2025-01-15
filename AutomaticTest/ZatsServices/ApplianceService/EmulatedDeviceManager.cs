// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using ZWave.Enums;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System;
using System.IO;
using System.Reflection;

namespace Zats.Services
{
    [DataContract]
    public class InitResponseData
    {
        [DataMember(Name = "connectedDevicesCount")]
        public int ConnectedDevicesCount { get; set; }

        [DataMember(Name = "supportedLibraries")]
        public string[] SupportedLibraries { get; set; }
    }

    public class HostConfiguration
    {
        public string HostAddress { get; set; }
        public int ConnectedDevicesCount { get; set; }
        public Libraries[] SupportedLibraries { get; set; }
    }

    public class RemoteDevice
    {
        public string HostAddress { get; set; }
        public Alias DeviceAlias { get; set; }
        public Libraries[] Libraries { get; set; }
    }

    public class CommandRequest
    {
        public string MethodName { get; set; }
        public object[] Params { get; set; }
    }

    public static class DictionaryExtensionMethods
    {
        public static bool ContentEquals<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, Dictionary<TKey, TValue> otherDictionary)
        {
            if (dictionary.Count != otherDictionary.Count)
            {
                return false;
            }
            return otherDictionary.OrderBy(kvp => kvp.Key).SequenceEqual(dictionary.OrderBy(kvp => kvp.Key));
        }
    }

    public class EmulatedDeviceManager : IEmulatedDeviceManager
    {
        private readonly HttpClient _client;
        private readonly string[] _applianceHosts;
        private Dictionary<Alias, RemoteDevice> _devices = new Dictionary<Alias, RemoteDevice>();

        private string UserAgent => $"zatstestconsole/{Assembly.GetEntryAssembly().GetName().Version.ToString()}";

        public EmulatedDeviceManager(string[] applianceHosts)
        {
            _applianceHosts = applianceHosts;
            _client = new HttpClient();
            _client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        }

        public void InitDevices(List<UnknownDevice> required)
        {
            if (required.Select(req => req.DeviceAlias).Distinct().Count() != required.Count)
            {
                throw new ArgumentException("At least two devices have the same numbers IDs");
            }

            var serializer = new DataContractJsonSerializer(typeof(InitResponseData));
            List<HostConfiguration> hostsConfig = new List<HostConfiguration>();
            foreach (var host in _applianceHosts)
            {
                _client.DefaultRequestHeaders.Accept.Clear();
                _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                InitResponseData response = null;
                using (var jsonStream = _client.GetStreamAsync($"http://{host}/api/devices").Result)
                {
                    response = serializer.ReadObject(jsonStream) as InitResponseData;
                }
                if (response != null)
                {
                    hostsConfig.Add(new HostConfiguration
                    {
                        HostAddress = host,
                        ConnectedDevicesCount = response.ConnectedDevicesCount,
                        SupportedLibraries = response.SupportedLibraries.
                            Select(lib =>
                            {
                                if (Enum.TryParse(lib, out Libraries library))
                                {
                                    return library;
                                }
                                return Libraries.NoLib;
                            }).
                            Where(lib => lib != Libraries.NoLib).
                            ToArray()
                    });
                }
            }

            if (required.Count > hostsConfig.Select(config => config.ConnectedDevicesCount).Sum())
            {
                return;
            }

            bool hostFound = true;
            while (required.Count > 0 && hostFound)
            {
                var device = required.ElementAt(0);
                hostFound = false;
                foreach (var config in hostsConfig)
                {
                    if (config.ConnectedDevicesCount > 0 &&
                        device.Libraries.Intersect(config.SupportedLibraries).Count() == device.Libraries.Length)
                    {
                        _devices.Add(device.DeviceAlias, new RemoteDevice
                        {
                            HostAddress = config.HostAddress,
                            DeviceAlias = device.DeviceAlias,
                            Libraries = config.SupportedLibraries
                        });
                        config.ConnectedDevicesCount--;
                        required.RemoveAt(0);
                        hostFound = true;
                        break;
                    }
                }
            }
        }

        public bool ConnectDevices(Alias[] devicesNumbers)
        {
            var dataToPost = new Dictionary<string, Dictionary<Alias, string>>();
            var devicesToConnect = _devices.Where(dev => devicesNumbers.Contains(dev.Key));
            foreach (var device in devicesToConnect)
            {
                if (!dataToPost.ContainsKey(device.Value.HostAddress))
                {
                    dataToPost.Add(device.Value.HostAddress, new Dictionary<Alias, string>());
                }
                dataToPost[device.Value.HostAddress].Add(device.Value.DeviceAlias, device.Value.Libraries[0].ToString());
            }

            foreach (var postData in dataToPost)
            {
                var serializer = new DataContractJsonSerializer(typeof(Dictionary<int, string>),
                    new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
                string jsonContent = null;
                using (var memStream = new MemoryStream())
                {
                    serializer.WriteObject(memStream, postData.Value);
                    jsonContent = Encoding.UTF8.GetString(memStream.ToArray());
                }
                _client.DefaultRequestHeaders.Accept.Clear();
                _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = _client.PostAsync($"http://{postData.Key}/api/devices/", content).Result;
                using (var responseStream = response.Content.ReadAsStreamAsync().Result)
                {
                    var connectedDevices = serializer.ReadObject(responseStream) as Dictionary<Alias, string>;
                    if (!postData.Value.ContentEquals(connectedDevices))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public void SendRequest(Alias deviceAlias, string method, params object[] methodParams)
        {
            if (_devices.ContainsKey(deviceAlias))
            {
                var content = new StringContent(ToJsonContent(new CommandRequest { MethodName = method, Params = methodParams }), Encoding.UTF8, "application/json");
                string host = $"http://{_devices[deviceAlias].HostAddress}/api/devices/{deviceAlias}";
                _client.DefaultRequestHeaders.Accept.Clear();
                _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _client.PutAsync(host, content).Wait();
            }
        }

        public void ResetConfiguredState()
        {
            foreach (var usedHost in _devices.Values.Select(val => val.HostAddress).Distinct())
            {
                string host = $"http://{usedHost}/api/devices";
                _client.DefaultRequestHeaders.Accept.Clear();
                _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _client.DeleteAsync(host).Wait();
            }
            _devices.Clear();
        }

        public void FinalizeDevices()
        {
            ResetConfiguredState();
        }

        private string ToJsonContent<T>(T data)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            using (var memStream = new MemoryStream())
            {
                serializer.WriteObject(memStream, data);
                return Encoding.UTF8.GetString(memStream.ToArray());
            }
        }
    }
}
