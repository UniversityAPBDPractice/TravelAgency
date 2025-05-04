using TravelAPI.Contracts.Requests;
using TravelAPI.Models;

namespace TravelAPI.Services.Abstractions;

public interface IClientService
{
    public Task<ICollection<ClientTrip>> GetAllClientTripsAsync(int clientId, CancellationToken token = default);
    public Task<int> CreateClientAsync(CreateClientRequest client, CancellationToken token = default);
    public ValueTask<bool> ClientExistsByPeselAsync(string pesel, CancellationToken token = default);
    public ValueTask<bool> ClientExistsByIdAsync(int clientId, CancellationToken token = default);
}