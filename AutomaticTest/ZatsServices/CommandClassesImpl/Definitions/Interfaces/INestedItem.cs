namespace ZatsServices_netcore.CommandClassesImpl.Definitions.Interfaces
{
    public interface INestedItem
    {
        // used for N+1 controls, nested child list management
        //
        // can be mapped to another instance property via getter ONLY if that property is unique between different INestedItem under the same INodeItem
        //  e.g.: - mapping to OperatingState in ThermostatOperatingStateSupport OperatingStateLog
        //        - mapping to Number in ConfigurationSupport ConfigurationParam
        //
        // for auto-increment behavior on an specific (numeric) instance property, GET & SET Id from / to that property
        //  e.g.: - mapping to GroupId in AssociationSupport AssociationGroupEx
        //        - mapping to ToneId in SoundSwitchSupport SoundSwitchTone
        //
        // if not mapped to another instance property, Id must be assigned explicitly during instance creation in specific N+1 implementation
        // set [JsonIgnore] only if mapped to another instance property, but NEVER if unmapped!
        int NestedId { get; set; }
    }
}
