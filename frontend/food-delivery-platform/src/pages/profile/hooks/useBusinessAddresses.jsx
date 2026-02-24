// src/hooks/useBusinessAddresses.js
import { useEffect, useState } from "react";
import { useUser } from "../../../context/UserContext";

const useBusinessAddresses = () => {
    const { user, accounts, currentAccountId } = useUser();

    const [businessAddresses, setBusinessAddresses] = useState([]);
    const [editingAddressId, setEditingAddressId] = useState(null);
    const [addressForm, setAddressForm] = useState({ address: "" });

    useEffect(() => {
        if (user && accounts && currentAccountId) {
            const currentAccount = accounts.find(a => a.id === currentAccountId);
            if (currentAccount) {
                if (currentAccount.accountType === "Business" && currentAccount.businessAddresses) {
                    const addresses = currentAccount.businessAddresses.map((addr, index) => ({
                        id: addr.id || `addr-${index}`,
                        address: addr.address || addr
                    }));
                    setBusinessAddresses(addresses);
                } else if (currentAccount.accountType !== "Business") {
                    setBusinessAddresses([]);
                }
            }
        }
    }, [user, accounts, currentAccountId]);

    const startAddingAddress = () => {
        setEditingAddressId("new");
        setAddressForm({ address: "" });
    };

    const startEditingAddress = (addr) => {
        setEditingAddressId(addr.id);
        setAddressForm({ address: addr.address });
    };

    const deleteAddress = (id) => {
        setBusinessAddresses(prev => prev.filter(a => a.id !== id));
    };

    const handleAddressSubmit = (e) => {
        e.preventDefault();
        if (!addressForm.address.trim()) return;

        if (editingAddressId === "new") {
            const newAddr = {
                id: Date.now().toString(),
                address: addressForm.address.trim()
            };
            setBusinessAddresses(prev => [...prev, newAddr]);
        } else {
            setBusinessAddresses(prev => prev.map(a =>
                a.id === editingAddressId ? { ...a, address: addressForm.address.trim() } : a
            ));
        }

        setEditingAddressId(null);
        setAddressForm({ address: "" });
    };

    const cancelAddressEdit = () => {
        setEditingAddressId(null);
        setAddressForm({ address: "" });
    };

    return {
        businessAddresses,
        editingAddressId,
        addressForm,
        startAddingAddress,
        startEditingAddress,
        deleteAddress,
        handleAddressSubmit,
        cancelAddressEdit,
        setAddressForm,
    };
};

export default useBusinessAddresses;