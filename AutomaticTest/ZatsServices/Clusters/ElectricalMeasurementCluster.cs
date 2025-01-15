// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Newtonsoft.Json;
using System.Collections.Generic;

namespace UicApplication.Clusters
{
    public class ElectricalMeasurementCluster
    {
        public const string ClusterName = "ElectricalMeasurement";
        public const string ClusterDesired = "Desired";
        public const string ClusterReported = "Reported";
        public const string ClusterSupportedCommands = "SupportedCommands";
        public const string ClusterAttributes = "Attributes";

        #region Cluster Attributes
        public class ATT_MeasurementType
        {
            public const string MeasurementType_name = "MeasurementType";
            public class MeasurementType_Attributes
            {
                public static implicit operator string(MeasurementType_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_MeasurementType command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_MeasurementType.MeasurementType_name);
            }
            public class MeasurementType_Desired
            {
                public static implicit operator string(MeasurementType_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_MeasurementType.MeasurementType_name, ClusterDesired);
                }
            }
            public class MeasurementType_Report
            {
                public static implicit operator string(MeasurementType_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_MeasurementType.MeasurementType_name, ClusterReported);
                }
            }
            public class MeasurementType_SupportCommands
            {
                public static implicit operator string(MeasurementType_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_DCVoltage
        {
            public const string DCVoltage_name = "DCVoltage";
            public class DCVoltage_Attributes
            {
                public static implicit operator string(DCVoltage_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_DCVoltage command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCVoltage.DCVoltage_name);
            }
            public class DCVoltage_Desired
            {
                public static implicit operator string(DCVoltage_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCVoltage.DCVoltage_name, ClusterDesired);
                }
            }
            public class DCVoltage_Report
            {
                public static implicit operator string(DCVoltage_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCVoltage.DCVoltage_name, ClusterReported);
                }
            }
            public class DCVoltage_SupportCommands
            {
                public static implicit operator string(DCVoltage_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_DCVoltageMin
        {
            public const string DCVoltageMin_name = "DCVoltageMin";
            public class DCVoltageMin_Attributes
            {
                public static implicit operator string(DCVoltageMin_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_DCVoltageMin command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCVoltageMin.DCVoltageMin_name);
            }
            public class DCVoltageMin_Desired
            {
                public static implicit operator string(DCVoltageMin_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCVoltageMin.DCVoltageMin_name, ClusterDesired);
                }
            }
            public class DCVoltageMin_Report
            {
                public static implicit operator string(DCVoltageMin_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCVoltageMin.DCVoltageMin_name, ClusterReported);
                }
            }
            public class DCVoltageMin_SupportCommands
            {
                public static implicit operator string(DCVoltageMin_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_DCVoltageMax
        {
            public const string DCVoltageMax_name = "DCVoltageMax";
            public class DCVoltageMax_Attributes
            {
                public static implicit operator string(DCVoltageMax_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_DCVoltageMax command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCVoltageMax.DCVoltageMax_name);
            }
            public class DCVoltageMax_Desired
            {
                public static implicit operator string(DCVoltageMax_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCVoltageMax.DCVoltageMax_name, ClusterDesired);
                }
            }
            public class DCVoltageMax_Report
            {
                public static implicit operator string(DCVoltageMax_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCVoltageMax.DCVoltageMax_name, ClusterReported);
                }
            }
            public class DCVoltageMax_SupportCommands
            {
                public static implicit operator string(DCVoltageMax_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_DCCurrent
        {
            public const string DCCurrent_name = "DCCurrent";
            public class DCCurrent_Attributes
            {
                public static implicit operator string(DCCurrent_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_DCCurrent command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCCurrent.DCCurrent_name);
            }
            public class DCCurrent_Desired
            {
                public static implicit operator string(DCCurrent_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCCurrent.DCCurrent_name, ClusterDesired);
                }
            }
            public class DCCurrent_Report
            {
                public static implicit operator string(DCCurrent_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCCurrent.DCCurrent_name, ClusterReported);
                }
            }
            public class DCCurrent_SupportCommands
            {
                public static implicit operator string(DCCurrent_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_DCCurrentMin
        {
            public const string DCCurrentMin_name = "DCCurrentMin";
            public class DCCurrentMin_Attributes
            {
                public static implicit operator string(DCCurrentMin_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_DCCurrentMin command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCCurrentMin.DCCurrentMin_name);
            }
            public class DCCurrentMin_Desired
            {
                public static implicit operator string(DCCurrentMin_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCCurrentMin.DCCurrentMin_name, ClusterDesired);
                }
            }
            public class DCCurrentMin_Report
            {
                public static implicit operator string(DCCurrentMin_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCCurrentMin.DCCurrentMin_name, ClusterReported);
                }
            }
            public class DCCurrentMin_SupportCommands
            {
                public static implicit operator string(DCCurrentMin_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_DCCurrentMax
        {
            public const string DCCurrentMax_name = "DCCurrentMax";
            public class DCCurrentMax_Attributes
            {
                public static implicit operator string(DCCurrentMax_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_DCCurrentMax command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCCurrentMax.DCCurrentMax_name);
            }
            public class DCCurrentMax_Desired
            {
                public static implicit operator string(DCCurrentMax_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCCurrentMax.DCCurrentMax_name, ClusterDesired);
                }
            }
            public class DCCurrentMax_Report
            {
                public static implicit operator string(DCCurrentMax_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCCurrentMax.DCCurrentMax_name, ClusterReported);
                }
            }
            public class DCCurrentMax_SupportCommands
            {
                public static implicit operator string(DCCurrentMax_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_DCPower
        {
            public const string DCPower_name = "DCPower";
            public class DCPower_Attributes
            {
                public static implicit operator string(DCPower_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_DCPower command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCPower.DCPower_name);
            }
            public class DCPower_Desired
            {
                public static implicit operator string(DCPower_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCPower.DCPower_name, ClusterDesired);
                }
            }
            public class DCPower_Report
            {
                public static implicit operator string(DCPower_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCPower.DCPower_name, ClusterReported);
                }
            }
            public class DCPower_SupportCommands
            {
                public static implicit operator string(DCPower_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_DCPowerMin
        {
            public const string DCPowerMin_name = "DCPowerMin";
            public class DCPowerMin_Attributes
            {
                public static implicit operator string(DCPowerMin_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_DCPowerMin command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCPowerMin.DCPowerMin_name);
            }
            public class DCPowerMin_Desired
            {
                public static implicit operator string(DCPowerMin_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCPowerMin.DCPowerMin_name, ClusterDesired);
                }
            }
            public class DCPowerMin_Report
            {
                public static implicit operator string(DCPowerMin_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCPowerMin.DCPowerMin_name, ClusterReported);
                }
            }
            public class DCPowerMin_SupportCommands
            {
                public static implicit operator string(DCPowerMin_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_DCPowerMax
        {
            public const string DCPowerMax_name = "DCPowerMax";
            public class DCPowerMax_Attributes
            {
                public static implicit operator string(DCPowerMax_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_DCPowerMax command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCPowerMax.DCPowerMax_name);
            }
            public class DCPowerMax_Desired
            {
                public static implicit operator string(DCPowerMax_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCPowerMax.DCPowerMax_name, ClusterDesired);
                }
            }
            public class DCPowerMax_Report
            {
                public static implicit operator string(DCPowerMax_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCPowerMax.DCPowerMax_name, ClusterReported);
                }
            }
            public class DCPowerMax_SupportCommands
            {
                public static implicit operator string(DCPowerMax_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_DCVoltageMultiplier
        {
            public const string DCVoltageMultiplier_name = "DCVoltageMultiplier";
            public class DCVoltageMultiplier_Attributes
            {
                public static implicit operator string(DCVoltageMultiplier_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_DCVoltageMultiplier command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCVoltageMultiplier.DCVoltageMultiplier_name);
            }
            public class DCVoltageMultiplier_Desired
            {
                public static implicit operator string(DCVoltageMultiplier_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCVoltageMultiplier.DCVoltageMultiplier_name, ClusterDesired);
                }
            }
            public class DCVoltageMultiplier_Report
            {
                public static implicit operator string(DCVoltageMultiplier_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCVoltageMultiplier.DCVoltageMultiplier_name, ClusterReported);
                }
            }
            public class DCVoltageMultiplier_SupportCommands
            {
                public static implicit operator string(DCVoltageMultiplier_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_DCVoltageDivisor
        {
            public const string DCVoltageDivisor_name = "DCVoltageDivisor";
            public class DCVoltageDivisor_Attributes
            {
                public static implicit operator string(DCVoltageDivisor_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_DCVoltageDivisor command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCVoltageDivisor.DCVoltageDivisor_name);
            }
            public class DCVoltageDivisor_Desired
            {
                public static implicit operator string(DCVoltageDivisor_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCVoltageDivisor.DCVoltageDivisor_name, ClusterDesired);
                }
            }
            public class DCVoltageDivisor_Report
            {
                public static implicit operator string(DCVoltageDivisor_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCVoltageDivisor.DCVoltageDivisor_name, ClusterReported);
                }
            }
            public class DCVoltageDivisor_SupportCommands
            {
                public static implicit operator string(DCVoltageDivisor_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_DCCurrentMultplier
        {
            public const string DCCurrentMultplier_name = "DCCurrentMultplier";
            public class DCCurrentMultplier_Attributes
            {
                public static implicit operator string(DCCurrentMultplier_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_DCCurrentMultplier command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCCurrentMultplier.DCCurrentMultplier_name);
            }
            public class DCCurrentMultplier_Desired
            {
                public static implicit operator string(DCCurrentMultplier_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCCurrentMultplier.DCCurrentMultplier_name, ClusterDesired);
                }
            }
            public class DCCurrentMultplier_Report
            {
                public static implicit operator string(DCCurrentMultplier_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCCurrentMultplier.DCCurrentMultplier_name, ClusterReported);
                }
            }
            public class DCCurrentMultplier_SupportCommands
            {
                public static implicit operator string(DCCurrentMultplier_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_DCCurrentDivisor
        {
            public const string DCCurrentDivisor_name = "DCCurrentDivisor";
            public class DCCurrentDivisor_Attributes
            {
                public static implicit operator string(DCCurrentDivisor_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_DCCurrentDivisor command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCCurrentDivisor.DCCurrentDivisor_name);
            }
            public class DCCurrentDivisor_Desired
            {
                public static implicit operator string(DCCurrentDivisor_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCCurrentDivisor.DCCurrentDivisor_name, ClusterDesired);
                }
            }
            public class DCCurrentDivisor_Report
            {
                public static implicit operator string(DCCurrentDivisor_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCCurrentDivisor.DCCurrentDivisor_name, ClusterReported);
                }
            }
            public class DCCurrentDivisor_SupportCommands
            {
                public static implicit operator string(DCCurrentDivisor_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_DCPowerMultiplier
        {
            public const string DCPowerMultiplier_name = "DCPowerMultiplier";
            public class DCPowerMultiplier_Attributes
            {
                public static implicit operator string(DCPowerMultiplier_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_DCPowerMultiplier command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCPowerMultiplier.DCPowerMultiplier_name);
            }
            public class DCPowerMultiplier_Desired
            {
                public static implicit operator string(DCPowerMultiplier_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCPowerMultiplier.DCPowerMultiplier_name, ClusterDesired);
                }
            }
            public class DCPowerMultiplier_Report
            {
                public static implicit operator string(DCPowerMultiplier_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCPowerMultiplier.DCPowerMultiplier_name, ClusterReported);
                }
            }
            public class DCPowerMultiplier_SupportCommands
            {
                public static implicit operator string(DCPowerMultiplier_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_DCPowerDivisor
        {
            public const string DCPowerDivisor_name = "DCPowerDivisor";
            public class DCPowerDivisor_Attributes
            {
                public static implicit operator string(DCPowerDivisor_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_DCPowerDivisor command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCPowerDivisor.DCPowerDivisor_name);
            }
            public class DCPowerDivisor_Desired
            {
                public static implicit operator string(DCPowerDivisor_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCPowerDivisor.DCPowerDivisor_name, ClusterDesired);
                }
            }
            public class DCPowerDivisor_Report
            {
                public static implicit operator string(DCPowerDivisor_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_DCPowerDivisor.DCPowerDivisor_name, ClusterReported);
                }
            }
            public class DCPowerDivisor_SupportCommands
            {
                public static implicit operator string(DCPowerDivisor_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_ACFrequency
        {
            public const string ACFrequency_name = "ACFrequency";
            public class ACFrequency_Attributes
            {
                public static implicit operator string(ACFrequency_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_ACFrequency command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_ACFrequency.ACFrequency_name);
            }
            public class ACFrequency_Desired
            {
                public static implicit operator string(ACFrequency_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_ACFrequency.ACFrequency_name, ClusterDesired);
                }
            }
            public class ACFrequency_Report
            {
                public static implicit operator string(ACFrequency_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_ACFrequency.ACFrequency_name, ClusterReported);
                }
            }
            public class ACFrequency_SupportCommands
            {
                public static implicit operator string(ACFrequency_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_ACFrequencyMin
        {
            public const string ACFrequencyMin_name = "ACFrequencyMin";
            public class ACFrequencyMin_Attributes
            {
                public static implicit operator string(ACFrequencyMin_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_ACFrequencyMin command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_ACFrequencyMin.ACFrequencyMin_name);
            }
            public class ACFrequencyMin_Desired
            {
                public static implicit operator string(ACFrequencyMin_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_ACFrequencyMin.ACFrequencyMin_name, ClusterDesired);
                }
            }
            public class ACFrequencyMin_Report
            {
                public static implicit operator string(ACFrequencyMin_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_ACFrequencyMin.ACFrequencyMin_name, ClusterReported);
                }
            }
            public class ACFrequencyMin_SupportCommands
            {
                public static implicit operator string(ACFrequencyMin_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_ACFrequencyMax
        {
            public const string ACFrequencyMax_name = "ACFrequencyMax";
            public class ACFrequencyMax_Attributes
            {
                public static implicit operator string(ACFrequencyMax_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_ACFrequencyMax command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_ACFrequencyMax.ACFrequencyMax_name);
            }
            public class ACFrequencyMax_Desired
            {
                public static implicit operator string(ACFrequencyMax_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_ACFrequencyMax.ACFrequencyMax_name, ClusterDesired);
                }
            }
            public class ACFrequencyMax_Report
            {
                public static implicit operator string(ACFrequencyMax_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_ACFrequencyMax.ACFrequencyMax_name, ClusterReported);
                }
            }
            public class ACFrequencyMax_SupportCommands
            {
                public static implicit operator string(ACFrequencyMax_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_NeutralCurrent
        {
            public const string NeutralCurrent_name = "NeutralCurrent";
            public class NeutralCurrent_Attributes
            {
                public static implicit operator string(NeutralCurrent_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_NeutralCurrent command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_NeutralCurrent.NeutralCurrent_name);
            }
            public class NeutralCurrent_Desired
            {
                public static implicit operator string(NeutralCurrent_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_NeutralCurrent.NeutralCurrent_name, ClusterDesired);
                }
            }
            public class NeutralCurrent_Report
            {
                public static implicit operator string(NeutralCurrent_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_NeutralCurrent.NeutralCurrent_name, ClusterReported);
                }
            }
            public class NeutralCurrent_SupportCommands
            {
                public static implicit operator string(NeutralCurrent_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_TotalActiveCurrent
        {
            public const string TotalActiveCurrent_name = "TotalActiveCurrent";
            public class TotalActiveCurrent_Attributes
            {
                public static implicit operator string(TotalActiveCurrent_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_TotalActiveCurrent command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_TotalActiveCurrent.TotalActiveCurrent_name);
            }
            public class TotalActiveCurrent_Desired
            {
                public static implicit operator string(TotalActiveCurrent_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_TotalActiveCurrent.TotalActiveCurrent_name, ClusterDesired);
                }
            }
            public class TotalActiveCurrent_Report
            {
                public static implicit operator string(TotalActiveCurrent_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_TotalActiveCurrent.TotalActiveCurrent_name, ClusterReported);
                }
            }
            public class TotalActiveCurrent_SupportCommands
            {
                public static implicit operator string(TotalActiveCurrent_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_TotalReactiveCurrent
        {
            public const string TotalReactiveCurrent_name = "TotalReactiveCurrent";
            public class TotalReactiveCurrent_Attributes
            {
                public static implicit operator string(TotalReactiveCurrent_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_TotalReactiveCurrent command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_TotalReactiveCurrent.TotalReactiveCurrent_name);
            }
            public class TotalReactiveCurrent_Desired
            {
                public static implicit operator string(TotalReactiveCurrent_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_TotalReactiveCurrent.TotalReactiveCurrent_name, ClusterDesired);
                }
            }
            public class TotalReactiveCurrent_Report
            {
                public static implicit operator string(TotalReactiveCurrent_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_TotalReactiveCurrent.TotalReactiveCurrent_name, ClusterReported);
                }
            }
            public class TotalReactiveCurrent_SupportCommands
            {
                public static implicit operator string(TotalReactiveCurrent_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_TotalApparentPower
        {
            public const string TotalApparentPower_name = "TotalApparentPower";
            public class TotalApparentPower_Attributes
            {
                public static implicit operator string(TotalApparentPower_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_TotalApparentPower command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_TotalApparentPower.TotalApparentPower_name);
            }
            public class TotalApparentPower_Desired
            {
                public static implicit operator string(TotalApparentPower_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_TotalApparentPower.TotalApparentPower_name, ClusterDesired);
                }
            }
            public class TotalApparentPower_Report
            {
                public static implicit operator string(TotalApparentPower_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_TotalApparentPower.TotalApparentPower_name, ClusterReported);
                }
            }
            public class TotalApparentPower_SupportCommands
            {
                public static implicit operator string(TotalApparentPower_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        //------------HARMONIC STUFF----------------
        public class ATT_ACFrequencyMultiplier
        {
            public const string ACFrequencyMultiplier_name = "ACFrequencyMultiplier";
            public class ACFrequencyMultiplier_Attributes
            {
                public static implicit operator string(ACFrequencyMultiplier_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_ACFrequencyMultiplier command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_ACFrequencyMultiplier.ACFrequencyMultiplier_name);
            }
            public class ACFrequencyMultiplier_Desired
            {
                public static implicit operator string(ACFrequencyMultiplier_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_ACFrequencyMultiplier.ACFrequencyMultiplier_name, ClusterDesired);
                }
            }
            public class ACFrequencyMultiplier_Report
            {
                public static implicit operator string(ACFrequencyMultiplier_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_ACFrequencyMultiplier.ACFrequencyMultiplier_name, ClusterReported);
                }
            }
            public class ACFrequencyMultiplier_SupportCommands
            {
                public static implicit operator string(ACFrequencyMultiplier_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_ACFrequencyDivisor
        {
            public const string ACFrequencyDivisor_name = "ACFrequencyDivisor";
            public class ACFrequencyDivisor_Attributes
            {
                public static implicit operator string(ACFrequencyDivisor_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_ACFrequencyDivisor command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_ACFrequencyDivisor.ACFrequencyDivisor_name);
            }
            public class ACFrequencyDivisor_Desired
            {
                public static implicit operator string(ACFrequencyDivisor_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_ACFrequencyDivisor.ACFrequencyDivisor_name, ClusterDesired);
                }
            }
            public class ACFrequencyDivisor_Report
            {
                public static implicit operator string(ACFrequencyDivisor_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_ACFrequencyDivisor.ACFrequencyDivisor_name, ClusterReported);
                }
            }
            public class ACFrequencyDivisor_SupportCommands
            {
                public static implicit operator string(ACFrequencyDivisor_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_PowerMultiplier
        {
            public const string PowerMultiplier_name = "PowerMultiplier";
            public class PowerMultiplier_Attributes
            {
                public static implicit operator string(PowerMultiplier_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_PowerMultiplier command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_PowerMultiplier.PowerMultiplier_name);
            }
            public class PowerMultiplier_Desired
            {
                public static implicit operator string(PowerMultiplier_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_PowerMultiplier.PowerMultiplier_name, ClusterDesired);
                }
            }
            public class PowerMultiplier_Report
            {
                public static implicit operator string(PowerMultiplier_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_PowerMultiplier.PowerMultiplier_name, ClusterReported);
                }
            }
            public class PowerMultiplier_SupportCommands
            {
                public static implicit operator string(PowerMultiplier_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_PowerDivisor
        {
            public const string PowerDivisor_name = "PowerDivisor";
            public class PowerDivisor_Attributes
            {
                public static implicit operator string(PowerDivisor_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_PowerDivisor command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_PowerDivisor.PowerDivisor_name);
            }
            public class PowerDivisor_Desired
            {
                public static implicit operator string(PowerDivisor_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_PowerDivisor.PowerDivisor_name, ClusterDesired);
                }
            }
            public class PowerDivisor_Report
            {
                public static implicit operator string(PowerDivisor_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_PowerDivisor.PowerDivisor_name, ClusterReported);
                }
            }
            public class PowerDivisor_SupportCommands
            {
                public static implicit operator string(PowerDivisor_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_PhaseHarmonicCurrentMultplier
        {
            public const string PhaseHarmonicCurrentMultplier_name = "PhaseHarmonicCurrentMultplier";
            public class PhaseHarmonicCurrentMultplier_Attributes
            {
                public static implicit operator string(PhaseHarmonicCurrentMultplier_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_PhaseHarmonicCurrentMultplier command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_PhaseHarmonicCurrentMultplier.PhaseHarmonicCurrentMultplier_name);
            }
            public class PhaseHarmonicCurrentMultplier_Desired
            {
                public static implicit operator string(PhaseHarmonicCurrentMultplier_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_PhaseHarmonicCurrentMultplier.PhaseHarmonicCurrentMultplier_name, ClusterDesired);
                }
            }
            public class PhaseHarmonicCurrentMultplier_Report
            {
                public static implicit operator string(PhaseHarmonicCurrentMultplier_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_PhaseHarmonicCurrentMultplier.PhaseHarmonicCurrentMultplier_name, ClusterReported);
                }
            }
            public class PhaseHarmonicCurrentMultplier_SupportCommands
            {
                public static implicit operator string(PhaseHarmonicCurrentMultplier_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_LineCurrent
        {
            public const string LineCurrent_name = "LineCurrent";
            public class LineCurrent_Attributes
            {
                public static implicit operator string(LineCurrent_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_LineCurrent command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_LineCurrent.LineCurrent_name);
            }
            public class LineCurrent_Desired
            {
                public static implicit operator string(LineCurrent_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_LineCurrent.LineCurrent_name, ClusterDesired);
                }
            }
            public class LineCurrent_Report
            {
                public static implicit operator string(LineCurrent_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_LineCurrent.LineCurrent_name, ClusterReported);
                }
            }
            public class LineCurrent_SupportCommands
            {
                public static implicit operator string(LineCurrent_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_ActiveCurrent
        {
            public const string ActiveCurrent_name = "ActiveCurrent";
            public class ActiveCurrent_Attributes
            {
                public static implicit operator string(ActiveCurrent_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_ActiveCurrent command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_ActiveCurrent.ActiveCurrent_name);
            }
            public class ActiveCurrent_Desired
            {
                public static implicit operator string(ActiveCurrent_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_ActiveCurrent.ActiveCurrent_name, ClusterDesired);
                }
            }
            public class ActiveCurrent_Report
            {
                public static implicit operator string(ActiveCurrent_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_ActiveCurrent.ActiveCurrent_name, ClusterReported);
                }
            }
            public class ActiveCurrent_SupportCommands
            {
                public static implicit operator string(ActiveCurrent_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_ReactiveCurrent
        {
            public const string ReactiveCurrent_name = "ReactiveCurrent";
            public class ReactiveCurrent_Attributes
            {
                public static implicit operator string(ReactiveCurrent_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_ReactiveCurrent command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_ReactiveCurrent.ReactiveCurrent_name);
            }
            public class ReactiveCurrent_Desired
            {
                public static implicit operator string(ReactiveCurrent_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_ReactiveCurrent.ReactiveCurrent_name, ClusterDesired);
                }
            }
            public class ReactiveCurrent_Report
            {
                public static implicit operator string(ReactiveCurrent_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_ReactiveCurrent.ReactiveCurrent_name, ClusterReported);
                }
            }
            public class ReactiveCurrent_SupportCommands
            {
                public static implicit operator string(ReactiveCurrent_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_RMSVoltage
        {
            public const string RMSVoltage_name = "RMSVoltage";
            public class RMSVoltage_Attributes
            {
                public static implicit operator string(RMSVoltage_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_RMSVoltage command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_RMSVoltage.RMSVoltage_name);
            }
            public class RMSVoltage_Desired
            {
                public static implicit operator string(RMSVoltage_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_RMSVoltage.RMSVoltage_name, ClusterDesired);
                }
            }
            public class RMSVoltage_Report
            {
                public static implicit operator string(RMSVoltage_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_RMSVoltage.RMSVoltage_name, ClusterReported);
                }
            }
            public class RMSVoltage_SupportCommands
            {
                public static implicit operator string(RMSVoltage_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_RMSVoltageMin
        {
            public const string RMSVoltageMin_name = "RMSVoltageMin";
            public class RMSVoltageMin_Attributes
            {
                public static implicit operator string(RMSVoltageMin_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_RMSVoltageMin command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_RMSVoltageMin.RMSVoltageMin_name);
            }
            public class RMSVoltageMin_Desired
            {
                public static implicit operator string(RMSVoltageMin_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_RMSVoltageMin.RMSVoltageMin_name, ClusterDesired);
                }
            }
            public class RMSVoltageMin_Report
            {
                public static implicit operator string(RMSVoltageMin_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_RMSVoltageMin.RMSVoltageMin_name, ClusterReported);
                }
            }
            public class RMSVoltageMin_SupportCommands
            {
                public static implicit operator string(RMSVoltageMin_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_RMSVoltageMax
        {
            public const string RMSVoltageMax_name = "RMSVoltageMax";
            public class RMSVoltageMax_Attributes
            {
                public static implicit operator string(RMSVoltageMax_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_RMSVoltageMax command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_RMSVoltageMax.RMSVoltageMax_name);
            }
            public class RMSVoltageMax_Desired
            {
                public static implicit operator string(RMSVoltageMax_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_RMSVoltageMax.RMSVoltageMax_name, ClusterDesired);
                }
            }
            public class RMSVoltageMax_Report
            {
                public static implicit operator string(RMSVoltageMax_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_RMSVoltageMax.RMSVoltageMax_name, ClusterReported);
                }
            }
            public class RMSVoltageMax_SupportCommands
            {
                public static implicit operator string(RMSVoltageMax_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_RMSCurrent
        {
            public const string RMSCurrent_name = "RMSCurrent";
            public class RMSCurrent_Attributes
            {
                public static implicit operator string(RMSCurrent_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_RMSCurrent command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_RMSCurrent.RMSCurrent_name);
            }
            public class RMSCurrent_Desired
            {
                public static implicit operator string(RMSCurrent_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_RMSCurrent.RMSCurrent_name, ClusterDesired);
                }
            }
            public class RMSCurrent_Report
            {
                public static implicit operator string(RMSCurrent_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_RMSCurrent.RMSCurrent_name, ClusterReported);
                }
            }
            public class RMSCurrent_SupportCommands
            {
                public static implicit operator string(RMSCurrent_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/ //38.1.48
        public class ATT_ActivePower
        {
            public const string ActivePower_name = "ActivePower";
            public class ActivePower_Attributes
            {
                public static implicit operator string(ActivePower_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_ActivePower command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_ActivePower.ActivePower_name);
            }
            public class ActivePower_Desired
            {
                public static implicit operator string(ActivePower_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_ActivePower.ActivePower_name, ClusterDesired);
                }
            }
            public class ActivePower_Report
            {
                public static implicit operator string(ActivePower_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_ActivePower.ActivePower_name, ClusterReported);
                }
            }
            public class ActivePower_SupportCommands
            {
                public static implicit operator string(ActivePower_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_ReactivePower
        {
            public const string ReactivePower_name = "ReactivePower";
            public class ReactivePower_Attributes
            {
                public static implicit operator string(ReactivePower_Attributes command)
                {
                    return SumAllAtt(ElectricalMeasurementCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_ReactivePower command)
            {
                return SumATTStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_ReactivePower.ReactivePower_name);
            }
            public class ReactivePower_Desired
            {
                public static implicit operator string(ReactivePower_Desired command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_ReactivePower.ReactivePower_name, ClusterDesired);
                }
            }
            public class ReactivePower_Report
            {
                public static implicit operator string(ReactivePower_Report command)
                {
                    return SumATTStr_WithSubValue(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.ATT_ReactivePower.ReactivePower_name, ClusterReported);
                }
            }
            public class ReactivePower_SupportCommands
            {
                public static implicit operator string(ReactivePower_SupportCommands command)
                {
                    return SumSupCMD(ElectricalMeasurementCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        #endregion

        #region Cluster Commands

        /*CLASSES AND ENUMS FOR COMMANDS*/
        public enum ProfileIntervalPeriod
        {
            Daily,
            t60Minutes,
            t30Minutes,
            t15Minutes,
            t10Minutes,
            t7dot5Minutes,
            t5Minutes,
            t2dot5Minutes
        }
        public enum GetMeasurementProfileResponseStatus
        {
            Success,
            AttributeProfileNotSupported,
            InvalidStartTime,
            MoreIntervalsRequestedThanCanBeReturned,
            NoIntervalAvailableForTheRequestedTime
        }
        public class attribId
        {
            public int id { get; set; }
        } 
        /*SUPPORTED COMMANDS*/
        public class CMD_GetProfileInfoResponse
        {
            public const string GetProfileInfoResponse_name = "GetProfileInfoResponse";
            public static implicit operator string(CMD_GetProfileInfoResponse command)
            {
                return SumCMDStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.CMD_GetProfileInfoResponse.GetProfileInfoResponse_name);
            }
            public class CMD_GetProfileInfoResponse_Payload
            {
                public int ProfileCount { get; set; }
                [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
                public ProfileIntervalPeriod ProfileIntervalPeriod { get; set; }
                public int MaxNumberOfIntervals { get; set; }
                public attribId[] ListOfAttributes { get; set; }
                /*Start of public Payload*/
                public CMD_GetProfileInfoResponse_Payload(int my_ProfileCount, ProfileIntervalPeriod my_ProfileIntervalPeriod, int my_MaxNumberOfIntervals, attribId[] my_ListOfAttributes )
                {
                    ProfileCount = my_ProfileCount;
                    ProfileIntervalPeriod = my_ProfileIntervalPeriod;
                    MaxNumberOfIntervals = my_MaxNumberOfIntervals;
                    ListOfAttributes = my_ListOfAttributes;
                }/*End of public Payload*/
                public static implicit operator CMD_GetProfileInfoResponse_Payload(string command)
                {
                    return JsonConvert.DeserializeObject<CMD_GetProfileInfoResponse_Payload>(command);
                }
                public static implicit operator string(CMD_GetProfileInfoResponse_Payload command)
                {
                    return JsonConvert.SerializeObject(command);
                }
            }/*End Payload class*/
        }/*End of CMD class with Childnodes*/
        public class CMD_GetProfileInfo
        {
            public const string GetProfileInfo_name = "GetProfileInfo";
            public static implicit operator string(CMD_GetProfileInfo command)
            {
                return SumCMDStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.CMD_GetProfileInfo.GetProfileInfo_name);
            }
        }/*End of CMD class with Childnodes*/
        public class CMD_GetMeasurementProfileResponse
        {
            public const string GetMeasurementProfileResponse_name = "GetMeasurementProfileResponse";
            public static implicit operator string(CMD_GetMeasurementProfileResponse command)
            {
                return SumCMDStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.CMD_GetMeasurementProfileResponse.GetMeasurementProfileResponse_name);
            }
            public class GetMeasurementProfileResponse_Payload
            {
                public string StartTime { get; set; } //NOT SURE
                [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
                public GetMeasurementProfileResponseStatus GetMeasurementProfileResponseStatus { get; set; }
                [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
                public ProfileIntervalPeriod ProfileIntervalPeriod { get; set; }
                public int NumberOfIntervalsDelivered { get; set; }
                public attribId AttributeId { get; set; }
                public int[] Intervals { get; set; }
                /*Start of public Payload*/
                public GetMeasurementProfileResponse_Payload(string my_StartTime, GetMeasurementProfileResponseStatus my_GetMeasurementProfileResponseStatus, ProfileIntervalPeriod my_ProfileIntervalPeriod, int my_NumberOfIntervalsDelivered,
                   attribId my_AttributeId, int[] my_Intervals )
                {
                    StartTime = my_StartTime;
                    GetMeasurementProfileResponseStatus = my_GetMeasurementProfileResponseStatus;
                    NumberOfIntervalsDelivered = my_NumberOfIntervalsDelivered;
                    AttributeId = my_AttributeId;
                    Intervals = my_Intervals;
                }/*End of public Payload*/
                public static implicit operator GetMeasurementProfileResponse_Payload(string command)
                {
                    return JsonConvert.DeserializeObject<GetMeasurementProfileResponse_Payload>(command);
                }
                public static implicit operator string(GetMeasurementProfileResponse_Payload command)
                {
                    return JsonConvert.SerializeObject(command);
                }
            }/*End Payload class*/
        }/*End of CMD class with Childnodes*/
        public class CMD_GetMeasurementProfile
        {
            public const string GetMeasurementProfile_name = "GetMeasurementProfile";
            public static implicit operator string(CMD_GetMeasurementProfile command)
            {
                return SumCMDStr(ElectricalMeasurementCluster.ClusterName, ElectricalMeasurementCluster.CMD_GetMeasurementProfile.GetMeasurementProfile_name);
            }
            public class GetMeasurementProfile_Payload
            {
                public attribId AttributeID { get; set; }
                public string StartTime { get; set; } //NOT SURE
                public int NumberOfIntervals { get; set; }
                /*Start of public Payload*/
                public GetMeasurementProfile_Payload(attribId my_AttributeID, string my_StartTime, int my_NumberOfIntervals)
                {
                    AttributeID = my_AttributeID;
                    StartTime = my_StartTime;
                    NumberOfIntervals = my_NumberOfIntervals;
                }/*End of public Payload*/
                public static implicit operator GetMeasurementProfile_Payload(string command)
                {
                    return JsonConvert.DeserializeObject<GetMeasurementProfile_Payload>(command);
                }
                public static implicit operator string(GetMeasurementProfile_Payload command)
                {
                    return JsonConvert.SerializeObject(command);
                }
            }/*End Payload class*/
        }/*End of CMD class with Childnodes*/
        #endregion

        #region Cluster Operations

        public class Payload_Att_SupportedCommands
        {
            public class Payload
            {
                public List<string> value { get; set; }
            }

            public Payload _payload { get; set; }

            public Payload_Att_SupportedCommands(Payload payload)
            {
                _payload = payload;
            }

            public Payload_Att_SupportedCommands()
            {
            }

            public static implicit operator Payload_Att_SupportedCommands(string options)
            {
                Payload deserialized = JsonConvert.DeserializeObject<Payload>(options);
                return new Payload_Att_SupportedCommands(deserialized);
            }
        }

        public struct Payload_Att_Value
        {
            public string value;

            public static implicit operator Payload_Att_Value(string options)
            {
                return JsonConvert.DeserializeObject<Payload_Att_Value>(options);
            }
        }

        public static implicit operator ElectricalMeasurementCluster(string options)
        {
            return JsonConvert.DeserializeObject<ElectricalMeasurementCluster>(options);
        }
        public static implicit operator string(ElectricalMeasurementCluster command)
        {
            return ElectricalMeasurementCluster.ClusterName;
        }
        private static string SumAllAtt(string AttClusterName, string Att)
        {
            string SumStr = "/" + AttClusterName + "/" + Att + "/" + "#";
            return SumStr;
        }
        private static string SumCMDStr(string ClusterName, string CmdName)
        {
            string SumStr = "/" + ClusterName + "/" + "Commands" + "/" + CmdName;
            return SumStr;
        }

        private static string SumATTStr(string ClusterName, string AttName)
        {
            string SumStr = "/" + ClusterName + "/" + "Attributes" + "/" + AttName;
            return SumStr;
        }
        private static string SumATTStr_WithSubValue(string ClusterName, string AttName, string SubValue)
        {
            string SumStr = SumATTStr(ClusterName, AttName) + "/" + SubValue;
            return SumStr;
        }
        private static string SumSupCMD(string ClusterName, string SupCMD)
        {
            string SumStr = "/" + ClusterName + "/" + SupCMD;
            return SumStr;
        }
        #endregion
    }
}
