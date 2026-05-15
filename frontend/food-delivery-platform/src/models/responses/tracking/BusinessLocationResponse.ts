import type { LocationResponse } from './LocationResponse';

export interface BusinessLocationResponse {
    id: string;      
    locationId: string;  
    location: LocationResponse;
    businessId: string; 
}