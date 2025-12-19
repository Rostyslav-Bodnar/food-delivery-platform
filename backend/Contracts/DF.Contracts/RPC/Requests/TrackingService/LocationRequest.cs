using System;

namespace DF.Contracts.RPC.Requests.TrackingService;

public record GetLocationRequest(Guid DeliverToId, Guid DeliverFromId);