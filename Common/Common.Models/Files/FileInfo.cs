using Frever.Protobuf;

namespace Common.Models.Files;

public class FileInfo
{
    public FileSource Source { get; set; }

    public string Version { get; set; }

    public FileType File { get; set; }

    public FileExtension Extension { get; set; }

    public Resolution? Resolution { get; set; }

    public Platform? Platform { get; set; }

    [ProtoNewField(1)] public string UnityVersion { get; set; }

    [ProtoNewField(2)] public string AssetManagerVersion { get; set; }

    [ProtoNewField(3)] public string[] Tags { get; set; }

    public override string ToString()
    {
        return $"{File} {Extension.ToString()} {Resolution?.ToString()}";
    }

    public FileInfo Copy()
    {
        return (FileInfo) MemberwiseClone();
    }
}