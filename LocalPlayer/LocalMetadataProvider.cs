using Core;
using Core.Dto;

namespace LocalPlayer;

/// <summary>
/// Can get Metadata of a song by looking into the file.
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

        var title = tag.Title ?? "Unknown";
        var artist = tag.FirstPerformer ??  "Unknown";
        var album  = tag.Album  ?? "Unknown";
        var year = tag.Year;
        var track    = tag.Track;
        var genre  = tag.FirstGenre ?? "Unknown";
        var duration = file.Properties.Duration;
        var bitrate = file.Properties.AudioBitrate;

        List<Artist> artists = null;
        artists!.Add(new Artist(" ", artist));

        Task<Song> local = Task.Run(() =>
            SongFactory.CreateLocal(title, artist, duration, path, artists)
        );
        return local;
    }
}