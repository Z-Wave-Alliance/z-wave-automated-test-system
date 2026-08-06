// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using ZWave.Enums;

namespace Zats.Services
{
    public enum PathTypes
    {
        DevKitsRoot = 0,
        DefaultResources,
        ReportsRoot,
        TestCaseTemp,
    }

    public interface IFileSystemManager
    {
        /// <summary>
        /// Retrieves the latest firmware file path based on the provided parameters.
        /// <remarks>Not filter by parameter in case of null or empty parameter values</remarks>
        /// </summary>
        /// <param name="applicationLibrary">Demo application firmware library.</param>
        /// <param name="chipSeries">Specific chip series.</param>
        /// <param name="frequency">Rf frequency.</param>
        /// <param name="ending">File ending.</param>
        /// <param name="regexFilters">Regular expression filters.</param>
        /// <returns>Latest firmware file path.</returns>
        string GetLatestFirmwareFilePath(Libraries applicationLibrary, string chipSeries, string frequency, string ending, params string[] regexFilters);
        /// <summary>
        /// Get Bootloader and keys file paths needed for the application flashing to the device.
        /// Default root path to search specified in the IFileSystemManager.BasePath. (Resources folder)
        /// Default OTA/OTW pattern follows Serial API vs sample app; keys follow chip policy
        /// (Series1 SAPI except ZG14 use sample_* even for OTW; ZG14 and Series2 SAPI use controller_*).
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
        /// <summary>
        /// Gets folder path based on path type
        /// </summary>
        /// <param name="pathType">The type of path to retrieve</param>
        /// <returns>Folder path.</returns>
        string GetPath(PathTypes pathType);
        /// <summary>
        /// Creates and sets up specific test case run folder setting
        /// </summary>
        /// <param name="testCaseFolder"></param>
        void SetupTestCaseTempDirectory(string testCaseFolder);
    }
}