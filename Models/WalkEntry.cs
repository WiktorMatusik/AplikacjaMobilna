using SQLite;

namespace AplikacjaMobilna.Models;

public class WalkEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public double DistanceKm { get; set; }
    public int DurationSeconds { get; set; }
}