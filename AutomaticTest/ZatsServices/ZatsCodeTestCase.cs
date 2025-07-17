// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
// SPDX-FileCopyrightText: 2025 Z-Wave Alliance https://z-wavealliance.org
using System;
using System.Threading;
using ZWave.Exceptions;
using System.Collections.Generic;
using ZWave.Xml.Application;
using Utils;
using System.Linq;

namespace Zats.Services
{
    public abstract partial class ZatsCodeTestCase
    {
        public TransmitOptions TX_OPTIONS = TransmitOptions.Ack | TransmitOptions.AutoRoute | TransmitOptions.Explore;
        public TransmitSecurityOptions TX_SEC_OPTIONS = TransmitSecurityOptions.S2_TXOPTION_VERIFY_DELIVERY;

        public ProfileContext Context { get; set; }
        protected TestCaseResult Result { get; set; }
        private List<string> _failures = new List<string>();
        public IReportService ReportService { get; set; }
        public IZnifferService ZnifferService { get; set; }
        public ProgrammerService ProgrammerService { get; set; }
        public IService Service { get; set; }
        public IDebugService DebugService { get; set; }
        public IAsyncService AsyncService { get; set; }
        public IInstantAsyncService InstantAsyncService { get; set; }
        public IParallelService ParallelService { get; set; }
        public IComplianceService ComplianceService { get; set; }
        public IZipService ZipService { get; set; }
        public IUicSerice UicService { get; set; }
        public IUicAsyncService UicAsyncService { get; set; }
        public IUicDeviceAsyncService UicDeviceAsyncService { get; set; }
        public IZipAsyncService ZipAsyncService { get; set; }
        public IApplicationService ApplicationService { get; set; }
        public IZipApplicationService ZipApplicationService { get; set; }
        public ILibzwaveipService LibzwaveipService { get; set; }
        public ITestInterfaceService TestInterfaceService { get; set; }
        public IZipVerificationService ZipVerificationService { get; set; }
        public ITerminalService TerminalService { get; set; }
        public RailService RailService { get; set; }
        public InstrumentsService InstrumentsService { get; set; }
        public MeshRouterService NetworkRouterService { get; set; }

        /// <summary>
        /// Pause TC execution for specified time, miliseconds
        /// </summary>
        /// <param name="timeoutMs"></param>
        public void Delay(int timeoutMs)
        {
            Delay(timeoutMs, null);
        }

        /// <summary>
        /// Returns test procedure from common test library
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected T GetProcedure<T>(string name)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Pause TC execution for specified time, miliseconds
        /// </summary>
        /// <param name="timeoutMs"></param>
        public void Delay(int timeoutMs, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                ReportService.LogInfo($"Delay for {timeoutMs} ms");
            }
            else
            {
                ReportService.LogInfo($"Delay for {timeoutMs} ms - {text}");
            }

            Thread.Sleep(timeoutMs);
        }

        protected virtual void InitializeTest()
        {

        }

        protected virtual void FinalizeTest()
        {

        }

        public void Initialize()
        {
            InitializeTest();
        }

        private bool _isTerminationInProgress = false;
        protected bool IsTerminationInProgress()
        {
            return _isTerminationInProgress;
        }

        const string TERMINATION_ALREADY_IN_PROGRESS = "Termination is already in progress!";

