// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using ZWave.Enums;

namespace Zats.Services
{
    public interface IFileSystemManager
    {
        string BasePath { get; set; }
        Profile CurrentProfile { get; set; }
        string GetLatestFilePath(Libraries lib, string chipSeries, string frequency, string ending, DevKits devKit, params string[] regexFilters);
        string GetLatestFilePath(Libraries lib, string chipSeries, string frequency, DevKits devKit, params string[] regexFilters);
        string GetLatestFilePath(string filter, DevKits devKit, params string[] regexFilters);
    }
}