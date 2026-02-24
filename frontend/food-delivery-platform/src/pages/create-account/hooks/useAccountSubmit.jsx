import { useNavigate } from "react-router-dom";
import { useUser } from "../../../context/UserContext";
import { createAccount } from "../../../api/Account.jsx";

export const useAccountSubmit = ({
                                     endpoint,
                                     buildAccountPayload,
                                     formData
                                 }) => {
    const navigate = useNavigate();
    const { reloadUser } = useUser();

    const handleSubmit = async (e) => {
        e.preventDefault();

        const payload = buildAccountPayload(formData);

        try {
            await createAccount(endpoint, payload);
            await reloadUser();
            navigate("/profile");
        } catch (err) {
            console.error(err);
            alert("Failed to create account");
        }
    };

    return { handleSubmit };
};
