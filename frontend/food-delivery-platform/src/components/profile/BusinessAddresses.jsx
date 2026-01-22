import React from "react";
import "./styles/BusinessAddresses.css";

const BusinessAddresses = ({
                               businessAddresses,
                               editingAddressId,
                               addressForm,
                               startAddingAddress,
                               startEditingAddress,
                               deleteAddress,
                               handleAddressSubmit,
                               cancelAddressEdit,
                               setAddressForm,
                           }) => {
    return (
        <div className="business-addresses">
            <h3 className="business-addresses-title">
                Establishment's addresses
            </h3>

            {businessAddresses.map((addr) => (
                <div key={addr.id} className="business-address-card">
                    <p className="business-address-text">
                         {addr.address}
                    </p>

                    <button
                        className="business-address-edit"
                        onClick={() => startEditingAddress(addr)}
                    >
                         Edit
                    </button>

                    <button
                        className="business-address-delete"
                        onClick={() => deleteAddress(addr.id)}
                    >
                         Delete
                    </button>
                </div>
            ))}

            {editingAddressId && (
                <div className="business-address-form">
                    <form onSubmit={handleAddressSubmit}>
                        <input
                            type="text"
                            placeholder="Enter establishment's address"
                            value={addressForm.address}
                            onChange={(e) =>
                                setAddressForm({ address: e.target.value })
                            }
                            required
                            autoFocus
                            className="business-address-input"
                        />

                        <div className="business-address-actions">
                            <button type="submit" className="business-address-submit">
                                {editingAddressId === "new"
                                    ? "Add address"
                                    : "Save"}
                            </button>

                            <button
                                type="button"
                                onClick={cancelAddressEdit}
                                className="business-address-cancel"
                            >
                                Cancel
                            </button>
                        </div>
                    </form>
                </div>
            )}

            {!editingAddressId && (
                <button
                    className="business-address-add"
                    onClick={startAddingAddress}
                    style={{
                        marginTop:
                            businessAddresses.length > 0 ? "12px" : "0",
                    }}
                >
                    Add establishment address
                </button>
            )}
        </div>
    );
};

export default BusinessAddresses;
