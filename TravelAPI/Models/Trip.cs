namespace TravelAPI.Models;

public class Trip : BaseModel
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int MaxPeople { get; set; }
}