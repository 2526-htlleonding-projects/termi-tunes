namespace Core.Dto;

public class Artist
{
    public string Id { get; }
    public string Name { get; }
    
    public Artist(string id, string name)
    {
        Id = id;
        Name = name;
    }
}