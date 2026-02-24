// src/hooks/useAccountSwitch.js
import { useUser } from "../../../context/UserContext";

const useAccountSwitch = () => {
    const { switchAccount, reloadUser } = useUser();

    const handleAccountSwitch = async (account) => {
        await switchAccount(account.id);
        await reloadUser();
    };

    return { handleAccountSwitch };
};

export default useAccountSwitch;