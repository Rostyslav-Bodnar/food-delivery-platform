export interface CreateLocationRequest {
    fullAddress: string;
    city: string;
    street: string;
    house: string;
}

export interface UpdateLocationRequest {
    fullAddress: string;
    city: string;
    street: string;
    house: string;
}

export interface AddLocationRequest {
    businessId: string; // Guid у C# → string (UUID) у TS
    fullAddress: string;
    city: string;
    street: string;
    house: string;
    latitude: number;
    longitude: number;
}
