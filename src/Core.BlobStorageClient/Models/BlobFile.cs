using System.IO;

namespace Core.BlobStorageClient.Models;

public class BlobFile
{
    public string FileName { get; set; }
    public Stream Stream { get; set; }
}

public class BlobFileUpload: BlobFile
{
    public int? Order { get; set; }
    public uint? MaxAge { get; set; }
}

public class FileUploaded
{
    public int? Order { get; set; }
    public string FileName { get; set; }
    public string Url { get; set; }
}