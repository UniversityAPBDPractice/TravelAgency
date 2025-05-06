using System.Collections;
using TravelAPI.Services.Abstractions;

namespace TravelAPI.Services;
using TravelAPI.Models;
using Microsoft.Data.SqlClient;
using TravelAPI.Exceptions;
using TravelAPI.Contracts.Requests;

public class ClientService : IClientService
{
    private readonly string _connectionString;
    public ClientService(IConfiguration cfg)
    {
        _connectionString = cfg.GetConnectionString("Default") ??
                            throw new ArgumentNullException(nameof(cfg), "No Default connection string was specified.");
    }
    
    public async Task<ICollection<ClientTrip>> GetAllClientTripsAsync(int id, CancellationToken cancellationToken)
    {
        var clientTrips = new List<ClientTrip>();
        await ValidateClientExistsAsync(id, cancellationToken);
        const string query = """
                       SELECT
                           t.IdTrip AS TripId,
                           t.Name AS TripName,
                           t.Description AS TripDescription,
                           t.DateFrom AS StartDate,
                           t.DateTo AS EndDate,
                           t.MaxPeople AS MaximumParticipants,
                           ct.RegisteredAt AS RegistrationDate,
                           ct.PaymentDate AS DateOfPayment,
                           co.IdCountry AS CountryId,
                           co.Name AS CountryName
                       FROM
                           Client_Trip AS ct
                               JOIN Trip AS t ON ct.IdTrip = t.IdTrip
                               JOIN Country_Trip AS ctr ON ctr.IdTrip = t.IdTrip
                               JOIN Country AS co ON co.IdCountry = ctr.IdCountry
                       WHERE
                           ct.IdClient = @clientId;
                       """;

        await using (SqlConnection connection = new(_connectionString))
        {
            await using (SqlCommand command = new(query, connection))
            {
                command.Parameters.AddWithValue("@clientId", id);
                await connection.OpenAsync(cancellationToken);
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var tripId = reader.GetInt32(0);

                    var clientTrip = new ClientTrip()
                    {
                        ClientId = id,
                        TripId = tripId,
                        Name = reader.GetString(1),
                        Description = reader.GetString(2),
                        DateFrom = reader.GetDateTime(3),
                        DateTo = reader.GetDateTime(4),
                        MaxPeople = reader.GetInt32(5),
                        RegisteredAt = reader.GetInt32(6),
                        PaymentDate = reader.GetInt32(7),
                        Countries = new List<Country>()
                    };
                    clientTrips.Add(clientTrip);

                    if (!reader.IsDBNull(8))
                    {
                        var countryId = reader.GetInt32(8);
                        var countryName = reader.GetString(9);
                        var country = new Country()
                        {
                            Id = countryId,
                            Name = countryName
                        };
                        clientTrip.Countries.Add(country);
                    }
                }
            }
        }
        return clientTrips;
    }

    public async Task<int> CreateClientAsync(CreateClientRequest clientRequest, CancellationToken cancellationToken)
    {
        await ValidateClientExistsAsync(clientRequest.Pesel, cancellationToken);
        const string query = """
                             INSERT INTO Client(FirstName, LastName, Email, Telephone, Pesel)
                             VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel)
                             SELECT SCOPE_IDENTITY();
                             """;
        await using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            await using (SqlCommand command = new SqlCommand(query, connection))
            {
                await connection.OpenAsync(cancellationToken);
                command.Parameters.AddWithValue("@FirstName", clientRequest.FirstName);
                command.Parameters.AddWithValue("@LastName", clientRequest.LastName);
                command.Parameters.AddWithValue("@Email", clientRequest.Email);
                command.Parameters.AddWithValue("@Telephone", clientRequest.Telephone);
                command.Parameters.AddWithValue("@Pesel", clientRequest.Pesel);
                
                var id = await command.ExecuteScalarAsync(cancellationToken);
                return Convert.ToInt32(id);
            }
        }
    }

    public async Task ValidateClientExistsAsync(int id, CancellationToken cancellationToken)
    {
        var exists = await ClientExistsByIdAsync(id, cancellationToken);
        if (!exists) throw new NoSuchClientException(id);
    }
    
    public async Task ValidateClientExistsAsync(string pesel, CancellationToken cancellationToken)
    {
        var exists = await ClientExistsByPeselAsync(pesel, cancellationToken);
        Console.WriteLine(exists);
        if (exists) throw new ClientAlreadyExistsException(pesel);
    }

    public async ValueTask<bool> ClientExistsByIdAsync(int id, CancellationToken cancellationToken)
    {
        const string query = """
                             SELECT 
                                 IIF(EXISTS (SELECT 1 FROM Client 
                                         WHERE Client.IdClient = @clientId), 1, 0) AS ClientExists;
                             """;
        await using (var connection = new SqlConnection(_connectionString))
        {
            await using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@clientId", id);
                await connection.OpenAsync(cancellationToken);

                var result = await command.ExecuteScalarAsync(cancellationToken);
                return Convert.ToInt32(result) == 1;
            }
        }
    }

    public async ValueTask<bool> ClientExistsByPeselAsync(string pesel, CancellationToken cancellationToken)
    {
        const string query = """
                             SELECT 
                                 IIF(EXISTS (SELECT 1 FROM Client 
                                         WHERE Client.Pesel = @clientPesel), 1, 0) AS ClientExists
                             """;
        await using (var connection = new SqlConnection(_connectionString))
        {
            await using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@clientPesel", pesel);
                await connection.OpenAsync(cancellationToken);

                var result = await command.ExecuteScalarAsync(cancellationToken);
                return Convert.ToInt32(result) == 1;
            }
        }
    }
}