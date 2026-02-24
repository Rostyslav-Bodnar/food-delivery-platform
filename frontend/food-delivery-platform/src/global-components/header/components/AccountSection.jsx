import { useNavigate } from "react-router-dom";
import { useDropdown } from "../hooks/useDropdown";
import { useAccountSwitcher } from "../hooks/useAccountSwitcher";
import { AccountBubbles } from "./AccountBubbles";
import { AccountDropdown } from "./AccountDropdown";

export function AccountSection({
                                   user,
                                   accounts,
                                   currentAccountId,
                                   switchAccount,
                                   logout
                               }) {
    const navigate = useNavigate();

    const { isOpen, setIsOpen, ref } = useDropdown(false);

    const { handleSelectAccount } = useAccountSwitcher(
        currentAccountId,
        switchAccount,
        () => setIsOpen(false)
    );

    return (
        <div className="account-bubbles-container" ref={ref}>
            <AccountBubbles
                accounts={accounts}
                currentAccountId={currentAccountId}
                onToggleDropdown={() =>
                    setIsOpen(prev => !prev)
                }
            />

            <AccountDropdown
                isOpen={isOpen}
                user={user}
                accounts={accounts}
                currentAccountId={currentAccountId}
                navigate={navigate}
                handleSelectAccount={handleSelectAccount}
                logout={logout}
            />
        </div>
    );
}