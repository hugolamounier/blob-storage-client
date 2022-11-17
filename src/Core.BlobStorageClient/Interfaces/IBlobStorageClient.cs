using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.BlobStorageClient.Models;

namespace Core.BlobStorageClient.Interfaces;

/// <summary>
/// Cliente do Blob Storage
/// </summary>
public interface IBlobStorageClient
{
    /// <summary>
    /// Faz o upload de arquivos para o Blob Storage
    /// </summary>
    /// <param name="blobFiles"></param>
    /// <param name="containerName">Nome do container</param>
    /// <returns></returns>
    Task<IEnumerable<FileUploaded>> UploadFilesAsync(IEnumerable<BlobFileUpload> blobFiles, string containerName);
    
    /// <summary>
    /// Deleta arquivos do Blob se existirem
    /// </summary>
    /// <param name="uris"></param>
    /// <returns></returns>
    Task<bool> DeleteFilesAsync(IEnumerable<Uri> uris);
    
    /// <summary>
    /// Retorna o URI do Blob Storage
    /// </summary>
    /// <returns></returns>
    Uri GetStorageUri();

    /// <summary>
    /// Recuperar o número máximo de concorrência do cliente
    /// </summary>
    /// <returns></returns>
    int GetMaxConcurrency();
    
    /// <summary>
    /// Retorna arquivos do Blob Storage se existirem, caso contrário, retorna Stream.Null no Stream.
    /// </summary>
    /// <param name="uris"></param>
    /// <returns></returns>
    Task<IEnumerable<BlobFile>> GetIfExistsAsStreamAsync(IEnumerable<Uri> uris);
}