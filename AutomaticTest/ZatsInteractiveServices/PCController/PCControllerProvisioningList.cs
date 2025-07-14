// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Linq;
using Utils;
using Utils.UI.Enums;
using System.Text;

namespace Zats.Services
{
    //public class PCControllerProvisioningList : IProvisioningList
    //{
    //    public PCControllerProvisioningList()
    //    {
    //    }

    //    public void Add(int key, byte[] dsk, SecuritySchemes[] schemes)
    //    {
    //    }

    //    public void Clear(int key)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public ProvisioningItem Get(int key, byte[] dsk)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void Remove(int key, byte[] dsk)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    public class PCControllerProvisioningService : AutomationServiceBase, IProvisioningService
    {
        public PCControllerProvisioningService(PCControllerDeviceManager pccDeviceManager)
            : base(pccDeviceManager)
        {
        }

        public void Add(Alias controllerNumber, Alias deviceNumber)
        {
            Add(controllerNumber, PCControllerDeviceManager.GetDSK(deviceNumber, 16), SecuritySchemeSet.ALL);
        }

        public void Add(Alias controllerNumber, Alias deviceNumber, SecuritySchemes[] schemes)
        {
            Add(controllerNumber, PCControllerDeviceManager.GetDSK(deviceNumber, 16), schemes);
        }

        public void Add(Alias controllerNumber, byte[] dsk, SecuritySchemes[] schemes)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            if (uiController != null && schemes.SequenceEqual(SecuritySchemeSet.ALL))
            {
                StringBuilder dskSb = new StringBuilder();
                if (dsk != null)
                {
                    for (int i = 0; i < 16; i += 2)
                    {
                        if (i >= 14)
                        {
                            dskSb.Append(Tools.GetInt32(dsk.Skip(i).Take(2).ToArray()).ToString("00000"));
                        }
                        else
                        {
                            dskSb.Append(Tools.GetInt32(dsk.Skip(i).Take(2).ToArray()).ToString("00000") + "-");
                        }
                    }
                    var smartStartView = FromMainMenuPage(uiController, PCControllerIdMap.SmartStartView.ID);
                    smartStartView.NavigateTo(PCControllerIdMap.SmartStartView.DskValueTb).SetElementValue(dskSb.ToString());
                    smartStartView.NavigateTo(PCControllerIdMap.SmartStartView.AddDskBtn).ClickIfEnabled();
                }
            }
            else
            {
                throw new NotImplementedException("PCControllerProvisioningService.Add");
            }
        }

        public void Add(Alias controllerNumber, Alias[] deviceNumbers, SecuritySchemes[] schemes)
        {
            foreach (var deviceNumber in deviceNumbers)
            {
                Add(controllerNumber, deviceNumber, schemes);
            }
        }

        public void Remove(Alias controllerNumber, Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public void Clear(Alias controllerNumber)
        {
            throw new NotImplementedException();
        }

        public ProvisioningItem Get(Alias controllerNumber, Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            throw new NotImplementedException();
        }

        public void AddDUTPreConfigured(Alias controllerNumber, SecuritySchemes[] schemes)
        {
            throw new NotImplementedException();
        }

        public void Add(Alias controllerNumber, Alias deviceNumber, SecuritySchemes[] schemes, Modes nodeOptions)
        {
            throw new NotImplementedException();
        }

        public void Add(Alias controllerNumber, byte[] dsk, SecuritySchemes[] schemes, Modes nodeOptions)
        {
            throw new NotImplementedException();
        }
    }
}
