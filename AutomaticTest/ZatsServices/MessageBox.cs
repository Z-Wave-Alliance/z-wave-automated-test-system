using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Utils.UI.Enums;

namespace Zats.Services
{
    public class MessageBoxImpl : IMessageBox
    {
        const int STD_INPUT_HANDLE = -10;

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CancelIoEx(IntPtr handle, IntPtr lpOverlapped);

        public event Action<string, string, LogLevels> OnLog;
        public event Action OnSkip;
        private BackgroundWorker inputKeyReader;
        private bool skipIsDisabled = true;

        public DialogResult Show(string message, string title = "")
        {
            return Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
        }

        public StringBuilder UserInput = new StringBuilder();

        public static int TimeOut { get; set; }

        public void ShowSkip(Action skipCallback)
        {
            if (!Console.IsInputRedirected)
            {
                while (Console.KeyAvailable)
                {
                    Console.ReadKey(false);
                }
            }

            OnSkip = skipCallback;

            // Warning: Changing the following string without adapting the LogDataReceived function in CTT class ZatsTestWorker 
            // breaks the confirmation buttons in CTT!
            WriteLine("»SKIP:SHOW«");

            inputKeyReader = new BackgroundWorker();
            inputKeyReader.DoWork += new DoWorkEventHandler(WaitForSkipKey);
            //inputKeyReader.RunWorkerCompleted += new RunWorkerCompletedEventHandler(skipCallback);
            inputKeyReader.RunWorkerAsync();
        }

        private void WaitForSkipKey(object sender, DoWorkEventArgs e)
        {
            var keyValue = ConsoleKey.Escape;

            skipIsDisabled = false;
            try
            {
                ReadKey(out keyValue, out ConsoleKeyInfo key);
                while (key.Key != ConsoleKey.S)
                {
                    ReadKey(out keyValue, out key);
                }
                OnSkip();

            }
            catch (Exception)
            {
                WriteLine("»SKIP:DISABLE«");
            }

            skipIsDisabled = true;
        }

        public void DisableSkip()
        {
            var handle = GetStdHandle(STD_INPUT_HANDLE);
            CancelIoEx(handle, IntPtr.Zero);

            int maxWaitForDisabledCycles = 20;

            while (skipIsDisabled == false && maxWaitForDisabledCycles-- > 0) Thread.Sleep(100);

            //if(maxWaitForDisabledCycles == -1)
            //{
            //    WriteLine("DISABLE SKIP BREAK BY TIMEOUT!!!");
            //}
        }

        public DialogResult Show(string message, string title, MessageBoxButtons messageBoxButtons, MessageBoxIcon messageBoxIcon = MessageBoxIcon.Asterisk)
        {
            if (TimeOut > 0)
            {
                WriteLine("Method MessageBox.Show skipped due to timeout property enabled ");
                return DialogResult.Cancel;
            }
            else
            {
                if (!Console.IsInputRedirected)
                {
                    while (Console.KeyAvailable)
                    {
                        Console.ReadKey(false);
                    }
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
                        ReadKey(out keyValue, out ConsoleKeyInfo key);
                        while (key.Key != ConsoleKey.Escape && key.Key != ConsoleKey.Enter && key.Key != ConsoleKey.Y && key.Key != ConsoleKey.N && key.Key != ConsoleKey.O)
                        {
                            ReadKey(out keyValue, out key);
                        }
                        if (UserInput.Length > 0)
                        {
                            WriteLine(UserInput.ToString());
                        }
                        if (Console.IsInputRedirected)
                        {
                            switch (key.Key)
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
        }

        private void ReadKey(out ConsoleKey keyValue, out ConsoleKeyInfo key)
        {
            if (Console.IsInputRedirected)
            {
                char ret = (char)Console.In.Read();
                keyValue = (ConsoleKey)ret;
                key = new ConsoleKeyInfo(ret, keyValue, false, false, false);
            }
            else
            {
                key = Console.ReadKey(true);
                keyValue = key.Key;
                if (keyValue == ConsoleKey.Backspace && UserInput.Length > 0)
                {
                    UserInput.Remove(UserInput.Length - 1, 1);
                }
                else
                {
                    UserInput.Append(key.KeyChar);
                }
            }
        }

        private void WriteLine(string text)
        {
            OnLog?.Invoke(text, null, LogLevels.Information);
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
