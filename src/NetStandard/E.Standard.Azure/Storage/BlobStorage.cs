using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System.Threading.Tasks;

namespace E.Standard.Azure.Storage;

public class BlobStorage
{
    private readonly string _connectionString;

    public BlobStorage(string connectionString)
    {
        _connectionString = connectionString;
    }

    async public Task<CloudBlobContainer> GetContainer(string containerName)
    {
        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_connectionString);

        // Create the blob client.
        CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

        // Retrieve a reference to a container. 
        CloudBlobContainer container = blobClient.GetContainerReference(containerName);

        // Create the container if it doesn't already exist.
        await container.CreateIfNotExistsAsync();

        return container;
    }
}
