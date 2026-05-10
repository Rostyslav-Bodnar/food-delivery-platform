// src/hooks/useProfileForm.js
import { useEffect, useState, useRef } from "react";
import { useUser } from "../../../context/UserContext";
import { updateAccount } from "../../../api/Account.ts";
import { refresh } from "../../../api/Auth.ts";

const useProfileForm = () => {
    const { user, accounts, currentAccountId, reloadUser } = useUser();

    const [error, setError] = useState(null);
    const [editingField, setEditingField] = useState(null);
    const [formData, setFormData] = useState({
        name: "",
        phone: "",
        address: "",
        avatar: null
    });
    const [isAvatarHovered, setIsAvatarHovered] = useState(false);
    const inputRef = useRef(null);

    const accountTypeMap = { Customer: 0, Business: 1, Courier: 2 };

    useEffect(() => {
        if (user && accounts && currentAccountId) {
            const currentAccount = accounts.find(a => a.id === currentAccountId);
            if (currentAccount) {
                setFormData({
                    name: currentAccount.name || user.name || "",
                    phone: currentAccount.phoneNumber || "",
                    address: currentAccount.address || "",
                    avatar: currentAccount.imageUrl || null
                });
            }
        }
    }, [user, accounts, currentAccountId]);

    const clearError = () => setError(null);

    const handleEditToggle = (field) => setEditingField(field);

    const handleInputChange = (e) => {
        const { name, value } = e.target;
        setFormData(prev => ({ ...prev, [name]: value }));
    };

    const getValidToken = async () => {
        let token = localStorage.getItem("accessToken");
        if (!token) {
            const tokens = await refresh();
            token = tokens.accessToken;
        }
        return token;
    };

    const buildBasePayload = (currentAccount) => ({
        Id: currentAccount.id,
        UserId: currentAccount.userId,
        AccountType: accountTypeMap[currentAccount.accountType] ?? 0,
        Name: formData.name || currentAccount.name,
        PhoneNumber: formData.phone || currentAccount.phoneNumber || "",
        Address: formData.address || currentAccount.address || "",
        Surname: currentAccount.surname || "",
        Description: currentAccount.description || ""
    });

    const handleAvatarChange = async (e) => {
        const file = e.target.files[0];
        if (!file) return;

        setFormData(prev => ({
            ...prev,
            avatar: URL.createObjectURL(file)
        }));
        setIsAvatarHovered(false);
        setError(null);

        try {
            const currentAccount = accounts.find(a => a.id === currentAccountId);
            if (!currentAccount) throw new Error("No active account found");

            const payload = {
                ...buildBasePayload(currentAccount),
                ImageFile: file
            };

            await updateAccount(
                currentAccount.accountType.toLowerCase(),
                payload
            );

            await reloadUser();
        } catch (err) {
            setError(err.message);
        }
    };

    const handleSave = async (field) => {
        setError(null);

        try {
            const token = await getValidToken();
            const currentAccount = accounts.find(a => a.id === currentAccountId);
            if (!currentAccount) throw new Error("No active account found");

            const payload = buildBasePayload(currentAccount);

            if (field === "name") payload.Name = formData.name;
            if (field === "phone") payload.PhoneNumber = formData.phone || "";
            if (field === "address") payload.Address = formData.address || "";

            const file = inputRef.current?.files?.[0];
            if (file) payload.ImageFile = file;

            await updateAccount(
                currentAccount.accountType.toLowerCase(),
                payload,
                token
            );

            setEditingField(null);
            await reloadUser();
        } catch (err) {
            setError(err.message);
        }
    };

    return {
        error,
        clearError,
        editingField,
        formData,
        isAvatarHovered,
        setIsAvatarHovered,
        inputRef,
        handleEditToggle,
        handleInputChange,
        handleAvatarChange,
        handleSave
    };
};

export default useProfileForm;