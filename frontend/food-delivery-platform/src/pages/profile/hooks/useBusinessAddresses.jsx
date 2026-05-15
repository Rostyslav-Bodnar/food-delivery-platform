import { useEffect, useMemo, useState } from "react";

import { getBusinessLocationsByBusinessId }
    from "../../../api/BusinessLocation"

import { addBusinessLocation }
    from "../../../api/Location"


import { useUser } from "../../../context/UserContext";
import {
    reverseGeocodeAddress,
    searchAddressSuggestions
} from "../../../utils/locationSearch.js";

const DEFAULT_CENTER = {
    latitude: 50.4501,
    longitude: 30.5234
};

const createEmptyAddressForm = () => ({
    fullAddress: "",
    city: "",
    street: "",
    house: "",
    latitude: "",
    longitude: ""
});

const normalizeBusinessAddress = (address, index) => ({
    id: address.id ?? `business-address-${index}`,
    locationId: address.locationId ?? address.location?.id ?? null,
    fullAddress: address.fullAddress ?? address.location?.fullAddress ?? address.address ?? String(address),
    city: address.city ?? address.location?.city ?? "",
    street: address.street ?? address.location?.street ?? "",
    house: address.house ?? address.location?.house ?? "",
    latitude: address.latitude ?? address.location?.latitude ?? address.lat ?? null,
    longitude: address.longitude ?? address.location?.longitude ?? address.lng ?? null
});

