// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace ZatsTestConsole
{
    public class ConsoleOutputDecorator
    {
        private readonly ConsoleColor[] _colores = new[] { ConsoleColor.Black,
            ConsoleColor.DarkBlue,
            ConsoleColor.DarkGreen,
            ConsoleColor.DarkCyan,
            ConsoleColor.DarkRed,
            ConsoleColor.DarkMagenta,
            ConsoleColor.DarkYellow,
            ConsoleColor.Gray,
            ConsoleColor.DarkGray,
            ConsoleColor.Blue,
            ConsoleColor.Green,
            ConsoleColor.Cyan,
            ConsoleColor.Red,
            ConsoleColor.Magenta,
            ConsoleColor.Yellow,
            ConsoleColor.White};

        public bool IsLess { get; set; }

        public bool IsOutputColorFormated { get; set; }

        public void WithColor(ConsoleColor color, string text, Action<string> action)
        {

            if (!IsLess && IsOutputColorFormated)
            {
                var output = AddColorAsAttribute(text, color);
                action.Invoke(output);
            }
            else
            {
                var oldColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                action.Invoke(text);
                Console.ForegroundColor = oldColor;
            }
        }

        public void ParseColoredText(string text, out ConsoleColor color, out string res)
        {
            res = null;
            color = ConsoleColor.Black;
            var tags = Regex.Matches(text, @"{[^}]*}");
            if (tags.Count == 2)
            {
                var splitRes = Regex.Split(text, @"{[^}]*}");
                if (splitRes.Length > 2)
                {
                    res = splitRes[1];
                    splitRes = Regex.Split(tags[0].Value, @"{color:(.*)}");
                    if (splitRes.Length > 2)
                    {
                        var colorText = splitRes[1];
                        foreach (var consoleColor in _colores)
                        {
                            if (consoleColor.ToString().ToLower() == colorText)
                            {
                                color = consoleColor;
                                break;
                            }
                        }
                    }
                }
            }
            res = text;
        }

        private string AddColorAsAttribute(string text, ConsoleColor color)
        {
            var ret = new StringBuilder();
            ret.Append("{color:");
            ret.Append(color.ToString().ToLower());
            ret.Append("}");
            ret.Append(text);
            ret.Append("{color}");
            return ret.ToString();
        }
    }
}