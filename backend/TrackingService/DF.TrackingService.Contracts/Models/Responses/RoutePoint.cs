namespace DF.TrackingService.Contracts.Models.Responses;

public record RoutePoint(double Longitude, double Latitude);

public record RouteResult(
    double DistanceMeters, 
    double DurationSeconds, 
    string GeometryGeoJson,
    IReadOnlyList<double> LegDistances,
    IReadOnlyList<double> LegDurations
);