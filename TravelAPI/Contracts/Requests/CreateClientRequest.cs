namespace TravelAPI.Contracts.Requests;
using System.ComponentModel.DataAnnotations;

public class CreateClientRequest
{
    [Required]
    [StringLength(120)]
    public required string FirstName { get; set; } = String.Empty;
    
    [Required]
    [StringLength(120)]
    public required string LastName { get; set; } = String.Empty;
    
    [Required]
    [StringLength(120)]
    public required string Email { get; set; } = String.Empty;
    
    [Required]
    [StringLength(120)]
    public required string Telephone { get; set; } = String.Empty;
    
    [Required]
    [StringLength(120)]
    public required string Pesel { get; set; } = String.Empty;
}