using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Core.BlobStorageClient.Interfaces;
using Core.BlobStorageClient.Models;
using Microsoft.Extensions.Options;

namespace Core.BlobStorageClient;

public class BlobStorageClient: IBlobStorageClient
{
    private readonly BlobStorageClientOptions _options;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly Uri _blobStorageUri;
    
    public BlobStorageClient(IOptions<BlobStorageClientOptions> options)
    {
        _options = options.Value;
        _blobServiceClient = new BlobServiceClient(_options.AzureBlobStorageConnectionString);
        _blobStorageUri = _blobServiceClient.Uri;
    }
    
    public async Task<IEnumerable<FileUploaded>> UploadFilesAsync(IEnumerable<BlobFileUpload> blobFiles, string containerName)
    {
        ValidateAllowedExtensions(blobFiles);
        CheckContainerName(ref blobFiles, ref containerName);
        var blobContainer = _blobServiceClient.GetBlobContainerClient(containerName);
        await blobContainer.CreateIfNotExistsAsync(PublicAccessType.Blob);
        var containerUrl = blobContainer.Uri.ToString();
        
        var uploadJobs = new ActionBlock<BlobFileUpload>(async blobFile =>
        {
            var blobClient = blobContainer.GetBlobClient(blobFile.FileName);
            var blobUploadOptions = SetBlobHeaderCacheControl(blobFile.MaxAge, blobFile.FileName);
            await blobClient.UploadAsync(blobFile.Stream, blobUploadOptions);
        }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _options.MaximumConcurrency });
        
        blobFiles.AsParallel().ForAll(blobFile => uploadJobs.Post(blobFile));
        
        uploadJobs.Complete();
        await uploadJobs.Completion;

        await Parallel.ForEachAsync(blobFiles, async (file, _) => await file.Stream.DisposeAsync());
        
        var filesUploaded = blobFiles.Select(file => new FileUploaded
        {
            Order = file.Order,
            FileName = file.FileName,
            Url = containerUrl + "/" + file.FileName
        });

        return !string.IsNullOrWhiteSpace(_options.ReplaceBlobHostTo) ? ReplaceUriHost(filesUploaded) : filesUploaded;
    }

    public async Task<IEnumerable<BlobFile>> GetIfExistsAsStreamAsync(IEnumerable<Uri> uris)
    {
        var blobFiles = new ConcurrentBag<BlobFile>();
        var getJobs = new ActionBlock<Uri>(async uri =>
        {
            var (containerName, fileName) = GetFileAndContainerNameFromUri(uri);
            var blobContainer = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = blobContainer.GetBlobClient(fileName);

            var blobStream = new MemoryStream();
            if (await blobClient.ExistsAsync())
            {
                await blobClient.DownloadToAsync(blobStream);
                blobStream.Position = 0;
            }
            
            blobFiles.Add(new BlobFile
            {
                FileName = fileName,
                Stream = blobStream.Length > 0 ? blobStream : Stream.Null
            });
        }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _options.MaximumConcurrency });
        
        uris.AsParallel().ForAll(uri => getJobs.Post(uri));
        getJobs.Complete();
        await getJobs.Completion;
        
        return blobFiles;
    }

    public async Task<bool> DeleteFilesAsync(IEnumerable<Uri> uris)
    {
        if (!uris.Any())
            return false;
        
        var deleteJobs = new ActionBlock<Uri>(async uri =>
        {
            var (containerName, fileName) = GetFileAndContainerNameFromUri(uri);

            var blobContainer = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = blobContainer.GetBlobClient(fileName);

            await blobClient.DeleteIfExistsAsync();
        }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _options.MaximumConcurrency });
        
        uris.AsParallel().ForAll(uri => deleteJobs.Post(uri));
        deleteJobs.Complete();

        await deleteJobs.Completion;
        return true;
    }

    public Uri GetStorageUri()
    {
        return _blobStorageUri;
    }

    private static void CheckContainerName(ref IEnumerable<BlobFileUpload> filesToUpload, ref string containerName)
    {
        if (!containerName.Contains('/'))
            return;
        
        var splitContainerName = new Queue<string>(containerName.Split("/").Where(s => !string.IsNullOrWhiteSpace(s)));
        containerName = splitContainerName.Dequeue();

        filesToUpload = splitContainerName.Reverse().Aggregate(filesToUpload, (current, cName) =>
            current.Select(file => new BlobFileUpload {FileName = cName + "/" + file.FileName, MaxAge = file.MaxAge, Stream = file.Stream}));
    }

    private (string, string) GetFileAndContainerNameFromUri(Uri uri)
    {
        var absolutePath = uri.ToString().Replace(_blobStorageUri.ToString(), string.Empty);
        var splitPath = new Queue<string>(absolutePath.Split("/").Where(s => !string.IsNullOrWhiteSpace(s)));
            
        var containerName = splitPath.Dequeue();
        var fileName = string.Join("/", splitPath);

        return (containerName, fileName);
    }

    private static string GetFileMimeType(string fileName) => Path.GetExtension(fileName) switch
    {
        ImageExtension.Jpeg => "image/jpeg",
        ImageExtension.Jpg => "image/jpeg",
        ImageExtension.Png => "image/png",
        ImageExtension.Gif => "image/gif",
        _ => "application/octet-stream"
    };

    private static BlobUploadOptions SetBlobHeaderCacheControl(uint? maxAge, string fileName)
    {
        var blobOptions = new BlobUploadOptions
        {
            TransferOptions = new StorageTransferOptions { MaximumConcurrency = 2 },
            HttpHeaders = new BlobHttpHeaders()
            {
                CacheControl = maxAge is not null ? $"max-age={maxAge}" : null,
                ContentType = GetFileMimeType(fileName)
            }
        };

        return blobOptions;
    }

    private IEnumerable<FileUploaded> ReplaceUriHost(IEnumerable<FileUploaded> filesUploaded) => filesUploaded.Select(file =>
    {
        var uriBuilder = new UriBuilder(file.Url)
        {
            Host = _options.ReplaceBlobHostTo
        };

        file.Url = uriBuilder.Uri.ToString();

        return file;
    });

    private void ValidateAllowedExtensions(IEnumerable<BlobFileUpload> blobFiles)
    {
        if (!_options.AllowedExtensions?.Any() ?? true)
            return;

        var exceptions = new ConcurrentBag<InvalidOperationException>();

        blobFiles.AsParallel().ForAll(blobFile =>
        {
            if(!_options.AllowedExtensions.Contains(Path.GetExtension(blobFile.FileName)))
                exceptions.Add(new InvalidOperationException($"The file {blobFile.FileName} extension is not allowed. You can upload files with the following extensions: {string.Join(",", _options.AllowedExtensions)}"));
        });

        if (exceptions.Any())
            throw new AggregateException("No files were uploaded.", exceptions);
    }
}