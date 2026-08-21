// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Z-Wave Alliance <https://z-wavealliance.org>
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zats.Services
{
    /// <summary>
    /// Sole consumer of the process's stdin stream. Runs a single read loop for the lifetime
    /// of the process and fans every character out to whoever is currently waiting on it.
    /// No other code should read from Console.In / Console.OpenStandardInput() / Console.ReadKey
    /// directly - doing so races the dispatcher's read loop for the same underlying handle and
    /// can silently steal bytes meant for the other side (see StdinDispatcher for details).
    /// Callers that no longer want to wait for input (e.g. disabling skip) simply cancel their
    /// own wait - the read loop itself keeps running unaffected.
    /// </summary>
    public interface IStdinDispatcher
    {
        /// <summary>
        /// Starts the read loop. Safe to call multiple times - only the first call actually
        /// starts the loop, subsequent calls are no-ops. Runs until EOF or cancellation.
        /// </summary>
        Task RunAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Fires once for every character read from stdin while the read loop is active.
        /// </summary>
        event Action<char> CharacterReceived;

        /// <summary>
        /// True once the underlying stream has reported EOF (e.g. the pipe was closed by the parent process).
        /// </summary>
        bool IsCompleted { get; }
    }
}
