// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Newtonsoft.Json;
using System.Collections.Generic;

namespace UicApplication.Clusters
{
    public class MeteringCluster
    {
        public const string ClusterName = "Metering";
        public const string ClusterDesired = "Desired";
        public const string ClusterReported = "Reported";
        public const string ClusterSupportedCommands = "SupportedCommands";
        public const string ClusterAttributes = "Attributes";

        #region Cluster Attributes

        public class ATT_CurrentSummationDelivered
        {
            public const string CurrentSummationDelivered_name = "CurrentSummationDelivered";
            public class CurrentSummationDelivered_Attributes
            {
                public static implicit operator string(CurrentSummationDelivered_Attributes command)
                {
                    return SumAllAtt(MeteringCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_CurrentSummationDelivered command)
            {
                return SumATTStr(MeteringCluster.ClusterName, MeteringCluster.ATT_CurrentSummationDelivered.CurrentSummationDelivered_name);
            }
            public class CurrentSummationDelivered_Desired
            {
                public static implicit operator string(CurrentSummationDelivered_Desired command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_CurrentSummationDelivered.CurrentSummationDelivered_name, ClusterDesired);
                }
            }
            public class CurrentSummationDelivered_Report
            {
                public static implicit operator string(CurrentSummationDelivered_Report command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_CurrentSummationDelivered.CurrentSummationDelivered_name, ClusterReported);
                }
            }
            public class CurrentSummationDelivered_SupportCommands
            {
                public static implicit operator string(CurrentSummationDelivered_SupportCommands command)
                {
                    return SumSupCMD(MeteringCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_CurrentSummationReceived
        {
            public const string CurrentSummationReceived_name = "CurrentSummationReceived";
            public class CurrentSummationReceived_Attributes
            {
                public static implicit operator string(CurrentSummationReceived_Attributes command)
                {
                    return SumAllAtt(MeteringCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_CurrentSummationReceived command)
            {
                return SumATTStr(MeteringCluster.ClusterName, MeteringCluster.ATT_CurrentSummationReceived.CurrentSummationReceived_name);
            }
            public class CurrentSummationReceived_Desired
            {
                public static implicit operator string(CurrentSummationReceived_Desired command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_CurrentSummationReceived.CurrentSummationReceived_name, ClusterDesired);
                }
            }
            public class CurrentSummationReceived_Report
            {
                public static implicit operator string(CurrentSummationReceived_Report command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_CurrentSummationReceived.CurrentSummationReceived_name, ClusterReported);
                }
            }
            public class CurrentSummationReceived_SupportCommands
            {
                public static implicit operator string(CurrentSummationReceived_SupportCommands command)
                {
                    return SumSupCMD(MeteringCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_CurrentMaxDemandDelivered
        {
            public const string CurrentMaxDemandDelivered_name = "CurrentMaxDemandDelivered";
            public class CurrentMaxDemandDelivered_Attributes
            {
                public static implicit operator string(CurrentMaxDemandDelivered_Attributes command)
                {
                    return SumAllAtt(MeteringCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_CurrentMaxDemandDelivered command)
            {
                return SumATTStr(MeteringCluster.ClusterName, MeteringCluster.ATT_CurrentMaxDemandDelivered.CurrentMaxDemandDelivered_name);
            }
            public class CurrentMaxDemandDelivered_Desired
            {
                public static implicit operator string(CurrentMaxDemandDelivered_Desired command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_CurrentMaxDemandDelivered.CurrentMaxDemandDelivered_name, ClusterDesired);
                }
            }
            public class CurrentMaxDemandDelivered_Report
            {
                public static implicit operator string(CurrentMaxDemandDelivered_Report command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_CurrentMaxDemandDelivered.CurrentMaxDemandDelivered_name, ClusterReported);
                }
            }
            public class CurrentMaxDemandDelivered_SupportCommands
            {
                public static implicit operator string(CurrentMaxDemandDelivered_SupportCommands command)
                {
                    return SumSupCMD(MeteringCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_CurrentMaxDemandReceived
        {
            public const string CurrentMaxDemandReceived_name = "CurrentMaxDemandReceived";
            public class CurrentMaxDemandReceived_Attributes
            {
                public static implicit operator string(CurrentMaxDemandReceived_Attributes command)
                {
                    return SumAllAtt(MeteringCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_CurrentMaxDemandReceived command)
            {
                return SumATTStr(MeteringCluster.ClusterName, MeteringCluster.ATT_CurrentMaxDemandReceived.CurrentMaxDemandReceived_name);
            }
            public class CurrentMaxDemandReceived_Desired
            {
                public static implicit operator string(CurrentMaxDemandReceived_Desired command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_CurrentMaxDemandReceived.CurrentMaxDemandReceived_name, ClusterDesired);
                }
            }
            public class CurrentMaxDemandReceived_Report
            {
                public static implicit operator string(CurrentMaxDemandReceived_Report command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_CurrentMaxDemandReceived.CurrentMaxDemandReceived_name, ClusterReported);
                }
            }
            public class CurrentMaxDemandReceived_SupportCommands
            {
                public static implicit operator string(CurrentMaxDemandReceived_SupportCommands command)
                {
                    return SumSupCMD(MeteringCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_PowerFactor
        {
            public const string PowerFactor_name = "PowerFactor";
            public class PowerFactor_Attributes
            {
                public static implicit operator string(PowerFactor_Attributes command)
                {
                    return SumAllAtt(MeteringCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_PowerFactor command)
            {
                return SumATTStr(MeteringCluster.ClusterName, MeteringCluster.ATT_PowerFactor.PowerFactor_name);
            }
            public class PowerFactor_Desired
            {
                public static implicit operator string(PowerFactor_Desired command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_PowerFactor.PowerFactor_name, ClusterDesired);
                }
            }
            public class PowerFactor_Report
            {
                public static implicit operator string(PowerFactor_Report command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_PowerFactor.PowerFactor_name, ClusterReported);
                }
            }
            public class PowerFactor_SupportCommands
            {
                public static implicit operator string(PowerFactor_SupportCommands command)
                {
                    return SumSupCMD(MeteringCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_ReadingSnapShotTime
        {
            public const string ReadingSnapShotTime_name = "ReadingSnapShotTime";
            public class ReadingSnapShotTime_Attributes
            {
                public static implicit operator string(ReadingSnapShotTime_Attributes command)
                {
                    return SumAllAtt(MeteringCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_ReadingSnapShotTime command)
            {
                return SumATTStr(MeteringCluster.ClusterName, MeteringCluster.ATT_ReadingSnapShotTime.ReadingSnapShotTime_name);
            }
            public class ReadingSnapShotTime_Desired
            {
                public static implicit operator string(ReadingSnapShotTime_Desired command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_ReadingSnapShotTime.ReadingSnapShotTime_name, ClusterDesired);
                }
            }
            public class ReadingSnapShotTime_Report
            {
                public static implicit operator string(ReadingSnapShotTime_Report command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_ReadingSnapShotTime.ReadingSnapShotTime_name, ClusterReported);
                }
            }
            public class ReadingSnapShotTime_SupportCommands
            {
                public static implicit operator string(ReadingSnapShotTime_SupportCommands command)
                {
                    return SumSupCMD(MeteringCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_CurrentMaxDemandDeliveredTime
        {
            public const string CurrentMaxDemandDeliveredTime_name = "CurrentMaxDemandDeliveredTime";
            public class CurrentMaxDemandDeliveredTime_Attributes
            {
                public static implicit operator string(CurrentMaxDemandDeliveredTime_Attributes command)
                {
                    return SumAllAtt(MeteringCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_CurrentMaxDemandDeliveredTime command)
            {
                return SumATTStr(MeteringCluster.ClusterName, MeteringCluster.ATT_CurrentMaxDemandDeliveredTime.CurrentMaxDemandDeliveredTime_name);
            }
            public class CurrentMaxDemandDeliveredTime_Desired
            {
                public static implicit operator string(CurrentMaxDemandDeliveredTime_Desired command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_CurrentMaxDemandDeliveredTime.CurrentMaxDemandDeliveredTime_name, ClusterDesired);
                }
            }
            public class CurrentMaxDemandDeliveredTime_Report
            {
                public static implicit operator string(CurrentMaxDemandDeliveredTime_Report command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_CurrentMaxDemandDeliveredTime.CurrentMaxDemandDeliveredTime_name, ClusterReported);
                }
            }
            public class CurrentMaxDemandDeliveredTime_SupportCommands
            {
                public static implicit operator string(CurrentMaxDemandDeliveredTime_SupportCommands command)
                {
                    return SumSupCMD(MeteringCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_CurrentMaxDemandReceivedTime
        {
            public const string CurrentMaxDemandReceivedTime_name = "CurrentMaxDemandReceivedTime";
            public class CurrentMaxDemandReceivedTime_Attributes
            {
                public static implicit operator string(CurrentMaxDemandReceivedTime_Attributes command)
                {
                    return SumAllAtt(MeteringCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_CurrentMaxDemandReceivedTime command)
            {
                return SumATTStr(MeteringCluster.ClusterName, MeteringCluster.ATT_CurrentMaxDemandReceivedTime.CurrentMaxDemandReceivedTime_name);
            }
            public class CurrentMaxDemandReceivedTime_Desired
            {
                public static implicit operator string(CurrentMaxDemandReceivedTime_Desired command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_CurrentMaxDemandReceivedTime.CurrentMaxDemandReceivedTime_name, ClusterDesired);
                }
            }
            public class CurrentMaxDemandReceivedTime_Report
            {
                public static implicit operator string(CurrentMaxDemandReceivedTime_Report command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_CurrentMaxDemandReceivedTime.CurrentMaxDemandReceivedTime_name, ClusterReported);
                }
            }
            public class CurrentMaxDemandReceivedTime_SupportCommands
            {
                public static implicit operator string(CurrentMaxDemandReceivedTime_SupportCommands command)
                {
                    return SumSupCMD(MeteringCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_DefaultUpdatePeriod
        {
            public const string DefaultUpdatePeriod_name = "DefaultUpdatePeriod";
            public class DefaultUpdatePeriod_Attributes
            {
                public static implicit operator string(DefaultUpdatePeriod_Attributes command)
                {
                    return SumAllAtt(MeteringCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_DefaultUpdatePeriod command)
            {
                return SumATTStr(MeteringCluster.ClusterName, MeteringCluster.ATT_DefaultUpdatePeriod.DefaultUpdatePeriod_name);
            }
            public class DefaultUpdatePeriod_Desired
            {
                public static implicit operator string(DefaultUpdatePeriod_Desired command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_DefaultUpdatePeriod.DefaultUpdatePeriod_name, ClusterDesired);
                }
            }
            public class DefaultUpdatePeriod_Report
            {
                public static implicit operator string(DefaultUpdatePeriod_Report command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_DefaultUpdatePeriod.DefaultUpdatePeriod_name, ClusterReported);
                }
            }
            public class DefaultUpdatePeriod_SupportCommands
            {
                public static implicit operator string(DefaultUpdatePeriod_SupportCommands command)
                {
                    return SumSupCMD(MeteringCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_SupplyStatus
        {
            public const string SupplyStatus_name = "SupplyStatus";
            public class SupplyStatus_Attributes
            {
                public static implicit operator string(SupplyStatus_Attributes command)
                {
                    return SumAllAtt(MeteringCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_SupplyStatus command)
            {
                return SumATTStr(MeteringCluster.ClusterName, MeteringCluster.ATT_SupplyStatus.SupplyStatus_name);
            }
            public class SupplyStatus_Desired
            {
                public static implicit operator string(SupplyStatus_Desired command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_SupplyStatus.SupplyStatus_name, ClusterDesired);
                }
            }
            public class SupplyStatus_Report
            {
                public static implicit operator string(SupplyStatus_Report command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_SupplyStatus.SupplyStatus_name, ClusterReported);
                }
            }
            public class SupplyStatus_SupportCommands
            {
                public static implicit operator string(SupplyStatus_SupportCommands command)
                {
                    return SumSupCMD(MeteringCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_CurrentInletEnergyCarrierSummation
        {
            public const string CurrentInletEnergyCarrierSummation_name = "CurrentInletEnergyCarrierSummation";
            public class CurrentInletEnergyCarrierSummation_Attributes
            {
                public static implicit operator string(CurrentInletEnergyCarrierSummation_Attributes command)
                {
                    return SumAllAtt(MeteringCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_CurrentInletEnergyCarrierSummation command)
            {
                return SumATTStr(MeteringCluster.ClusterName, MeteringCluster.ATT_CurrentInletEnergyCarrierSummation.CurrentInletEnergyCarrierSummation_name);
            }
            public class CurrentInletEnergyCarrierSummation_Desired
            {
                public static implicit operator string(CurrentInletEnergyCarrierSummation_Desired command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_CurrentInletEnergyCarrierSummation.CurrentInletEnergyCarrierSummation_name, ClusterDesired);
                }
            }
            public class CurrentInletEnergyCarrierSummation_Report
            {
                public static implicit operator string(CurrentInletEnergyCarrierSummation_Report command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_CurrentInletEnergyCarrierSummation.CurrentInletEnergyCarrierSummation_name, ClusterReported);
                }
            }
            public class CurrentInletEnergyCarrierSummation_SupportCommands
            {
                public static implicit operator string(CurrentInletEnergyCarrierSummation_SupportCommands command)
                {
                    return SumSupCMD(MeteringCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_CurrentOutletEnergyCarrierSummation
        {
            public const string CurrentOutletEnergyCarrierSummation_name = "CurrentOutletEnergyCarrierSummation";
            public class CurrentOutletEnergyCarrierSummation_Attributes
            {
                public static implicit operator string(CurrentOutletEnergyCarrierSummation_Attributes command)
                {
                    return SumAllAtt(MeteringCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_CurrentOutletEnergyCarrierSummation command)
            {
                return SumATTStr(MeteringCluster.ClusterName, MeteringCluster.ATT_CurrentOutletEnergyCarrierSummation.CurrentOutletEnergyCarrierSummation_name);
            }
            public class CurrentOutletEnergyCarrierSummation_Desired
            {
                public static implicit operator string(CurrentOutletEnergyCarrierSummation_Desired command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_CurrentOutletEnergyCarrierSummation.CurrentOutletEnergyCarrierSummation_name, ClusterDesired);
                }
            }
            public class CurrentOutletEnergyCarrierSummation_Report
            {
                public static implicit operator string(CurrentOutletEnergyCarrierSummation_Report command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_CurrentOutletEnergyCarrierSummation.CurrentOutletEnergyCarrierSummation_name, ClusterReported);
                }
            }
            public class CurrentOutletEnergyCarrierSummation_SupportCommands
            {
                public static implicit operator string(CurrentOutletEnergyCarrierSummation_SupportCommands command)
                {
                    return SumSupCMD(MeteringCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_InletTemperature
        {
            public const string InletTemperature_name = "InletTemperature";
            public class InletTemperature_Attributes
            {
                public static implicit operator string(InletTemperature_Attributes command)
                {
                    return SumAllAtt(MeteringCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_InletTemperature command)
            {
                return SumATTStr(MeteringCluster.ClusterName, MeteringCluster.ATT_InletTemperature.InletTemperature_name);
            }
            public class InletTemperature_Desired
            {
                public static implicit operator string(InletTemperature_Desired command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_InletTemperature.InletTemperature_name, ClusterDesired);
                }
            }
            public class InletTemperature_Report
            {
                public static implicit operator string(InletTemperature_Report command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_InletTemperature.InletTemperature_name, ClusterReported);
                }
            }
            public class InletTemperature_SupportCommands
            {
                public static implicit operator string(InletTemperature_SupportCommands command)
                {
                    return SumSupCMD(MeteringCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_OutletTemperature
        {
            public const string OutletTemperature_name = "OutletTemperature";
            public class OutletTemperature_Attributes
            {
                public static implicit operator string(OutletTemperature_Attributes command)
                {
                    return SumAllAtt(MeteringCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_OutletTemperature command)
            {
                return SumATTStr(MeteringCluster.ClusterName, MeteringCluster.ATT_OutletTemperature.OutletTemperature_name);
            }
            public class OutletTemperature_Desired
            {
                public static implicit operator string(OutletTemperature_Desired command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_OutletTemperature.OutletTemperature_name, ClusterDesired);
                }
            }
            public class OutletTemperature_Report
            {
                public static implicit operator string(OutletTemperature_Report command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_OutletTemperature.OutletTemperature_name, ClusterReported);
                }
            }
            public class OutletTemperature_SupportCommands
            {
                public static implicit operator string(OutletTemperature_SupportCommands command)
                {
                    return SumSupCMD(MeteringCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_UnitofMeasure
        {
            public const string UnitofMeasure_name = "UnitofMeasure";
            public class UnitofMeasure_Attributes
            {
                public static implicit operator string(UnitofMeasure_Attributes command)
                {
                    return SumAllAtt(MeteringCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_UnitofMeasure command)
            {
                return SumATTStr(MeteringCluster.ClusterName, MeteringCluster.ATT_UnitofMeasure.UnitofMeasure_name);
            }
            public class UnitofMeasure_Desired
            {
                public static implicit operator string(UnitofMeasure_Desired command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_UnitofMeasure.UnitofMeasure_name, ClusterDesired);
                }
            }
            public class UnitofMeasure_Report
            {
                public static implicit operator string(UnitofMeasure_Report command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_UnitofMeasure.UnitofMeasure_name, ClusterReported);
                }
            }
            public class UnitofMeasure_SupportCommands
            {
                public static implicit operator string(UnitofMeasure_SupportCommands command)
                {
                    return SumSupCMD(MeteringCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_Multiplier
        {
            public const string Multiplier_name = "Multiplier";
            public class Multiplier_Attributes
            {
                public static implicit operator string(Multiplier_Attributes command)
                {
                    return SumAllAtt(MeteringCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_Multiplier command)
            {
                return SumATTStr(MeteringCluster.ClusterName, MeteringCluster.ATT_Multiplier.Multiplier_name);
            }
            public class Multiplier_Desired
            {
                public static implicit operator string(Multiplier_Desired command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_Multiplier.Multiplier_name, ClusterDesired);
                }
            }
            public class Multiplier_Report
            {
                public static implicit operator string(Multiplier_Report command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_Multiplier.Multiplier_name, ClusterReported);
                }
            }
            public class Multiplier_SupportCommands
            {
                public static implicit operator string(Multiplier_SupportCommands command)
                {
                    return SumSupCMD(MeteringCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_Divisor
        {
            public const string Divisor_name = "Divisor";
            public class Divisor_Attributes
            {
                public static implicit operator string(Divisor_Attributes command)
                {
                    return SumAllAtt(MeteringCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_Divisor command)
            {
                return SumATTStr(MeteringCluster.ClusterName, MeteringCluster.ATT_Divisor.Divisor_name);
            }
            public class Divisor_Desired
            {
                public static implicit operator string(Divisor_Desired command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_Divisor.Divisor_name, ClusterDesired);
                }
            }
            public class Divisor_Report
            {
                public static implicit operator string(Divisor_Report command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_Divisor.Divisor_name, ClusterReported);
                }
            }
            public class Divisor_SupportCommands
            {
                public static implicit operator string(Divisor_SupportCommands command)
                {
                    return SumSupCMD(MeteringCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_SummationFormatting
        {
            public const string SummationFormatting_name = "SummationFormatting";
            public class SummationFormatting_Attributes
            {
                public static implicit operator string(SummationFormatting_Attributes command)
                {
                    return SumAllAtt(MeteringCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_SummationFormatting command)
            {
                return SumATTStr(MeteringCluster.ClusterName, MeteringCluster.ATT_SummationFormatting.SummationFormatting_name);
            }
            public class SummationFormatting_Desired
            {
                public static implicit operator string(SummationFormatting_Desired command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_SummationFormatting.SummationFormatting_name, ClusterDesired);
                }
            }
            public class SummationFormatting_Report
            {
                public static implicit operator string(SummationFormatting_Report command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_SummationFormatting.SummationFormatting_name, ClusterReported);
                }
            }
            public class SummationFormatting_SupportCommands
            {
                public static implicit operator string(SummationFormatting_SupportCommands command)
                {
                    return SumSupCMD(MeteringCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_DemandFormatting
        {
            public const string DemandFormatting_name = "DemandFormatting";
            public class DemandFormatting_Attributes
            {
                public static implicit operator string(DemandFormatting_Attributes command)
                {
                    return SumAllAtt(MeteringCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_DemandFormatting command)
            {
                return SumATTStr(MeteringCluster.ClusterName, MeteringCluster.ATT_DemandFormatting.DemandFormatting_name);
            }
            public class DemandFormatting_Desired
            {
                public static implicit operator string(DemandFormatting_Desired command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_DemandFormatting.DemandFormatting_name, ClusterDesired);
                }
            }
            public class DemandFormatting_Report
            {
                public static implicit operator string(DemandFormatting_Report command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_DemandFormatting.DemandFormatting_name, ClusterReported);
                }
            }
            public class DemandFormatting_SupportCommands
            {
                public static implicit operator string(DemandFormatting_SupportCommands command)
                {
                    return SumSupCMD(MeteringCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_HistoricalConsumptionFormatting
        {
            public const string HistoricalConsumptionFormatting_name = "HistoricalConsumptionFormatting";
            public class HistoricalConsumptionFormatting_Attributes
            {
                public static implicit operator string(HistoricalConsumptionFormatting_Attributes command)
                {
                    return SumAllAtt(MeteringCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_HistoricalConsumptionFormatting command)
            {
                return SumATTStr(MeteringCluster.ClusterName, MeteringCluster.ATT_HistoricalConsumptionFormatting.HistoricalConsumptionFormatting_name);
            }
            public class HistoricalConsumptionFormatting_Desired
            {
                public static implicit operator string(HistoricalConsumptionFormatting_Desired command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_HistoricalConsumptionFormatting.HistoricalConsumptionFormatting_name, ClusterDesired);
                }
            }
            public class HistoricalConsumptionFormatting_Report
            {
                public static implicit operator string(HistoricalConsumptionFormatting_Report command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_HistoricalConsumptionFormatting.HistoricalConsumptionFormatting_name, ClusterReported);
                }
            }
            public class HistoricalConsumptionFormatting_SupportCommands
            {
                public static implicit operator string(HistoricalConsumptionFormatting_SupportCommands command)
                {
                    return SumSupCMD(MeteringCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_MeteringDeviceType
        {
            public const string MeteringDeviceType_name = "MeteringDeviceType";
            public class MeteringDeviceType_Attributes
            {
                public static implicit operator string(MeteringDeviceType_Attributes command)
                {
                    return SumAllAtt(MeteringCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_MeteringDeviceType command)
            {
                return SumATTStr(MeteringCluster.ClusterName, MeteringCluster.ATT_MeteringDeviceType.MeteringDeviceType_name);
            }
            public class MeteringDeviceType_Desired
            {
                public static implicit operator string(MeteringDeviceType_Desired command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_MeteringDeviceType.MeteringDeviceType_name, ClusterDesired);
                }
            }
            public class MeteringDeviceType_Report
            {
                public static implicit operator string(MeteringDeviceType_Report command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_MeteringDeviceType.MeteringDeviceType_name, ClusterReported);
                }
            }
            public class MeteringDeviceType_SupportCommands
            {
                public static implicit operator string(MeteringDeviceType_SupportCommands command)
                {
                    return SumSupCMD(MeteringCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_EnergyCarrierUnitOfMeasure
        {
            public const string EnergyCarrierUnitOfMeasure_name = "EnergyCarrierUnitOfMeasure";
            public class EnergyCarrierUnitOfMeasure_Attributes
            {
                public static implicit operator string(EnergyCarrierUnitOfMeasure_Attributes command)
                {
                    return SumAllAtt(MeteringCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_EnergyCarrierUnitOfMeasure command)
            {
                return SumATTStr(MeteringCluster.ClusterName, MeteringCluster.ATT_EnergyCarrierUnitOfMeasure.EnergyCarrierUnitOfMeasure_name);
            }
            public class EnergyCarrierUnitOfMeasure_Desired
            {
                public static implicit operator string(EnergyCarrierUnitOfMeasure_Desired command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_EnergyCarrierUnitOfMeasure.EnergyCarrierUnitOfMeasure_name, ClusterDesired);
                }
            }
            public class EnergyCarrierUnitOfMeasure_Report
            {
                public static implicit operator string(EnergyCarrierUnitOfMeasure_Report command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_EnergyCarrierUnitOfMeasure.EnergyCarrierUnitOfMeasure_name, ClusterReported);
                }
            }
            public class EnergyCarrierUnitOfMeasure_SupportCommands
            {
                public static implicit operator string(EnergyCarrierUnitOfMeasure_SupportCommands command)
                {
                    return SumSupCMD(MeteringCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_EnergyCarrierSummationFormatting
        {
            public const string EnergyCarrierSummationFormatting_name = "EnergyCarrierSummationFormatting";
            public class EnergyCarrierSummationFormatting_Attributes
            {
                public static implicit operator string(EnergyCarrierSummationFormatting_Attributes command)
                {
                    return SumAllAtt(MeteringCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_EnergyCarrierSummationFormatting command)
            {
                return SumATTStr(MeteringCluster.ClusterName, MeteringCluster.ATT_EnergyCarrierSummationFormatting.EnergyCarrierSummationFormatting_name);
            }
            public class EnergyCarrierSummationFormatting_Desired
            {
                public static implicit operator string(EnergyCarrierSummationFormatting_Desired command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_EnergyCarrierSummationFormatting.EnergyCarrierSummationFormatting_name, ClusterDesired);
                }
            }
            public class EnergyCarrierSummationFormatting_Report
            {
                public static implicit operator string(EnergyCarrierSummationFormatting_Report command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_EnergyCarrierSummationFormatting.EnergyCarrierSummationFormatting_name, ClusterReported);
                }
            }
            public class EnergyCarrierSummationFormatting_SupportCommands
            {
                public static implicit operator string(EnergyCarrierSummationFormatting_SupportCommands command)
                {
                    return SumSupCMD(MeteringCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_EnergyCarrierDemandFormatting
        {
            public const string EnergyCarrierDemandFormatting_name = "EnergyCarrierDemandFormatting";
            public class EnergyCarrierDemandFormatting_Attributes
            {
                public static implicit operator string(EnergyCarrierDemandFormatting_Attributes command)
                {
                    return SumAllAtt(MeteringCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_EnergyCarrierDemandFormatting command)
            {
                return SumATTStr(MeteringCluster.ClusterName, MeteringCluster.ATT_EnergyCarrierDemandFormatting.EnergyCarrierDemandFormatting_name);
            }
            public class EnergyCarrierDemandFormatting_Desired
            {
                public static implicit operator string(EnergyCarrierDemandFormatting_Desired command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_EnergyCarrierDemandFormatting.EnergyCarrierDemandFormatting_name, ClusterDesired);
                }
            }
            public class EnergyCarrierDemandFormatting_Report
            {
                public static implicit operator string(EnergyCarrierDemandFormatting_Report command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_EnergyCarrierDemandFormatting.EnergyCarrierDemandFormatting_name, ClusterReported);
                }
            }
            public class EnergyCarrierDemandFormatting_SupportCommands
            {
                public static implicit operator string(EnergyCarrierDemandFormatting_SupportCommands command)
                {
                    return SumSupCMD(MeteringCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_TemperatureUnitOfMeasure
        {
            public const string TemperatureUnitOfMeasure_name = "TemperatureUnitOfMeasure";
            public class TemperatureUnitOfMeasure_Attributes
            {
                public static implicit operator string(TemperatureUnitOfMeasure_Attributes command)
                {
                    return SumAllAtt(MeteringCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_TemperatureUnitOfMeasure command)
            {
                return SumATTStr(MeteringCluster.ClusterName, MeteringCluster.ATT_TemperatureUnitOfMeasure.TemperatureUnitOfMeasure_name);
            }
            public class TemperatureUnitOfMeasure_Desired
            {
                public static implicit operator string(TemperatureUnitOfMeasure_Desired command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_TemperatureUnitOfMeasure.TemperatureUnitOfMeasure_name, ClusterDesired);
                }
            }
            public class TemperatureUnitOfMeasure_Report
            {
                public static implicit operator string(TemperatureUnitOfMeasure_Report command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_TemperatureUnitOfMeasure.TemperatureUnitOfMeasure_name, ClusterReported);
                }
            }
            public class TemperatureUnitOfMeasure_SupportedCommands
            {
                public static implicit operator string(TemperatureUnitOfMeasure_SupportedCommands command)
                {
                    return SumSupCMD(MeteringCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/
        public class ATT_TemperatureFormatting
        {
            public const string TemperatureFormatting_name = "TemperatureFormatting";
            public class TemperatureFormatting_Attributes
            {
                public static implicit operator string(TemperatureFormatting_Attributes command)
                {
                    return SumAllAtt(MeteringCluster.ClusterName, ClusterAttributes);
                }
            }
            public static implicit operator string(ATT_TemperatureFormatting command)
            {
                return SumATTStr(MeteringCluster.ClusterName, MeteringCluster.ATT_TemperatureFormatting.TemperatureFormatting_name);
            }
            public class TemperatureFormatting_Desired
            {
                public static implicit operator string(TemperatureFormatting_Desired command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_TemperatureFormatting.TemperatureFormatting_name, ClusterDesired);
                }
            }
            public class TemperatureFormatting_Report
            {
                public static implicit operator string(TemperatureFormatting_Report command)
                {
                    return SumATTStr_WithSubValue(MeteringCluster.ClusterName, MeteringCluster.ATT_TemperatureFormatting.TemperatureFormatting_name, ClusterReported);
                }
            }
            public class TemperatureFormatting_SupportedCommands
            {
                public static implicit operator string(TemperatureFormatting_SupportedCommands command)
                {
                    return SumSupCMD(MeteringCluster.ClusterName, ClusterSupportedCommands);
                }
            }
        }/*End of ATT class*/


        #endregion

        #region Cluster Commands

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

        public static implicit operator MeteringCluster(string options)
        {
            return JsonConvert.DeserializeObject<MeteringCluster>(options);
        }
        public static implicit operator string(MeteringCluster command)
        {
            return MeteringCluster.ClusterName;
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
