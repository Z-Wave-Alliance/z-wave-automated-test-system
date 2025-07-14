// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using Utils;
using ZWave.Xml.Application;

namespace Zats.Services
{
    public class LogRecordParser
    {
        public class LogRecordParserResult
        {
            public byte[] Command { get; private set; }
            public string CommandName { get; private set; }
            public byte[] Payload { get; private set; }
            public SecuritySchemes SecurityScheme { get; private set; }

            public LogRecordParserResult(byte[] command, string commandName, byte[] payload, SecuritySchemes securityScheme)
            {
                Command = command;
                CommandName = commandName;
                Payload = payload;
                SecurityScheme = securityScheme;
            }
        }

        const string RX = "Rx";
        const string PLUS = "+";
        const string OPEN_BRACE = "(";
        const string CLOSE_BRACE = ")";
        const string SECURITY_TAG_OPEN = "[";
        const string SECURITY_TAG_CLOSE = "]";
        const string SECURE_S0 = "S0";
        const string SECURE_S2_ACCESS = "S2_ACCESS";
        const string SECURE_S2_AUTH = "S2_AUTH";
        const string SECURE_S2_UNAUTH = "S2_UNAUTH";
        const string SECURE_S2_TEMP = "S2_TEMP";

        private readonly ZWaveDefinition _zWaveDefinition;

        public LogRecordParser(ZWaveDefinition zWaveDefinition)
        {
            _zWaveDefinition = zWaveDefinition;
        }

        public LogRecordParserResult Parse(string logRecord)
        {
            if (string.IsNullOrEmpty(logRecord))
            {
                throw new ArgumentException("logRecord");
            }

            int begin = logRecord.IndexOf(RX);
            if (begin == -1)
            {
                throw new ArgumentException("broken log record");
            }
            begin += RX.Length;

            SecuritySchemes securityScheme = SecuritySchemes.NONE;
            int securTag = logRecord.IndexOf(SECURITY_TAG_OPEN, begin);
            int end = 0;
            if (securTag != -1)
            {
                begin = securTag;
                end = logRecord.IndexOf(SECURITY_TAG_CLOSE, begin);
                if (end == -1)
                {
                    throw new ArgumentException("broken log record");
                }
                begin += SECURITY_TAG_OPEN.Length;
                var securitySchemeString = logRecord.Substring(begin, end - begin);
                if (string.Compare(SecuritySchemes.S0.ToString(), securitySchemeString, StringComparison.InvariantCulture) == 0)
                {
                    securityScheme = SecuritySchemes.S0;
                }
                else if (string.Compare(SecuritySchemes.S2_ACCESS.ToString(), securitySchemeString, StringComparison.InvariantCulture) == 0)
                {
                    securityScheme = SecuritySchemes.S2_ACCESS;
                }
                else if (string.Compare(SecuritySchemes.S2_AUTHENTICATED.ToString(), securitySchemeString, StringComparison.InvariantCulture) == 0)
                {
                    securityScheme = SecuritySchemes.S2_AUTHENTICATED;
                }
                else if (string.Compare(SecuritySchemes.S2_UNAUTHENTICATED.ToString(), securitySchemeString, StringComparison.InvariantCulture) == 0)
                {
                    securityScheme = SecuritySchemes.S2_UNAUTHENTICATED;
                }
                else if (string.Compare(SecuritySchemes.S2_TEMP.ToString(), securitySchemeString, StringComparison.InvariantCulture) == 0)
                {
                    securityScheme = SecuritySchemes.S2_TEMP;
                }
                else
                {
                    throw new ArgumentException("broken log record");
                }
                begin = end + SECURITY_TAG_CLOSE.Length;
            }
            
            end = logRecord.IndexOf(OPEN_BRACE, begin);
            if (end == -1)
            {
                throw new ArgumentException("broken log record");
            }
            var cmdName = logRecord.Substring(begin, end - begin);
            cmdName = cmdName.Trim();
            begin = end + OPEN_BRACE.Length;
            end = logRecord.IndexOf(CLOSE_BRACE, begin);
            if (end == -1)
            {
                throw new ArgumentException("broken log record");
            }
            var cmdBytesString = logRecord.Substring(begin, end - begin);
            var command = Tools.GetBytes(cmdBytesString);

            int plus = logRecord.IndexOf(PLUS, end);
            byte[] payload = null;
            if (plus != -1)
            {
                plus += PLUS.Length;
                var payloadString = logRecord.Substring(plus, logRecord.Length - plus);
                payloadString = payloadString.Trim();
                payload = Tools.GetBytes(payloadString);
            }
            return new LogRecordParserResult(command, cmdName, payload, securityScheme);
        }
    }
}
