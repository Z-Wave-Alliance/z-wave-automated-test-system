// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Zats.Services
{
    /// <summary>
    /// Default <see cref="IStdinDispatcher"/> implementation. Reads stdin on a single background
    /// task/thread for the lifetime of the process and fans every character out via
    /// <see cref="CharacterReceived"/>. Never uses OS-specific cancellation (no CancelIoEx /
    /// GetStdHandle) - the read loop simply keeps running for as long as the process does;
    /// individual waiters cancel their own wait, not the underlying read.
    ///
    /// Two read strategies, both funneled through the same event so callers don't need to care
    /// which one is active:
    ///  - Redirected stdin (the normal case when spawned by CTT with piped stdio): asynchronous,
    ///    non-blocking reads via StreamReader.ReadAsync over Console.OpenStandardInput().
    ///  - Real console (a developer running the console app directly): Console.ReadKey(true) on a
    ///    dedicated long-running task, since a real console only delivers bytes to a raw Stream
    ///    read once the line is submitted (canonical mode), which would break immediate single
    ///    keystroke handling (Escape, arrow keys, etc.).
    /// </summary>
    public sealed class StdinDispatcher : IStdinDispatcher
    {
        private static readonly Lazy<StdinDispatcher> sharedInstance =
            new Lazy<StdinDispatcher>(() => new StdinDispatcher());

        /// <summary>
        /// Process-wide singleton. The read loop starts automatically the first time this is
        /// accessed - callers never need to call RunAsync themselves. Starting a second,
        /// independent instance would mean two competing readers on the same OS handle - see the
        /// type-level remarks, so there is no public constructor.
        /// </summary>
        public static IStdinDispatcher Shared => sharedInstance.Value;

        public event Action<char> CharacterReceived;

        public bool IsCompleted { get; private set; }

        private int runStarted;

        private StdinDispatcher()
        {
            _ = RunAsync(CancellationToken.None);
        }

        public Task RunAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.Exchange(ref runStarted, 1) != 0)
            {
                return Task.CompletedTask;
            }

            return Console.IsInputRedirected
                ? RunRedirectedAsync(cancellationToken)
                : RunInteractiveAsync(cancellationToken);
        }

        private async Task RunRedirectedAsync(CancellationToken cancellationToken)
        {
            using (var reader = new StreamReader(Console.OpenStandardInput(), Console.InputEncoding))
            {
                var buffer = new char[256];
                while (!cancellationToken.IsCancellationRequested)
                {
                    int read = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                    if (read == 0)
                    {
                        IsCompleted = true;
                        return;
                    }
                    for (int i = 0; i < read; i++)
                    {
                        CharacterReceived?.Invoke(buffer[i]);
                    }
                }
            }
        }

        private Task RunInteractiveAsync(CancellationToken cancellationToken)
        {
            // Console.ReadKey blocks synchronously and offers no way to observe a
            // CancellationToken while blocked. It runs on one dedicated long-running task for the
            // whole process lifetime instead, which is fine - it is never meant to be cancelled,
            // only to stop naturally when the process exits.
            return Task.Factory.StartNew(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    ConsoleKeyInfo key;
                    try
                    {
                        key = Console.ReadKey(true);
                    }
                    catch (InvalidOperationException)
                    {
                        IsCompleted = true;
                        return;
                    }
                    CharacterReceived?.Invoke(key.KeyChar);
                }
            }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
    }
}
