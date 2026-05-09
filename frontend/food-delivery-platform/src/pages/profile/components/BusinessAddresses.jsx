import React from "react";
import { MapContainer, TileLayer, CircleMarker, useMap, useMapEvents } from "react-leaflet";
import "leaflet/dist/leaflet.css";
import {
    Building2,
    CheckCircle2,
    ChevronDown,
    LoaderCircle,
    MapPin,
    Plus,
    Search
} from "lucide-react";
import "../styles/BusinessAddresses.css";

const MapClickHandler = ({ onSelect }) => {
    useMapEvents({
        click(event) {
            onSelect(event.latlng);
        }
    });

    return null;
};

const MapViewUpdater = ({ center }) => {
    const map = useMap();

    React.useEffect(() => {
        map.setView(center, map.getZoom(), {
            animate: true
        });
    }, [center, map]);

    return null;
};

const BusinessAddresses = ({
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
}) => {
    const hasMapPoint = addressForm.latitude !== "" && addressForm.longitude !== "";

    return (
        <section className="business-addresses">
            <div className="business-addresses__header">
                <div>
                    <h3>Business locations</h3>
                    <p>
                        Add verified pickup points for your business. Customers and couriers will rely on these addresses.
                    </p>
                </div>

                <button
                    type="button"
                    className="business-addresses__primary-button"
                    onClick={isComposerOpen ? closeComposer : openComposer}
                >
                    {isComposerOpen ? <ChevronDown size={18} /> : <Plus size={18} />}
                    {isComposerOpen ? "Hide form" : "Add location"}
                </button>
            </div>

            {submitSuccess && (
                <div className="business-addresses__notice is-success">
                    <CheckCircle2 size={16} />
                    {submitSuccess}
                </div>
            )}

            {loadError && (
                <div className="business-addresses__notice is-error">
                    <MapPin size={16} />
                    {loadError}
                </div>
            )}

            {loadingLocations ? (
                <div className="business-addresses__loading">
                    <LoaderCircle size={18} className="spin" />
                    <span>Loading saved business locations...</span>
                </div>
            ) : businessAddresses.length > 0 ? (
                <div className="business-addresses__grid">
                    {businessAddresses.map((address, index) => (
                        <article key={address.id} className="business-address-card">
                            <div className="business-address-card__top">
                                <div className="business-address-card__badge">
                                    <Building2 size={14} />
                                    {index === 0 ? "Main location" : `Location ${index + 1}`}
                                </div>
                                {address.latitude && address.longitude && (
                                    <div className="business-address-card__coords">
                                        <MapPin size={14} />
                                        {Number(address.latitude).toFixed(5)}, {Number(address.longitude).toFixed(5)}
                                    </div>
                                )}
                            </div>

                            <h4>{address.fullAddress}</h4>

                            <div className="business-address-card__meta">
                                <span>{address.city || "City not specified"}</span>
                                <span>{address.street || "Street not specified"}</span>
                                <span>{address.house || "House not specified"}</span>
                            </div>
                        </article>
                    ))}
                </div>
            ) : (
                <div className="business-addresses__empty">
                    <MapPin size={18} />
                    <div>
                        <strong>No business locations yet</strong>
                        <p>Add the first location so the restaurant can receive orders correctly.</p>
                    </div>
                </div>
            )}

            {isComposerOpen && (
                <form className="business-address-form" onSubmit={handleSubmit}>
                    <div className="business-address-form__heading">
                        <div>
                            <h4>Add a new location</h4>
                            <p>Search the address or click directly on the map. The form will fill city, street and house automatically when possible.</p>
                        </div>
                    </div>

                    <div className="business-address-form__search-shell">
                        <label htmlFor="business-address-search">Search by address</label>
                        <div className="business-address-form__search-input">
                            {searching ? <LoaderCircle size={16} className="spin" /> : <Search size={16} />}
                            <input
                                id="business-address-search"
                                type="text"
                                name="fullAddress"
                                placeholder="Start typing the business address"
                                value={searchQuery}
                                onChange={handleAddressFieldChange}
                            />
                        </div>

                        {searchError && (
                            <div className="business-addresses__notice is-error">{searchError}</div>
                        )}

                        {searchResults.length > 0 && (
                            <div className="business-address-form__results">
                                {searchResults.map((result) => (
                                    <button
                                        key={result.id}
                                        type="button"
                                        className="business-address-form__result"
                                        onClick={() => selectSuggestion(result)}
                                    >
                                        <MapPin size={15} />
                                        <div>
                                            <strong>{result.fullAddress}</strong>
                                            <span>{result.city || "Unknown city"}</span>
                                        </div>
                                    </button>
                                ))}
                            </div>
                        )}
                    </div>

                    <div className="business-address-form__map-shell">
                        <MapContainer
                            center={mapCenter}
                            zoom={13}
                            scrollWheelZoom={false}
                            className="business-address-form__map"
                        >
                            <MapViewUpdater center={mapCenter} />
                            <TileLayer
                                url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                                attribution="&copy; OpenStreetMap contributors"
                            />
                            <MapClickHandler onSelect={selectPointOnMap} />
                            {hasMapPoint && (
                                <CircleMarker
                                    center={[Number(addressForm.latitude), Number(addressForm.longitude)]}
                                    radius={10}
                                    pathOptions={{
                                        color: "#00d4ff",
                                        fillColor: "#7c5cff",
                                        fillOpacity: 0.9,
                                        weight: 3
                                    }}
                                />
                            )}
                        </MapContainer>

                        <div className="business-address-form__map-hint">
                            <span>
                                <MapPin size={14} />
                                Click the map to pin the exact entrance or storefront location.
                            </span>
                            {isResolvingPoint && (
                                <span>
                                    <LoaderCircle size={14} className="spin" />
                                    Resolving address...
                                </span>
                            )}
                        </div>
                    </div>

                    <div className="business-address-form__grid is-single">
                        <div className="business-address-form__field">
                            <label htmlFor="fullAddress">Full address</label>
                            <input
                                id="fullAddress"
                                type="text"
                                name="fullAddress"
                                value={addressForm.fullAddress}
                                onChange={handleAddressFieldChange}
                                placeholder="Full business address"
                                required
                            />
                        </div>
                    </div>

                    <div className="business-address-form__grid">
                        <div className="business-address-form__field">
                            <label htmlFor="city">City</label>
                            <input
                                id="city"
                                type="text"
                                name="city"
                                value={addressForm.city}
                                onChange={handleAddressFieldChange}
                                placeholder="City"
                                required
                            />
                        </div>

                        <div className="business-address-form__field">
                            <label htmlFor="street">Street</label>
                            <input
                                id="street"
                                type="text"
                                name="street"
                                value={addressForm.street}
                                onChange={handleAddressFieldChange}
                                placeholder="Street"
                                required
                            />
                        </div>

                        <div className="business-address-form__field">
                            <label htmlFor="house">House</label>
                            <input
                                id="house"
                                type="text"
                                name="house"
                                value={addressForm.house}
                                onChange={handleAddressFieldChange}
                                placeholder="House / unit"
                                required
                            />
                        </div>
                    </div>

                    <div className="business-address-form__grid">
                        <div className="business-address-form__field">
                            <label htmlFor="latitude">Latitude</label>
                            <input
                                id="latitude"
                                type="text"
                                name="latitude"
                                value={addressForm.latitude}
                                onChange={handleAddressFieldChange}
                                placeholder="50.45010"
                                required
                            />
                        </div>

                        <div className="business-address-form__field">
                            <label htmlFor="longitude">Longitude</label>
                            <input
                                id="longitude"
                                type="text"
                                name="longitude"
                                value={addressForm.longitude}
                                onChange={handleAddressFieldChange}
                                placeholder="30.52340"
                                required
                            />
                        </div>
                    </div>

                    {submitError && (
                        <div className="business-addresses__notice is-error">{submitError}</div>
                    )}

                    <div className="business-address-form__footer">
                        <p>The address is saved directly via the Tracking service using the business location DTO.</p>
                        <button
                            type="submit"
                            className="business-addresses__primary-button"
                            disabled={submitting}
                        >
                            {submitting ? <LoaderCircle size={18} className="spin" /> : <Plus size={18} />}
                            {submitting ? "Saving..." : "Save location"}
                        </button>
                    </div>
                </form>
            )}
        </section>
    );
};

export default BusinessAddresses;