const useBusinessAddresses = () => {
    const { accounts, currentAccountId, reloadUser } = useUser();
    const [addressForm, setAddressForm] = useState(createEmptyAddressForm);
    const [businessAddresses, setBusinessAddresses] = useState([]);
    const [loadingLocations, setLoadingLocations] = useState(false);
    const [loadError, setLoadError] = useState("");
    const [searchQuery, setSearchQuery] = useState("");
    const [searchResults, setSearchResults] = useState([]);
    const [searching, setSearching] = useState(false);
    const [searchError, setSearchError] = useState("");
    const [submitting, setSubmitting] = useState(false);
    const [submitError, setSubmitError] = useState("");
    const [submitSuccess, setSubmitSuccess] = useState("");
    const [isComposerOpen, setIsComposerOpen] = useState(false);
    const [mapCenter, setMapCenter] = useState([
        DEFAULT_CENTER.latitude,
        DEFAULT_CENTER.longitude
    ]);
    const [isResolvingPoint, setIsResolvingPoint] = useState(false);

    const currentAccount = useMemo(
        () => accounts.find((account) => account.id === currentAccountId),
        [accounts, currentAccountId]
    );

    useEffect(() => {
        const loadBusinessLocations = async () => {
            if (currentAccount?.accountType !== "Business" || !currentAccountId) {
                setBusinessAddresses([]);
                setLoadError("");
                return;
            }

            setLoadingLocations(true);
            setLoadError("");

            try {
                const locations = await getBusinessLocationsByBusinessId(currentAccountId);
                const normalized = (locations ?? []).map(normalizeBusinessAddress);
                setBusinessAddresses(normalized);

                if (normalized[0]?.latitude && normalized[0]?.longitude) {
                    setMapCenter([Number(normalized[0].latitude), Number(normalized[0].longitude)]);
                }
            } catch (error) {
                console.error(error);
                setLoadError("Failed to load business locations.");
                setBusinessAddresses([]);
            } finally {
                setLoadingLocations(false);
            }
        };

        loadBusinessLocations();
    }, [currentAccount, currentAccountId]);

    useEffect(() => {
        if (!searchQuery.trim()) {
            setSearchResults([]);
            setSearchError("");
            return undefined;
        }

        const timeoutId = window.setTimeout(async () => {
            setSearching(true);
            setSearchError("");

            try {
                const results = await searchAddressSuggestions(searchQuery);
                setSearchResults(results);
            } catch (error) {
                console.error(error);
                setSearchError("Failed to load address suggestions.");
            } finally {
                setSearching(false);
            }
        }, 350);

        return () => window.clearTimeout(timeoutId);
    }, [searchQuery]);

    const applyAddressData = (addressData) => {
        setAddressForm({
            fullAddress: addressData.fullAddress ?? "",
            city: addressData.city ?? "",
            street: addressData.street ?? "",
            house: addressData.house ?? "",
            latitude: addressData.latitude ?? "",
            longitude: addressData.longitude ?? ""
        });

        if (addressData.latitude && addressData.longitude) {
            setMapCenter([Number(addressData.latitude), Number(addressData.longitude)]);
        }
    };

    const openComposer = () => {
        setIsComposerOpen(true);
        setSubmitError("");
        setSubmitSuccess("");
    };

    const closeComposer = () => {
        setIsComposerOpen(false);
        setAddressForm(createEmptyAddressForm());
        setSearchQuery("");
        setSearchResults([]);
        setSearchError("");
        setSubmitError("");
    };

    const handleAddressFieldChange = (event) => {
        const { name, value } = event.target;
        setAddressForm((prev) => ({
            ...prev,
            [name]: value
        }));

        if (name === "fullAddress") {
            setSearchQuery(value);
        }
    };

    const selectSuggestion = (suggestion) => {
        applyAddressData(suggestion);
        setSearchQuery(suggestion.fullAddress);
        setSearchResults([]);
        setSearchError("");
    };

    const selectPointOnMap = async ({ lat, lng }) => {
        setIsResolvingPoint(true);
        setSubmitError("");

        try {
            const resolvedAddress = await reverseGeocodeAddress({
                latitude: lat,
                longitude: lng
            });

            applyAddressData(resolvedAddress);
            setSearchQuery(resolvedAddress.fullAddress);
        } catch (error) {
            console.error(error);
            setSubmitError("Failed to resolve address from the selected map point.");
        } finally {
            setIsResolvingPoint(false);
        }
    };

    const handleSubmit = async (event) => {
        event.preventDefault();

        if (!currentAccountId) {
            setSubmitError("Active business account was not found.");
            return;
        }

        const latitude = Number(addressForm.latitude);
        const longitude = Number(addressForm.longitude);

        if (
            !addressForm.fullAddress.trim() ||
            !addressForm.city.trim() ||
            !addressForm.street.trim() ||
            !addressForm.house.trim() ||
            !Number.isFinite(latitude) ||
            !Number.isFinite(longitude)
        ) {
            setSubmitError("Fill in the full address, city, street, house and map coordinates.");
            return;
        }

        setSubmitting(true);
        setSubmitError("");
        setSubmitSuccess("");

        try {
            await addBusinessLocation({
                businessId: currentAccountId,
                fullAddress: addressForm.fullAddress.trim(),
                city: addressForm.city.trim(),
                street: addressForm.street.trim(),
                house: addressForm.house.trim(),
                latitude,
                longitude
            });

            await reloadUser();
            const locations = await getBusinessLocationsByBusinessId(currentAccountId);
            const normalized = (locations ?? []).map(normalizeBusinessAddress);
            setBusinessAddresses(normalized);
            setSubmitSuccess("Business location has been added.");
            closeComposer();
        } catch (error) {
            console.error(error);
            setSubmitError(error?.response?.data?.message || error?.message || "Failed to add business location.");
        } finally {
            setSubmitting(false);
        }
    };

    return {
        businessAddresses,
        loadingLocations,
        loadError,
        addressForm,
        searchQuery,
        searchResults,
        searching,
        searchError,
        submitting,
        submitError,
        submitSuccess,
        isComposerOpen,
        mapCenter,
        isResolvingPoint,
        openComposer,
        closeComposer,
        handleAddressFieldChange,
        selectSuggestion,
        selectPointOnMap,
        handleSubmit
    };
};

export default useBusinessAddresses;
