using System.Data.SQLite;
using Core.Dto;
using Dapper;

namespace Data;

public class SongRegistry
{
    private readonly string _connectionString = "Data Source=music.db";

    public async Task<SongReference?> GetSongReferenceAsync(string? nickname)
    {
        await using var db = new SQLiteConnection(_connectionString);
        
        // Query to find the SongId and Source associated with the nickname
        return await db.QueryFirstOrDefaultAsync<SongReference>(
            "SELECT SongId, Source FROM Nicknames WHERE Nickname = @nickname", 
            new { nickname });
    }

    public async Task SaveNicknameAsync(string nickname, Song song)
    {
        await using var db = new SQLiteConnection(_connectionString);
        // If nickname exists, you'd handle the "call_me_blondie" logic here 
        // before executing the INSERT/UPDATE
        await db.ExecuteAsync(
            "INSERT OR REPLACE INTO Nicknames (Nickname, SongId, Source) VALUES (@nick, @id, @src)",
            new { nick = nickname, id = song.Id, src = (int)song.Source });
    }
}

public record SongReference(string SongId, int Source);