using Microsoft.AspNetCore.Mvc;
using TravelAPI.Models;
using TravelAPI.Services.Abstractions;

namespace TravelAPI.Controllers;

public class TripsController : ControllerBase
{
    private readonly ITripService _tripService;

    public TripsController(ITripService tripService)
    {
        _tripService = tripService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ICollection<Trip>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        var trips = await _tripService.GetAllTripsAsync(cancellationToken);
        return Ok(trips);
    }
}