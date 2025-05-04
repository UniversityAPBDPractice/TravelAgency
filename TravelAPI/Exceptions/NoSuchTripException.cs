namespace TravelAPI.Exceptions;

public class NoSuchTripException(int id) : Exception($"No trip with such id {id}");