using Microsoft.Data.SqlClient;
using TravelAPI.Exceptions;
using TravelAPI.Services.Abstractions;

namespace TravelAPI.Services;
using TravelAPI.Models;

public class TripService : ITripService
{
    private readonly string _connectionString;
    private readonly IClientService _clientService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public TripService(IConfiguration cfg, IClientService clientService, IDateTimeProvider dateTimeProvider)
    {
        _connectionString = cfg.GetConnectionString("Default") ??
                            throw new ArgumentNullException(nameof(cfg), "No Default connection string was specified.");
        _clientService = clientService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async ValueTask<bool> UpdateClientTripAsync(int clientId, int tripId, CancellationToken cancellationToken)
    {
        await ValidateClientExistsAsync(clientId, cancellationToken);
        await ValidateTripExistsAsync(tripId, cancellationToken);
        await ValidateNoSuchClientTripExistsAsync(clientId, tripId, cancellationToken);

        const string peopleRegisteredQuery = """
                                                SELECT COUNT(*) FROM Client_Trip WHERE Client_Trip.IdTrip = @tripId 
                                             """;

        const string maxPeopleQuery = """
                                        SELECT MaxPeople FROM Trip WHERE Trip.IdTrip = @tripId
                                      """;
        
        const string addClientTripQuery = """
                                          INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt, PaymentDate)
                                          VALUES (@idClient, @idTrip, @registeredAt, @paymentDate)
                                          """;

        await using (SqlConnection connection = new(_connectionString))
        {
            await using (SqlCommand peopleRegisteredCommand = new SqlCommand(peopleRegisteredQuery, connection))
            {
                await using (SqlCommand maxPeopleCommand = new SqlCommand(maxPeopleQuery, connection))
                {
                    await connection.OpenAsync(cancellationToken);
                    peopleRegisteredCommand.Parameters.AddWithValue("@tripId", tripId);
                    maxPeopleCommand.Parameters.AddWithValue("@tripId", tripId);
                    
                    var currentNumber = Convert.ToInt32(await peopleRegisteredCommand.ExecuteScalarAsync(cancellationToken));
                    var maxNumber = Convert.ToInt32(await maxPeopleCommand.ExecuteScalarAsync(cancellationToken));
                    if (!(currentNumber < maxNumber))
                    {
                        throw new NoSpotsLeftException(tripId);
                    }
                    
                    await using var addClientTripCommand = new SqlCommand(addClientTripQuery, connection);
                    var formattedDate = int.Parse(_dateTimeProvider.UtcNow.ToString("yyyyMMdd"));
                    addClientTripCommand.Parameters.AddWithValue("@idClient", clientId);
                    addClientTripCommand.Parameters.AddWithValue("@idTrip", tripId);
                    addClientTripCommand.Parameters.AddWithValue("@registeredAt", formattedDate);
                    addClientTripCommand.Parameters.AddWithValue("@paymentDate", DBNull.Value);
                    
                    var rowsAffected = await addClientTripCommand.ExecuteNonQueryAsync(cancellationToken);
                    return rowsAffected > 0;
                }
            }
        }
    }

    public async Task<bool> DeleteClientRegistration(int clientId, int tripId, CancellationToken cancellationToken)
    {
        const string query = """
                                DELETE FROM Client_Trip WHERE IdClient = @clientId AND IdTrip = @tripId;
                             """;
        using (SqlConnection connection = new SqlConnection(_connectionString))
        using (var command = new SqlCommand(query, connection))
        {
            await connection.OpenAsync(cancellationToken);
            command.Parameters.AddWithValue("@clientId", clientId);
            command.Parameters.AddWithValue("@tripId", tripId);
            
            int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
            return rowsAffected > 0;
        }
    }
    
    public async Task<ICollection<Trip>> GetAllTripsAsync(CancellationToken cancellationToken)
    {
        var trips = new List<Trip>();
        const string query = """
                             SELECT
                                IdTrip,
                                Name,
                                Description,
                                DateFrom,
                                DateTo,
                                MaxPeople
                             FROM Trip
                             """;
        await using (SqlConnection connection = new(_connectionString))
        {
            await using (SqlCommand command = new(query, connection))
            {
                await connection.OpenAsync(cancellationToken);
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var trip = new Trip
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Description = reader.GetString(2),
                        DateFrom = reader.GetDateTime(3),
                        DateTo = reader.GetDateTime(4),
                        MaxPeople = reader.GetInt32(5)
                    };
                    trips.Add(trip);
                }
            }
        }

        return trips;
    }

    private async Task ValidateTripExistsAsync(int id, CancellationToken cancellationToken)
    {
        var exists = await TripExistsByIdAsync(id, cancellationToken);
        if (!exists) throw new NoSuchTripException(id);
    }

    public async ValueTask<bool> TripExistsByIdAsync(int id, CancellationToken cancellationToken)
    {
        const string query = """"
                             SELECT 
                                 IIF(EXISTS (SELECT 1 FROM Trip 
                                         WHERE Trip.IdTrip = @tripId), 1, 0) AS TripExists;
                             """";
        await using (var connection = new SqlConnection(_connectionString))
        {
            await using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@tripId", id);
                await connection.OpenAsync(cancellationToken);

                var result = await command.ExecuteScalarAsync(cancellationToken);
                return Convert.ToInt32(result) == 1;
            }
        }
    }

    private async Task ValidateClientExistsAsync(int id, CancellationToken cancellationToken)
    {
        await _clientService.ClientExistsByIdAsync(id, cancellationToken);
    }

    public async Task ValidateNoSuchClientTripExistsAsync(int clientId, int tripId, CancellationToken cancellationToken)
    {
        const string query = """
                                SELECT 
                             IIF(EXISTS (SELECT 1 FROM Client_Trip 
                                     WHERE Client_Trip.IdTrip = @tripId AND Client_Trip.IdClient = @clientId), 1, 0) AS Client_TripExists;
                             """;
        using (SqlConnection connection = new(_connectionString))
        using (SqlCommand command = new SqlCommand(query, connection))
        {
            await connection.OpenAsync(cancellationToken);
            command.Parameters.AddWithValue("@tripId", tripId);
            command.Parameters.AddWithValue("@clientId", clientId);
            var exists = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            if (exists == 1) throw new ClientIsAlreadyRegisteredForTrip(clientId, tripId);
        }
    }
}