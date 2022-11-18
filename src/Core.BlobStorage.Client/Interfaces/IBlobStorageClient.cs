using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.BlobStorage.Client.Models;

namespace Core.BlobStorage.Client.Interfaces;

/// <summary>
/// Blob Storage Client
/// </summary>
public interface IBlobStorageClient
{
    /// <summary>
    /// Upload files to the blob storage.
    /// </summary>
    /// <param name="blobFiles"></param>
    /// <param name="containerName">Nome do container</param>
    /// <returns></returns>
    Task<IEnumerable<FileUploaded>> UploadFilesAsync(IEnumerable<BlobFileUpload> blobFiles, string containerName);
    
    /// <summary>
    /// Delete blob files if they exist.
    /// </summary>
    /// <param name="uris"></param>
    /// <returns></returns>
    Task<bool> DeleteFilesAsync(IEnumerable<Uri> uris);
    
    /// <summary>
    /// Retrieves the Storage URI.
    /// </summary>
    /// <returns></returns>
    Uri GetStorageUri();

    /// <summary>
    /// Gets blob files if they exist, otherwise, returns Stream.Null on the BlobFile Stream attribute.
    /// </summary>
    /// <param name="uris"></param>
    /// <returns></returns>
    Task<IEnumerable<BlobFile>> GetIfExistsAsStreamAsync(IEnumerable<Uri> uris);
}