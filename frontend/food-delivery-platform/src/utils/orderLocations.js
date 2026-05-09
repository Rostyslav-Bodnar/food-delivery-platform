const readNestedLocation = (source = {}, prefix = "") => {
    if (!prefix) {
        return source;
    }

    return source[`${prefix}Location`] ?? source;
};

export const buildLocation = (source = {}, prefix = "") => {
    const raw = readNestedLocation(source, prefix);
    const fullAddress = raw.fullAddress ?? raw.FullAddress ?? source[`${prefix}FullAddress`] ?? "";
    const city = raw.city ?? raw.City ?? source[`${prefix}City`] ?? "";
    const street = raw.street ?? raw.Street ?? source[`${prefix}Street`] ?? "";
    const house = raw.house ?? raw.House ?? source[`${prefix}House`] ?? "";
    const latitude = Number(raw.latitude ?? raw.Latitude ?? source[`${prefix}Latitude`] ?? 0);
    const longitude = Number(raw.longitude ?? raw.Longitude ?? source[`${prefix}Longitude`] ?? 0);

    return {
        fullAddress,
        city,
        street,
        house,
        latitude,
        longitude
    };
};

export const formatLocation = (location) => {
    if (!location) {
        return "";
    }

    if (location.fullAddress) {
        return location.fullAddress;
    }

    return [location.city, location.street, location.house]
        .filter(Boolean)
        .join(", ");
};

export const hasCoordinates = (location) =>
    Boolean(location) &&
    Number.isFinite(location.latitude) &&
    Number.isFinite(location.longitude) &&
    (location.latitude !== 0 || location.longitude !== 0);
