namespace TravelAPI.Models;

public class ClientTrip
{
    public int ClientId { get; set; }
    public int TripId { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int MaxPeople { get; set; }
    public List<Country> Countries { get; set; }
    public int RegisteredAt { get; set; }
    public int PaymentDate { get; set; }
}