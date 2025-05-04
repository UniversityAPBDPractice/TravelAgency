namespace TravelAPI.Exceptions;

public class ClientAlreadyExistsException(string pesel) : Exception($"Client already exists with such Pesel {pesel}");