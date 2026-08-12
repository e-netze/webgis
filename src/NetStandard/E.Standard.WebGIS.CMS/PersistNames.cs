namespace E.Standard.WebGIS.CMS;

//
// Never change const values here
// they are stored in the cms.xml files, a change will break running API instances!!!
// Also if there are typos!!! this are only internal constants stored in the cms.xml Files!!!
//
public class PersistNames
{
    public const string Name = "name";

    public class EditingCommitAction
    {
        public const string ActionTiming = "timing";
        public const string ActionProtocol = "protocol";
        public const string ActionTarget = "target";
        public const string ActionHeaders = "headers";
        public const string ActionPayload = "payload";
    }
}
