using Core.Dto;

namespace Core;

public interface IMetadataProvider
{
    /// <summary>
    /// Fetches metadata for a specific source and returns a populated Song object.
    /// </summary>
    Task<Song> GetMetadataAsync(string identifier);
    
    /// <summary>
    /// Used to determine which provider to use in the PlaybackController.
    /// </summary>
    SongSource SupportedSource { get; }
}