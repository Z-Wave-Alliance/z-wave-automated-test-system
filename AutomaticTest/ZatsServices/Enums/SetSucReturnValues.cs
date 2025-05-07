namespace Zats.Services
{
    public enum SetSucReturnValues : int
    {
        /// <summary>
        /// 
        /// </summary>
        SucUndefined = ZWave.BasicApplication.Enums.SetSucReturnValues.SucUndefined,
        /// <summary>
        /// ZWaveSUC_SET_SUCCEEDED
        /// </summary>
        SucSetSucceeded = ZWave.BasicApplication.Enums.SetSucReturnValues.SucSetSucceeded,
        /// <summary>
        /// ZWaveSUC_SET_FAILED
        /// </summary>
        SucSetFailed = ZWave.BasicApplication.Enums.SetSucReturnValues.SucSetFailed
    }
}
