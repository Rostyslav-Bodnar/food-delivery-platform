// src/hooks/useProfileForm.js
import { useEffect, useState, useRef } from "react";
import { useUser } from "../../../context/UserContext";
import { updateProfile } from "../../../api/Profile.jsx";
import { refresh } from "../../../api/Auth.jsx";

const useProfileForm = () => {
    const { user, accounts, currentAccountId, reloadUser } = useUser();

    const [error, setError] = useState(null);
    const [editingField, setEditingField] = useState(null);
    const [formData, setFormData] = useState({ name: "", phone: "", address: "", avatar: null });
    const [isAvatarHovered, setIsAvatarHovered] = useState(false);
    const inputRef = useRef(null);

    const accountTypeMap = { Customer: 0, Business: 1, Courier: 2 };

    useEffect(() => {
        if (user && accounts && currentAccountId) {
            const currentAccount = accounts.find(a => a.id === currentAccountId);
            if (currentAccount) {
                setFormData({
                    name: currentAccount?.name || user.name || "",
                    phone: currentAccount?.phoneNumber || "",
                    address: currentAccount?.address || "",
                    avatar: currentAccount?.imageUrl || null,
                });
            }
        }
    }, [user, accounts, currentAccountId]);

    const handleEditToggle = (field) => setEditingField(field);

    const handleInputChange = (e) => {
        const { name, value } = e.target;
        setFormData(prev => ({ ...prev, [name]: value }));
    };

    const handleAvatarChange = async (e) => {
        const file = e.target.files[0];
        if (!file) return;

        const avatarUrl = URL.createObjectURL(file);
        setFormData(prev => ({ ...prev, avatar: avatarUrl }));
        setIsAvatarHovered(false);

        try {
            let token = localStorage.getItem("accessToken");
            if (!token) {
                const tokens = await refresh();
                token = tokens.accessToken;
            }

            const currentAccount = accounts.find(a => a.id === currentAccountId);
            if (!currentAccount) throw new Error("No active account found");

            const body = {
                Id: currentAccount.id,
                UserId: currentAccount.userId,
                AccountType: accountTypeMap[currentAccount.accountType] ?? 0,
                Name: formData.name || currentAccount.name,
                PhoneNumber: formData.phone || currentAccount.phoneNumber || "",
                Address: formData.address || currentAccount.address || "",
                Surname: currentAccount.surname || "",
                Description: currentAccount.description || "",
                ImageFile: file
            };

            await updateProfile(currentAccount.accountType.toLowerCase(), body, token);
            await reloadUser();
        } catch (err) {
            setError(err.response?.data || err.message || "Failed to update avatar");
        }
    };

    const handleSave = async (field) => {
        try {
            let token = localStorage.getItem("accessToken");
            if (!token) {
                const tokens = await refresh();
                token = tokens.accessToken;
            }

            const currentAccount = accounts.find(a => a.id === currentAccountId);
            if (!currentAccount) throw new Error("No active account found");

            const body = {
                Id: currentAccount.id,
                UserId: currentAccount.userId,
                AccountType: accountTypeMap[currentAccount.accountType] ?? 0,
                Name: field === "name" ? formData.name : currentAccount.name,
                PhoneNumber: field === "phone" ? formData.phone || "" : currentAccount.phoneNumber || "",
                Address: field === "address" ? formData.address || "" : currentAccount.address || "",
                Surname: currentAccount.surname || "",
                Description: currentAccount.description || "",
            };

            if (inputRef.current?.files?.[0]) {
                body.ImageFile = inputRef.current.files[0];
            }

            await updateProfile(currentAccount.accountType.toLowerCase(), body, token);
            setEditingField(null);
            await reloadUser();
        } catch (err) {
            setError(err.response?.data || err.message || "Failed to save profile");
        }
    };

    return {
        error,
        editingField,
        formData,
        isAvatarHovered,
        setIsAvatarHovered,
        inputRef,
        handleEditToggle,
        handleInputChange,
        handleAvatarChange,
        handleSave,
    };
};

export default useProfileForm;