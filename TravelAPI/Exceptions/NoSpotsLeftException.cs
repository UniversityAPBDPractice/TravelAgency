namespace TravelAPI.Exceptions;

public class NoSpotsLeftException(int id) : Exception($"No available space for this trip left, id: {id}");