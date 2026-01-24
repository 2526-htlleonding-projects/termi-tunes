namespace Core.Dto;

public class Artist
{
    public string Id { get; }
    public string Name { get; }
    public List<Song> Populars { get; }
    
    public Artist(string id, string name, List<Song> populars)
    {
        Id = id;
        Name = name;
        Populars = populars;
    }
}