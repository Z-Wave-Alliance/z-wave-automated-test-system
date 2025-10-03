// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using ZWave.Enums;

namespace Zats.Services
{
    public interface IFileSystemManager
    {
        string BasePath { get; set; }
        string GetLatestFilePath(Libraries lib, string chipSeries, string frequency, string ending, DevKits devKit, params string[] regexFilters);
        string GetLatestFilePath(Libraries lib, string chipSeries, string frequency, DevKits devKit, params string[] regexFilters);
        string GetLatestFilePath(string filter, DevKits devKit, params string[] regexFilters);
        /// <summary>
        /// Get Bootloader and keys file paths needed for the application flashing to the device.
        /// Default root path to search specified in the IFileSystemManager.BasePath. (Resources folder)
        /// Default file names in the FilePatternDefinition, where Ota/Otw detects from the Device Host Library.
        /// </summary>
        /// <param name="deviceHostClient">Device host information.</param>
        /// <param name="specificFileNamePattern">Concrete file name or search pattern for bootloader, to use instead of default value.</param>
        /// <param name="regexFilters">Custom cascade filters. Specifies details for searching file.</param>
        /// <returns>Bootloader found files. Default in case not found:
        ///  - *bootloader* since v.7.23 or empty (if specificFileNamePattern is empty;
        ///  - default keys from 'Resources' or found</returns>
        BootloaderPath GetBootloaderPath(DeviceHostClient deviceHostClient, string[] specificFileNamePattern, params string[] regexFilters);
        /// <summary>
        /// Get predefined controller path from resources by chip series.
        /// </summary>
        /// <param name="chipSeries">Chip series for the controller.</param>
        /// <returns>File path for the predefined controller.</returns>
        string GetPredefinedControllerPathFromResources(ChipSeries chipSeries);
    }
}