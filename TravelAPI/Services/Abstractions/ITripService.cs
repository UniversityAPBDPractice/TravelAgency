using TravelAPI.Models;

namespace TravelAPI.Services.Abstractions;

public interface ITripService
{
    public Task<ICollection<Trip>> GetAllTripsAsync(CancellationToken token = default);
    public ValueTask<bool> TripExistsByIdAsync(int tripId, CancellationToken token = default);
    public ValueTask<bool> UpdateClientTripAsync(int clientId, int tripId, CancellationToken token = default);
    public Task<bool> DeleteClientRegistration(int clientId, int tripId, CancellationToken token = default);
}