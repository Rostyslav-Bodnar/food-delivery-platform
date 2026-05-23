using System;
using System.Collections.Generic;

namespace DF.Contracts.RPC.Requests.TrackingService;

public record GetLocationRequest(Guid DeliverToId, Guid DeliverFromId);
public record GetLocationsBatchRequest(List<GetLocationRequest> Locations);