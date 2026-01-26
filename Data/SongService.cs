using System.Data.SQLite;
using Core.Dto;
using Dapper;
using Data.Exceptions;

namespace Data;

public class SongService
{
    private readonly string _connectionString = "Data Source=music.db";

    public SongService()
    {
        if (InitializeAsync().GetAwaiter().GetResult()) throw new DatabaseNotInitializedException();
    }

    /// <summary>
    /// Returns a Record with a string SongId and a int Song Source
    /// </summary>
    /// <param name="nickname"></param>
    /// <returns></returns>
    public async Task<SongReference?> GetSongReferenceAsync(string? nickname)
    {
        await using var db = new SQLiteConnection(_connectionString);

        // Query to find the SongId and Source associated with the nickname
        return await db.QueryFirstOrDefaultAsync<SongReference>(
            "SELECT SongId, Source FROM Nicknames WHERE Nickname = @nickname",
            new { nickname });
    }

    /// <summary>
    /// Saves the nickname for a song, logic gets handled here as well.
    /// </summary>
    /// <param name="nickname"></param>
    /// <param name="song"></param>
    public async Task SaveNicknameAsync(string nickname, Song song)
    {
        await using var db = new SQLiteConnection(_connectionString);
        
        bool alreadyExists = GetSongReferenceAsync(nickname).Result is null;
        if(alreadyExists) throw new NicknameAlreadyExistsException(nickname);
        
        await db.ExecuteAsync(
            "INSERT OR REPLACE INTO Nicknames (Nickname, SongId, Source) VALUES (@nick, @id, @src)",
            new { nick = nickname, id = song.Id, src = (int)song.Source });
    }

    /// <summary>
    /// Runs at start, creates Table but preserves existing.
    /// </summary>
    public async Task<bool> InitializeAsync()
    {
        await using var db = new SQLiteConnection(_connectionString);

        //TODO add table for playlists
        await db.ExecuteAsync(@"
            CREATE IF NOT EXISTS TABLE Nicknames (
                Id String Primary Key,
                Nickname String,
                Source Integer 
            )");
    }
}

// -- Utils --

public record SongReference(string SongId, int Source);