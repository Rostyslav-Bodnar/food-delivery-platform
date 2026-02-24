import { useEffect, useState } from "react";
import { getAccounts } from "../../../api/Account.jsx";

const ALL_ACCOUNT_TYPES = ["Customer", "Business", "Courier"];

export const useAccountTypes = (user) => {
    const [accountType, setAccountType] = useState(null);
    const [existingAccounts, setExistingAccounts] = useState([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        if (!user) return;

        const fetchAccounts = async () => {
            try {
                const accounts = await getAccounts(user.id);
                const existingTypes = accounts.map(a => a.accountType);

                setExistingAccounts(existingTypes);

                const availableType = ALL_ACCOUNT_TYPES.find(
                    type => !existingTypes.includes(type)
                );

                setAccountType(availableType || null);
            } catch (err) {
                console.error("Failed to fetch accounts:", err);
            } finally {
                setLoading(false);
            }
        };

        fetchAccounts();
    }, [user]);

    const availableAccountTypes = ALL_ACCOUNT_TYPES.filter(
        type => !existingAccounts.includes(type)
    );

    return {
        accountType,
        setAccountType,
        availableAccountTypes,
        loading
    };
};
