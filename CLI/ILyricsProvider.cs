using Core.Dto;

namespace CLI;

public interface ILyricsProvider
{
    public Task GetLyrics(Song song);
}