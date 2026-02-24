import "./styles/CreateAccountPage.css";
import { useUser } from "../../context/UserContext";
import { useAccountTypes } from "./hooks/useAccountTypes";
import AccountTypeTabs from "./components/AccountTypeTabs";
import AccountFormRenderer from "./components/AccountFormRenderer";

const CreateAccountPage = () => {
    const { user } = useUser();

    const {
        accountType,
        setAccountType,
        availableAccountTypes,
        loading
    } = useAccountTypes(user);

    if (loading) return <div>Loading...</div>;

    return (
        <div className="create-page-wrapper">
            <div className="create-container">
                <aside className="create-sidebar">
                    <div className="create-brand">
                        <div className="create-logo">FE</div>
                        <div>
                            <div className="create-title">Create account</div>
                            <div className="create-sub">
                                Select account type and fill the form
                            </div>
                        </div>
                    </div>

                    <AccountTypeTabs
                        availableAccountTypes={availableAccountTypes}
                        accountType={accountType}
                        setAccountType={setAccountType}
                    />

                    <div className="create-footer">
                        <div>Need help? Contact support.</div>
                    </div>
                </aside>

                <section className="form-area">
                    <h2>
                        {accountType
                            ? `${accountType} — registration`
                            : "Select account type"}
                    </h2>

                    <div className="form-wrapper fade-in">
                        <AccountFormRenderer accountType={accountType} />
                    </div>
                </section>
            </div>
        </div>
    );
};

export default CreateAccountPage;
