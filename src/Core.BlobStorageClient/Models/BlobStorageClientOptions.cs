using System;

namespace Core.BlobStorageClient.Models;

/// <summary>
/// The options to be used by <see cref="BlobStorageClient" />.
/// </summary>
public abstract class BlobStorageClientOptions
{
    protected BlobStorageClientOptions(string azureBlobStorageConnectionString, int maximumConcurrency = 2, string? replaceBlobHostTo = null)
    {
        if (string.IsNullOrWhiteSpace(azureBlobStorageConnectionString))
            throw new ArgumentException("The value for this attribute cannot be null or empty", nameof(AzureBlobStorageConnectionString));

        AzureBlobStorageConnectionString = azureBlobStorageConnectionString;
        MaximumConcurrency = maximumConcurrency;
        ReplaceBlobHostTo = replaceBlobHostTo;
    }

    /// <summary>
    /// Connection string to the Azure Blob Storage
    /// </summary>
    public string AzureBlobStorageConnectionString { get; set; }

    /// <summary>
    /// Set the maximum concurrency used to download and upload files from the blob storage.
    /// </summary>
    public int MaximumConcurrency { get; set; }
    
    /// <summary>
    /// Optional. If set, it will replace the uploaded blob storage file URI Host to the one specified.
    /// </summary>
    public string? ReplaceBlobHostTo { get; set; }
    
}