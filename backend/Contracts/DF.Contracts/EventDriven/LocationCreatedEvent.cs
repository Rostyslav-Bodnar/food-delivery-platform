using System;

namespace DF.Contracts.EventDriven;

public record LocationsCreatedForOrder(
    Guid OrderId,
    LocationDTO DeliverTo, 
    LocationDTO DeliverFromId
    );
    
    public record LocationDTO(
        Guid Id, 
        double Latitude,
        double Longitude
    );