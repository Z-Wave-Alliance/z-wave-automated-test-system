// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
// SPDX-FileCopyrightText: Z-Wave Alliance https://z-wavealliance.org
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utils.UI.Enums;

namespace Zats.Services
{
    public class MessageBoxImpl : IMessageBox
    {
        private static readonly ConsoleKey[] terminalKeys =
        {
            ConsoleKey.Escape, ConsoleKey.Enter, ConsoleKey.Y, ConsoleKey.N, ConsoleKey.O
        };

        private readonly IStdinDispatcher stdin = StdinDispatcher.Shared;
        private CancellationTokenSource skipCts;

        public event Action<string, string, LogLevels> OnLog;
        public event Action OnSkip;

        public StringBuilder UserInput = new StringBuilder();

        public static int TimeOut { get; set; }

        public DialogResult Show(string message, string title = "")
        {
            return Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
        }

        public void ShowSkip(Action skipCallback)
        {
            OnSkip = skipCallback;

            // Warning: Changing the following string without adapting the LogDataReceived function in CTT class ZatsTestWorker
            // breaks the confirmation buttons in CTT!
            WriteLine("»SKIP:SHOW«");

            skipCts = new CancellationTokenSource();
            _ = ListenForSkipAsync(skipCts.Token);
        }

        private async Task ListenForSkipAsync(CancellationToken cancellationToken)
        {
            try
            {
                await stdin.WaitForCharAsync(c => (ConsoleKey)c == ConsoleKey.S, cancellationToken).ConfigureAwait(false);
                OnSkip?.Invoke();
            }
            catch (OperationCanceledException)
            {
                // Expected when DisableSkip() cancels the wait - not an error.
                WriteLine("»SKIP:DISABLE«");
            }
        }

        public void DisableSkip()
        {
            skipCts?.Cancel();
        }

        public void ShowDutInteraction(string message)
        {
            // Warning: Changing the following string without adapting the LogDataReceived function in CTT class ZatsTestWorker
            // breaks the output within the CTT!
            WriteTitle($"{message}»WAIT_DUT:SHOW«");
        }

        public DialogResult Show(string message, string title, MessageBoxButtons messageBoxButtons, MessageBoxIcon messageBoxIcon = MessageBoxIcon.Asterisk)
        {
            if (TimeOut > 0)
            {
                WriteLine("Method MessageBox.Show skipped due to timeout property enabled ");
                return DialogResult.Cancel;
            }

            UserInput.Clear();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("-----------------------------------------------------------------------");
            if (!string.IsNullOrEmpty(title))
            {
                sb.AppendLine(title);
                sb.AppendLine("-----------------------------------------------------------------------");
            }
            sb.AppendLine(message);

            if (Console.IsInputRedirected)
            {
                // Warning: Changing the following strings without adapting the LogDataReceived function in CTT class ZatsTestWorker
                // breaks the confirmation buttons in CTT!
                if (messageBoxButtons == MessageBoxButtons.OKCancel)
                {
                    sb.AppendLine("»OK-CANCEL:SHOW«");
                }
                else if (messageBoxButtons == MessageBoxButtons.OK)
                {
                    sb.AppendLine("»OK:SHOW«");
                }
                else if (messageBoxButtons == MessageBoxButtons.OpenCancel)
                {
                    sb.AppendLine("»URL-DEFINE:" + title + "«");
                    sb.AppendLine("»URL-OPEN-CANCEL:SHOW«");
                }
                else
                {
                    sb.AppendLine("»YES-NO:SHOW«");
                }
            }
            else
            {
                if (messageBoxButtons == MessageBoxButtons.OKCancel)
                {
                    sb.AppendLine("»ENTER-ESC:SHOW«");
                }
                else if (messageBoxButtons == MessageBoxButtons.OK)
                {
                    sb.AppendLine("»ENTER:SHOW«");
                }
                else
                {
                    sb.AppendLine("»Y-N:SHOW«");
                }
            }
            WriteLine(sb.ToString());
            var keyValue = ConsoleKey.Escape;
            while (true)
            {
                try
                {
                    keyValue = ReadNextRelevantKey();
                    if (UserInput.Length > 0)
                    {
                        WriteLine(UserInput.ToString());
                    }
                    if (Console.IsInputRedirected)
                    {
                        switch (keyValue)
                        {
                            case ConsoleKey.Escape:
                                WriteLine("Cancel");
                                break;
                            case ConsoleKey.Enter:
                                WriteLine("OK");
                                break;
                            case ConsoleKey.Y:
                                WriteLine("Yes");
                                break;
                            case ConsoleKey.N:
                                WriteLine("No");
                                break;
                            case ConsoleKey.O:
                                WriteLine("Open");
                                break;
                            default:
                                WriteLine("Unknown User Input");
                                break;
                        }
                    }
                    else
                    {
                        WriteLine(keyValue.ToString());
                    }
                }
                catch
                { }
                WriteLine("-----------------------------------------------------------------------");
                if (messageBoxButtons == MessageBoxButtons.OK)
                {
                    if (keyValue == ConsoleKey.Enter)
                    {
                        return DialogResult.OK;
                    }
                }
                else if (messageBoxButtons == MessageBoxButtons.YesNo)
                {
                    if (keyValue == ConsoleKey.Y)
                    {
                        return DialogResult.Yes;
                    }
                    else if (keyValue == ConsoleKey.N)
                    {
                        return DialogResult.No;
                    }
                }
                else if (messageBoxButtons == MessageBoxButtons.OKCancel)
                {
                    if (keyValue == ConsoleKey.Enter)
                    {
                        return DialogResult.OK;
                    }
                    else if (keyValue == ConsoleKey.Escape)
                    {
                        return DialogResult.Cancel;
                    }
                }
                else if (messageBoxButtons == MessageBoxButtons.OpenCancel)
                {
                    if (keyValue == ConsoleKey.O)
                    {
                        return DialogResult.Open;
                    }
                    else if (keyValue == ConsoleKey.Escape)
                    {
                        return DialogResult.Cancel;
                    }
                }
            }
        }

        private ConsoleKey ReadNextRelevantKey()
        {
            var tcs = new TaskCompletionSource<ConsoleKey>(TaskCreationOptions.RunContinuationsAsynchronously);

            void OnChar(char c)
            {
                var keyValue = (ConsoleKey)c;
                if (!Console.IsInputRedirected)
                {
                    if (keyValue == ConsoleKey.Backspace && UserInput.Length > 0)
                    {
                        UserInput.Remove(UserInput.Length - 1, 1);
                    }
                    else
                    {
                        UserInput.Append(c);
                    }
                }
                if (Array.IndexOf(terminalKeys, keyValue) >= 0)
                {
                    tcs.TrySetResult(keyValue);
                }
            }

            stdin.CharacterReceived += OnChar;
            try
            {
                return tcs.Task.GetAwaiter().GetResult();
            }
            finally
            {
                stdin.CharacterReceived -= OnChar;
            }
        }

        private void WriteLine(string text)
        {
            OnLog?.Invoke(text, null, LogLevels.Information);
        }

        private void WriteTitle(string text)
        {
            OnLog?.Invoke(text, null, LogLevels.Title);
        }

        public string GetUserInput()
        {
            return UserInput.ToString();
        }
    }

    public enum MessageBoxButtons
    {
        OKCancel,
        OK,
        YesNo,
        OpenCancel
    }

    public enum MessageBoxIcon
    {
        Asterisk
    }

    public enum DialogResult
    {
        OK,
        Cancel,
        Yes,
        No,
        Open
    }
}
