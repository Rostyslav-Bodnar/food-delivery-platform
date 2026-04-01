const NOMINATIM_BASE_URL = "https://nominatim.openstreetmap.org";

const buildSearchParams = (params) => {
    const searchParams = new URLSearchParams();

    Object.entries(params).forEach(([key, value]) => {
        if (value !== undefined && value !== null && value !== "") {
            searchParams.set(key, String(value));
        }
    });

    return searchParams.toString();
};

const requestLocation = async (path, params) => {
    const response = await fetch(`${NOMINATIM_BASE_URL}${path}?${buildSearchParams(params)}`, {
        headers: {
            Accept: "application/json"
        }
    });

    if (!response.ok) {
        throw new Error(`Location request failed with status ${response.status}`);
    }

    return response.json();
};

const extractHouseNumber = (address = {}) =>
    address.house_number ||
    address.building ||
    address.shop ||
    "";

const extractStreet = (address = {}) =>
    address.road ||
    address.pedestrian ||
    address.footway ||
    address.street ||
    address.neighbourhood ||
    "";

const extractCity = (address = {}) =>
    address.city ||
    address.town ||
    address.village ||
    address.municipality ||
    address.county ||
    "";

export const parseAddressParts = (address = {}) => ({
    city: extractCity(address),
    street: extractStreet(address),
    house: extractHouseNumber(address)
});

export const searchAddressSuggestions = async (query) => {
    if (!query.trim()) {
        return [];
    }

    const data = await requestLocation("/search", {
        q: query,
        format: "jsonv2",
        addressdetails: 1,
        limit: 5
    });

    return data.map((item) => ({
        id: item.place_id,
        fullAddress: item.display_name,
        latitude: Number(item.lat),
        longitude: Number(item.lon),
        ...parseAddressParts(item.address)
    }));
};

export const reverseGeocodeAddress = async ({ latitude, longitude }) => {
    const data = await requestLocation("/reverse", {
        format: "jsonv2",
        lat: latitude,
        lon: longitude,
        zoom: 18,
        addressdetails: 1
    });

    return {
        fullAddress: data.display_name ?? "",
        latitude: Number(latitude),
        longitude: Number(longitude),
        ...parseAddressParts(data.address)
    };
};
