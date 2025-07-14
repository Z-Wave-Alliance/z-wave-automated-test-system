// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zats.Services
{
    public class ZcpLink
    {
        private string portalUrl;
        private string certId;
        private string sectionId;
        private string itemId;

        public ZcpLink()
        {
            portalUrl = null;
            certId = null;
            sectionId = null;
            itemId = null;
        }

        public ZcpLink(string zcpUrl, string zcpCertId, string zcpSectionId = null, string zcpItemId = null)
        {
            portalUrl = zcpUrl;
            certId = zcpCertId;
            sectionId = zcpSectionId;
            itemId = zcpItemId;
        }

        public ZcpLink(string zcpUrl, int zcpCertId, int zcpSectionId, int zcpItemId)
        {
            portalUrl = zcpUrl;
            certId = zcpCertId.ToString();
            sectionId = zcpSectionId.ToString();
            itemId = zcpItemId.ToString();
        }


        public string GetViewLink()
        {

            if (string.IsNullOrEmpty(portalUrl) == false && string.IsNullOrEmpty(certId) == false && string.IsNullOrEmpty(sectionId) == false && string.IsNullOrEmpty(itemId) == false)
            {
                return portalUrl + @"/certs/" + certId + @"/sections/" + sectionId + @"/show#cert-item-" + itemId;
            }
            return null;
        }

        public string GetEditLink()
        {

            if (string.IsNullOrEmpty(portalUrl) == false && string.IsNullOrEmpty(certId) == false && string.IsNullOrEmpty(sectionId) == false && string.IsNullOrEmpty(itemId) == false)
            {
                return portalUrl + @"/certs/" + certId + @"/sections/" + sectionId + @"/select/edit/1#cert-item-" + itemId;
            }
            return null;
        }

        public string GetCaseViewLink()
        {
            if (string.IsNullOrEmpty(portalUrl) == false && string.IsNullOrEmpty(certId) == false)
            {
                return portalUrl + @"/certs/" + certId;
            }
            return null;
        }

        public string GetCaseEditLink()
        {
            if (string.IsNullOrEmpty(portalUrl) == false && string.IsNullOrEmpty(certId) == false)
            {
                return portalUrl + @"/certs/edit/" + certId;
            }
            return null;
        }
    }
}
