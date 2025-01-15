// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Text;
using ZWave.Enums;
using System.Diagnostics;
using ZWave.Devices;
using ZWave.Logging;
using System.ComponentModel;
using ZWave.SerialPortTransport;
using ZWave.ProgrammerFrame;
using ZWave.SerialPortSession;
using ZWave.SerialPortApplication;

namespace ZWave
{
    public delegate void LogPacketDelegate(LogPacket logPacket);
    /// <summary>
    /// Represents a <b>ZWaveManager</b> that manage all layers.
    /// </summary>
    public class ZWaveManager
    {
        /// <summary>
        /// Represents the method that will handle the status changed event.
        /// </summary>
        public event EventHandler StatusChanged;
        public event LogPacketDelegate LogPacketAdded;
        /// <summary>
        /// Initializes a new instance of the <see cref="ZWaveManager"/> class.
        /// </summary>
        public ZWaveManager()
        {
        }

        public void AddLogPacket(LogPacket logPacket)
        {
            LogPacketAdded?.Invoke(logPacket);
        }
        private IApplicationLayer mApplicationLayer;
        /// <summary>
        /// Gets or sets the application layer.
        /// </summary>
        /// <value>The application layer.</value>
        public IApplicationLayer ApplicationLayer
        {
            get
            {
                return mApplicationLayer;
            }
            set
            {
                mApplicationLayer = value;
            }
        }

        private IFrameLayer mFrameLayer;
        /// <summary>
        /// Gets or sets the frame layer.
        /// </summary>
        /// <value>The frame layer.</value>
        public IFrameLayer FrameLayer
        {
            get
            {
                return mFrameLayer;
            }
            set
            {
                mFrameLayer = value;
            }
        }

        private ISessionLayer mSessionLayer;
        /// <summary>
        /// Gets or sets the session layer.
        /// </summary>
        /// <value>The session layer.</value>
        public ISessionLayer SessionLayer
        {
            get
            {
                return mSessionLayer;
            }
            set
            {
                mSessionLayer = value;
            }
        }

        private ITransportLayer mTransportLayer;
        /// <summary>
        /// Gets or sets the transport layer.
        /// </summary>
        /// <value>The transport layer.</value>
        public ITransportLayer TransportLayer
        {
            get
            {
                return mTransportLayer;
            }
            set
            {
                mTransportLayer = value;
            }
        }

        private ManagerStatus mStatus = ManagerStatus.None;
        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        public ManagerStatus Status
        {
            get
            {
                return mStatus;
            }
            set
            {
                mStatus = value;
                StatusChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Init()
        {
            TransportLayer = new TransportLayer();
            FrameLayer = new FrameLayer();
            SessionLayer = new SessionLayer();
            ApplicationLayer = new ApplicationLayer();

            FrameLayer.Init(TransportLayer, AddLogPacket);
            SessionLayer.Init(FrameLayer);
            ApplicationLayer.Init(SessionLayer);
            Status = ManagerStatus.Initialized;
        }
    }
}