        public TestCaseResult Execute()
        {
            try
            {
                ExecuteTest();
            }
            catch (AggregateException aex)
            {
                foreach (var item in aex.InnerExceptions)
                {
                    if (item is ServiceException svex)
                    {
                        ReportExceptionAndTerminateAsFailed(svex);
                    }
                    else if (item is OperationException oex)
                    {
                        ReportExceptionAndTerminateAsFailed(oex);
                    }
                    else 
                    {
                        ReportExceptionAndTerminateAsFailed(item);
                    }
                }
            }
            catch (TerminationException tex) // Terminated.
            {
                _isTerminationInProgress = true;

                Result = tex.Result;
                switch (Result)
                {
                    case TestCaseResult.Pass:
                        if (!tex.IsHidden)
                        {
                            ReportService.LogInfo(tex.Message);
                        }
                        break;
                    case TestCaseResult.Fail:
                        AddFailure(tex.Message);
                        if (!tex.IsHidden)
                        {
                            ReportService.LogError(tex.Message);
                        }
                        break;
                    case TestCaseResult.Skip:
                        AddFailure(tex.Message);
                        if (!tex.IsHidden)
                        {
                            ReportService.LogWarning(tex.Message);
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (AssertionException aex) // Assertion failed.
            {
                if (!aex.IsHidden)
                    ReportService.LogError(aex.Message);
                AddFailure(aex.Message);
                Result = TestCaseResult.Fail;
            }
            catch (WatchException wex) // Missing expected response command or received not expected response command.
            {
                ReportExceptionAndTerminateAsFailed(wex);
            }
            catch (OperationException oex) // Operation returns Ret=0; complete status != OK; operation is not supported.
            {
                ReportExceptionAndTerminateAsFailed(oex);
            }
            catch (RequestTimeoutException rtex) // Waiting for response too long.
            {
                ReportExceptionAndTerminateAsFailed(rtex);
            }
            catch (ServiceException svex) // Service execution exception.
            {
                ReportExceptionAndTerminateAsFailed(svex);
            }
            catch (ThreadAbortException taex)
            {
                var exMessage = "Test Case aborted due to specified timeout exceeded";
                ReportService.LogError(exMessage);
                AddFailure(exMessage);
                Result = TestCaseResult.Skip;
                $"{taex.Message}"._DLOG();
            }
            catch (ThreadInterruptedException tiex)
            {
                var exMessage = "Test Case aborted due to specified timeout exceeded";
                ReportService.LogError(exMessage);
                AddFailure(exMessage);
                Result = TestCaseResult.Skip;
                $"{tiex.Message}"._DLOG();
            }
            catch (Exception ex)
            {
                ReportService.LogError(ex.Message);
                ReportService.LogError(ex.StackTrace);
                AddFailure(ex.Message);
                Result = TestCaseResult.Fail;
            }
            finally
            {
                FinalizeTest();
            }
            Thread.Sleep(1000); // Finalize SmartStart.
            return Result;
        }

        private void ReportExceptionAndTerminateAsFailed(Exception ex)
        {
            ReportService.LogError(ex.Message);
            AddFailure(ex.Message);
            Result = TestCaseResult.Fail;
        }

        public void TryParseCommandClassRef(IList<byte> values, out byte[] commandClasses, out byte[] secureCommandClasses)
        {
            commandClasses = null;
            secureCommandClasses = null;
            ZWaveDefinition.TryParseCommandClassRef(values, out commandClasses, out secureCommandClasses);
        }

        public void ResetServiceSettings()
        {
            ZnifferService.Settings.Default();
            Service.Settings.Default();
            DebugService.Settings.Default();
            ZipService.Settings.Default();
            TestInterfaceService.Settings.Default();
        }

        protected abstract void ExecuteTest();

        public void AssertIsTrue(bool bl)
        {
            if (!bl)
                AssertionException.Throw();
        }

        public void AssertIsTrue(bool bl, string message)
        {
            if (!bl)
                AssertionException.Throw(message);
            if (!string.IsNullOrEmpty(message))
            {
                ReportService.LogOk(message);
            }
        }

        public void AssertIsTrue(bool bl, string passMessage, string failMessage)
        {
            if (!bl)
                AssertionException.Throw(failMessage);
            if (!string.IsNullOrEmpty(passMessage))
            {
                ReportService.LogOk(passMessage);
            }
        }

        public void Inconclusive(bool expression, string passMessage, string failMessage)
        {
            if (!expression)
            {
                AddFailure(failMessage);
                Result = TestCaseResult.Fail;
                ReportService.LogWarning(failMessage);
            }
            else if (!string.IsNullOrEmpty(passMessage))
            {
                ReportService.LogOk(passMessage);
            }
        }

        public string GetLastFailure()
        {
            string ret = null;
            if (_failures != null && _failures.Count > 0)
            {
                ret = _failures.Last();

            }
            return ret;
        }

        public void AddFailure(string failMessage)
        {
            if (_failures != null)
            {
                _failures.Add(failMessage);
            }
        }

        public void Inconclusive(bool expression)
        {
            if (!expression)
            {
                Result = TestCaseResult.Fail;
                ReportService.LogWarning("Inconclusive");
            }
        }

        public void Inconclusive(bool expression, string message)
        {
            if (!expression)
            {
                AddFailure(message);
                Result = TestCaseResult.Fail;
                ReportService.LogWarning(message);
            }
            else
            {
                ReportService.LogOk(message);
            }
        }

        public void Inconclusive(string failMessage)
        {
            AddFailure(failMessage);
            Result = TestCaseResult.Fail;
            ReportService.LogWarning(failMessage);
        }

        /// <summary>
        /// Raises a warning in case the result from the boolean expression is true
        /// </summary>
        /// <param name="controllerNumber">expression</param>
        /// <param name="nodeNumber">message</param>
        public void Warning(bool expression, string message)
        {
            if (expression)
            {
                AddFailure(message);
                Result = TestCaseResult.Fail;
                ReportService.LogWarning(message);
            }
        }

        public virtual void Fail(string message)
        {
            if (IsTerminationInProgress())
            {
                ReportService.LogWarning("- " + TERMINATION_ALREADY_IN_PROGRESS);
                return;
            }
            TerminationException.Throw(TestCaseResult.Fail, message);
        }

        public virtual void Pass(string message)
        {
            if (IsTerminationInProgress())
            {
                ReportService.LogWarning("- " + TERMINATION_ALREADY_IN_PROGRESS);
                return;
            }
            TerminationException.Throw(TestCaseResult.Pass, message);
        }

        public virtual void Skip(string message)
        {
            if (IsTerminationInProgress())
            {
                ReportService.LogWarning("- " + TERMINATION_ALREADY_IN_PROGRESS);
                return;
            }
            TerminationException.Throw(TestCaseResult.Skip, message);
        }

        public string FreqInText(string F)
        {
            string argFreq = "NON_REGION";
            switch (F)
            {
                case "EU_LR":
                    argFreq = "REGION_EU_LR";
                    break;
                case "US_LR":
                    argFreq = "REGION_US_LR";
                    break;
                case "US":
                    argFreq = "REGION_US";
                    break;
                case "KR":
                    argFreq = "REGION_KR";
                    break;
                case "IN":
                    argFreq = "REGION_IN";
                    break;
                case "CN":
                    argFreq = "REGION_CN";
                    break;
                case "ANZ":
                    argFreq = "REGION_ANZ";
                    break;
                case "EU":
                    argFreq = "REGION_EU";
                    break;
                case "HK":
                    argFreq = "REGION_HK";
                    break;
                case "IL":
                    argFreq = "REGION_IL";
                    break;
                case "JP":
                    argFreq = "REGION_JP";
                    break;
                case "RU":
                    argFreq = "REGION_RU";
                    break;
                default:
                    break;
            }
            return argFreq;
        }
    }

    public struct ByteData
    {
        public static ByteData AnyValue
        {
            get
            {
                ByteData bi = new ByteData(0, 0, Presence.AnyValue);
                return bi;
            }
        }

        private readonly Presence _presence;
        public Presence Presence
        {
            get { return _presence; }
        }

        private readonly byte _maskInData;
        public byte MaskInData
        {
            get { return _maskInData; }
        }

        private readonly byte _value;
        public byte Value
        {
            get { return _value; }
        }

        public ByteData(byte value)
            : this(value, 0xFF, Presence.Value)
        {
        }

        public ByteData(byte value, byte mask)
            : this(value, mask, Presence.Value)
        {
        }

        public ByteData(byte value, byte mask, Presence presence)
        {
            _value = value;
            _maskInData = mask;
            _presence = presence;
        }

        public override int GetHashCode()
        {
            return _value ^ _maskInData ^ (int)_presence;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ByteData))
                return false;

            return Equals((ByteData)obj);
        }

        public bool Equals(ByteData other)
        {
            return _value == other._value && _maskInData == other._maskInData && _presence == other._presence;
        }

        public static bool operator ==(ByteData index1, ByteData index2)
        {
            return index1.Equals(index2);
        }

        public static bool operator !=(ByteData index1, ByteData index2)
        {
            return !index1.Equals(index2);
        }

        public static implicit operator ByteData(byte value)
        {
            return new ByteData(value);
        }

        public override string ToString()
        {
            string ret = "*_";
            switch (Presence)
            {
                case Presence.Value:
                    if (MaskInData == 0xFF)
                        ret = Tools.FormatStr("{0:X2}", Value);
                    else
                        ret = Tools.FormatStr("{0:X2}&{1:X2}", Value, MaskInData);
                    break;
                case Presence.ExceptValue:
                    if (MaskInData == 0xFF)
                        ret = Tools.FormatStr("!{0:X2}", Value);
                    else
                        ret = Tools.FormatStr("!{0:X2}&{1:X2}", Value, MaskInData);
                    break;
                case Presence.AnyValue:
                    ret = "__";
                    break;
            }
            return ret;
        }
    }
}
