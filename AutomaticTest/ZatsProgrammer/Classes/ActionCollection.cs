// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;
using ZWave.Programmer.Controllers;
using ZWave.Programmer.Actions;

namespace ZWave.Programmer.Classes
{
    /// <summary>
    /// ActionCollection class. Contains references to the application Action classes.
    /// </summary>
    public class ActionCollection : BaseAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActionCollection"/> class.
        /// </summary>
        /// <param name="controller">The controller.</param>
        public ActionCollection(ControllerManager controller)
            : base(controller)
        {
        }

        private ZW050xFormActions mZW050xFormActions;
        /// <summary>
        /// Gets the ZW050x Form actions.
        /// </summary>
        /// <value>The ZW050x Form actions.</value>
        public ZW050xFormActions ZW050xFormActions
        {
            get
            {
                if (mZW050xFormActions == null) mZW050xFormActions = new ZW050xFormActions(this.ControllerManager);
                return mZW050xFormActions;
            }
        }

        private ProgrammerActions mProgrammerActions;

        /// <summary>
        /// Gets the Programmer actions (Common functions that used to programm the chip).
        /// </summary>
        /// <value>The Programmer actions.</value>
        public ProgrammerActions ProgrammerActions
        {
            get
            {
                if (mProgrammerActions == null) mProgrammerActions = new ProgrammerActions(this.ControllerManager);
                return mProgrammerActions;
            }

        }
    }
}
