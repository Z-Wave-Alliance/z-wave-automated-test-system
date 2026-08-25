// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Utils;

namespace Zats.Services.SampleApplication
{
    /// <summary>
    /// Silink application provider.
    /// </summary>
    public class SilinkApplicationProvider : IDisposable
    {
        private readonly string _silinkAppPath;
        private Process _process;
        private bool _isActive = false;
        private string _serialNumber;
        private string _address;
        public string Address => _address;
        private int _port;
        public int Port => _port;
        public int VUART0 { get; private set; }
        public int VCOM0 { get; private set; }
        public int ADM_CONSOLE { get; private set; }
        public int VDCHUART0 { get; private set; }

        public SilinkApplicationProvider(string silinkAppPath)
        {
            _silinkAppPath = silinkAppPath;
            if (!File.Exists(_silinkAppPath))
            {
                throw new FileNotFoundException($"by path = {_silinkAppPath}");
            }
        }

        public void Dispose()
        {
            if (_process != null)
            {
                var pid = _process.Id;
                try
                {

                    _address = string.Empty;
                    _process.StandardInput.WriteAsync("quit");
                    Thread.Sleep(100);
                    _process.Kill();
                }
                catch (Exception ex)
                {
                    $"Silink application (PID = {pid}), got into exception on close localhost:{ADM_CONSOLE} for the SN:{_serialNumber}"._DLOG();
                    ex.Message._DLOG();
                }
                finally
                {
                    _isActive = false;
                    _process = null;
                    $"Silink application closed localhost:{ADM_CONSOLE} for the SN:{_serialNumber}"._DLOG();

                }
                Thread.Sleep(200);
            }
        }

        public bool Connect(string serialNumber, int port)
        {
            _serialNumber = serialNumber;
            _port = port;
            if (_isActive && _process != null) return true;

            string args = $" -sn {serialNumber} -automap {_port}";
            _process = new Process();
            _process.StartInfo.FileName = _silinkAppPath;
            _process.StartInfo.Arguments = args;
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.RedirectStandardInput = true;
            _process.StartInfo.CreateNoWindow = true;
            if (_process.Start())
            {
                $"Silink application (PID = {_process.Id}) started on localhost:{_port} for the SN:{serialNumber}"._DLOG();
                _isActive = ParseOutput(_process.StandardOutput);
            }
            else
            {
                $"Failed to started Silink with arg = {args}"._DLOG();
            }
            return _isActive;
        }

        /// <summary>
        /// Handle startup output and fill connection ports from it.
        /// </summary>
        private bool ParseOutput(StreamReader output)
        {
            _address = "localhost";
            var mapping = new Regex(@"Mapping channel (\w+) to port (\d+)");
            var maxReadLines = 20;
            while (maxReadLines-- > 0)
            {
                var readline = output.ReadLine();
                WirteOutputLog(readline);
                if (string.IsNullOrEmpty(readline))
                    continue;

                var match = mapping.Match(readline);
                if (match.Success)
                {
                    var port = int.Parse(match.Groups[2].Value);
                    switch (match.Groups[1].Value)
                    {
                        case "VUART0":
                            VUART0 = port;
                            break;
                        case "VCOM0":
                            VCOM0 = port;
                            break;
                        case "ADM_CONSOLE":
                            ADM_CONSOLE = port;
                            break;
                        case "DCH":
                        case "DATA_CHANNEL":
                            VDCHUART0 = port;
                            break;
                    }
                    continue;
                }

                if (readline.StartsWith("Serial number:"))
                    break;
            }

            return VUART0 > 0 && VCOM0 > 0 && ADM_CONSOLE > 0 && VDCHUART0 > 0;
        }

        private static void WirteOutputLog(string readline)
        {
            $"Silink application readline: {readline}"._DLOG();
        }
    }
}
