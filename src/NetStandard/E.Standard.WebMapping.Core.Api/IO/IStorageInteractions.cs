using E.Standard.WebMapping.Core.Api.Bridge;

namespace E.Standard.WebMapping.Core.Api.IO;

public interface IStorageInteractions
{
    string StoragePathFormatParameter(IBridge bridge, int index);
}
