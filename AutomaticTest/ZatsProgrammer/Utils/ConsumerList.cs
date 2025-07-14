// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ZWave.Framework
{
    public class ConsumerList<T>
      where T : class
    {
        #region privates
        int _timeoutMs = 0;
        LinkedList<T> mInnerQueue;
        Thread mWorkerThread;
        AutoResetEvent mWorkSignal = new AutoResetEvent(false);
        Action<LinkedList<T>> mConsumerCallback;
        #endregion privates
        public string Name { get; set; }

        private volatile bool mIsOpen;
        public bool IsOpen
        {
            get { return mIsOpen; }
        }

        public void Stop()
        {
            if (!mIsOpen)
                return;

            mIsOpen = false;
            mWorkSignal.Set();
            if (IsOpen && Thread.CurrentThread.ManagedThreadId != mWorkerThread.ManagedThreadId)
                mWorkerThread.Join();
        }

        public void Start(Action<LinkedList<T>> consumerCallback, int timeoutMs)
        {
            _timeoutMs = timeoutMs;
            Start("", consumerCallback);
        }

        public void Start(string name, Action<LinkedList<T>> consumerCallback)
        {
            Name = name;
            if (mIsOpen)
                return;

            mIsOpen = true;
            mInnerQueue = new LinkedList<T>();
            mConsumerCallback = consumerCallback;
            mWorkerThread = new Thread(DoWork)
            {
                Name = "Consumer List - " + Name,
                IsBackground = true
            };
            mWorkerThread.Start();
            mWorkSignal.Set();
        }

        public void Add(T item)
        {
            if (IsOpen)
            {
                lock (locker)
                {
                    mInnerQueue.AddLast(item);
                }
                SetWorkerSignal();
            }
        }

        private readonly object locker = new object();
        private void DoWork()
        {
            while (IsOpen)
            {
                WaitWorkerSignal();
                while (mInnerQueue.Count > 0)
                {
                    LinkedList<T> prevList = null;
                    lock (locker)
                    {
                        prevList = mInnerQueue;
                        mInnerQueue = new LinkedList<T>();
                    }
                    mConsumerCallback(prevList);
                }
                if (_timeoutMs > 0 && _timeoutMs < ushort.MaxValue)
                    Thread.Sleep(_timeoutMs);
            }
        }

        private void WaitWorkerSignal()
        {
            //Tools._writeDebugDiagnosticMessage(Name + ": Wait WORK");
            mWorkSignal.WaitOne();
        }

        private void SetWorkerSignal()
        {
            //Tools._writeDebugDiagnosticMessage(Name + ": Set WORK");
            mWorkSignal.Set();
        }

        public void Reset()
        {
            mInnerQueue.Clear();
        }
    }
}
