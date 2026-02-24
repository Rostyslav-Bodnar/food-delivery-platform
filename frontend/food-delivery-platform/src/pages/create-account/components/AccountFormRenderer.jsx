import CustomerForm from "./CustomerForm";
import BusinessForm from "./BusinessForm";
import CourierForm from "./CourierForm";

const AccountFormRenderer = ({ accountType }) => {
    switch (accountType) {
        case "Customer":
            return <CustomerForm />;
        case "Business":
            return <BusinessForm />;
        case "Courier":
            return <CourierForm />;
        default:
            return (
                <p className="no-accounts-text">
                    You already have all available account types 🎉
                </p>
            );
    }
};

export default AccountFormRenderer;
