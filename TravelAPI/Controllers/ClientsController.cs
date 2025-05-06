using Microsoft.AspNetCore.Mvc;
using TravelAPI.Contracts.Requests;
using TravelAPI.Models;
using TravelAPI.Services.Abstractions;
using TravelAPI.Exceptions;

namespace TravelAPI.Controllers;

[ApiController]
[Route("api/clients")]
public class ClientsController : ControllerBase
{
    private IClientService _clientService;
    private ITripService _tripService;

    public ClientsController(IClientService clientService, ITripService tripService)
    {
        _clientService = clientService;
        _tripService = tripService;
    }

    [HttpGet("{clientId:int}/trips")]
    [ProducesResponseType(typeof(ICollection<ClientTrip>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllClientTrips([FromRoute] int clientId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _clientService.GetAllClientTripsAsync(clientId, cancellationToken);
            return Ok(result);
        }
        catch (NoSuchClientException)
        {
            return NotFound($"Client with provided {nameof(clientId)} is not found");
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateClientAsync([FromBody] CreateClientRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var clientId = await _clientService.CreateClientAsync(request, cancellationToken);

            return CreatedAtAction(nameof(GetAllClientTrips), new { clientId }, new { clientId });
        }
        catch (ClientAlreadyExistsException)
        {
            return BadRequest("Client with provided PESEL already exists");
        }
    }

    [HttpPut("{clientId:int}/trips/{tripId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateClientTripAsync(
        [FromRoute] int clientId,
        [FromRoute] int tripId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var result = await _tripService.UpdateClientTripAsync(tripId, clientId, cancellationToken);
            if (!result)
            {
                return CreateProblemResult(
                    StatusCodes.Status500InternalServerError,
                    "Failed to update client trip");
            }

            return Ok();
        }
        catch (NoSuchTripException)
        {
            return NotFound($"Trip with id: {tripId} does not exist");
        }
        catch (NoSuchClientException)
        {
            return NotFound($"Client with id: {clientId} does not exist");
        }
    }

    private ObjectResult CreateProblemResult(int statusCode, string detail)
    {
        return new ObjectResult(new ProblemDetails
        {
            Status = statusCode,
            Detail = detail
        })
        {
            StatusCode = statusCode
        };
    }
}