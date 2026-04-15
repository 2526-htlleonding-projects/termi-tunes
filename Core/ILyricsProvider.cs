using Core.Dto;

namespace Core;

public interface ILyricsProvider
{
    public Task GetLyrics(Song song);
}
