// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Zats.Services
{
    /// <summary>
    /// Await-friendly helpers built on top of <see cref="IStdinDispatcher"/>. Each helper
    /// registers a temporary handler on <see cref="IStdinDispatcher.CharacterReceived"/> and
    /// removes it again once satisfied or cancelled - the dispatcher's read loop itself is never
    /// touched, so cancelling a wait here never needs to interrupt a blocking read.
    /// </summary>
    public static class StdinDispatcherExtensions
    {
        /// <summary>
        /// Waits for the next character for which <paramref name="predicate"/> returns true.
        /// Characters that don't match are ignored for this waiter (other, concurrently waiting
        /// callers still see them).
        /// </summary>
        public static async Task<char> WaitForCharAsync(
            this IStdinDispatcher dispatcher,
            Func<char, bool> predicate,
            CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<char>(TaskCreationOptions.RunContinuationsAsynchronously);

            void OnChar(char c)
            {
                if (predicate(c))
                {
                    tcs.TrySetResult(c);
                }
            }

            dispatcher.CharacterReceived += OnChar;
            using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken)))
            {
                try
                {
                    return await tcs.Task.ConfigureAwait(false);
                }
                finally
                {
                    dispatcher.CharacterReceived -= OnChar;
                }
            }
        }

        /// <summary>
        /// Waits for the next line, terminated by '\n' (a preceding '\r' is trimmed).
        /// </summary>
        public static async Task<string> WaitForLineAsync(
            this IStdinDispatcher dispatcher,
            CancellationToken cancellationToken = default)
        {
            var line = new StringBuilder();
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            void OnChar(char c)
            {
                if (c == '\n')
                {
                    tcs.TrySetResult(line.ToString().TrimEnd('\r'));
                }
                else
                {
                    line.Append(c);
                }
            }

            dispatcher.CharacterReceived += OnChar;
            using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken)))
            {
                try
                {
                    return await tcs.Task.ConfigureAwait(false);
                }
                finally
                {
                    dispatcher.CharacterReceived -= OnChar;
                }
            }
        }
    }
}
