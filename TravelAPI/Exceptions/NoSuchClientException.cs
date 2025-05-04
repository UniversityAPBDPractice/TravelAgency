namespace TravelAPI.Exceptions;

public class NoSuchClientException(int id) : Exception($"Client does not exists with {id} id");