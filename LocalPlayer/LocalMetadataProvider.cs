using Core;
using Core.Dto;

namespace LocalPlayer;

/// <summary>
/// Gets metadata of a local song file.
/// </summary>
public class LocalMetadataProvider : IMetadataProvider
{
    public SongSource SupportedSource { get; }

    public LocalMetadataProvider(SongSource supportedSource)
    {
        SupportedSource = supportedSource;
    }

    public Task<Song> GetMetadataAsync(string path)
    {
        var file = TagLib.File.Create(path);
        var tag = file.Tag;

        var title = tag.Title ?? Path.GetFileNameWithoutExtension(path);
        var artist = tag.FirstPerformer ?? "Unknown";
        var duration = file.Properties.Duration;

        List<Artist> artists = [new Artist("local", artist)];

        return Task.FromResult(SongFactory.CreateLocal(title, artist, duration, path, artists));
    }
}
