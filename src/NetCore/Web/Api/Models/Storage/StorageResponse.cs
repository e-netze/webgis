namespace Api.Core.Models.Storage;

public class StorageResponse
{
    public StorageResponse(object storageResponse, bool isSuccessfull = true)
    {
        this.response = storageResponse;
        this.success = isSuccessfull;
    }

    public bool success { get; set; }
    public object response { get; set; }
}
