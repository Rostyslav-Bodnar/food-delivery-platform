const AccountTypeTabs = ({
                             availableAccountTypes,
                             accountType,
                             setAccountType
                         }) => {
    return (
        <div className="tabs" role="tablist" aria-label="Account types">
            {availableAccountTypes.map(type => (
                <button
                    key={type}
                    role="tab"
                    aria-selected={accountType === type}
                    className={`tab ${accountType === type ? "active" : ""}`}
                    onClick={() => setAccountType(type)}
                >
                    {type === "Customer" && "👤 Customer"}
                    {type === "Business" && "🏪 Business"}
                    {type === "Courier" && "🚚 Courier"}
                </button>
            ))}
        </div>
    );
};

export default AccountTypeTabs;
