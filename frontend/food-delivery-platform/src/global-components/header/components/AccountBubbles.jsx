import { AccountBubble } from "./AccountBubble";

export function AccountBubbles({
                                   accounts,
                                   currentAccountId,
                                   onToggleDropdown
                               }) {
    const activeAccount =
        accounts.find(acc => acc.id === currentAccountId) || {};

    const otherAccounts =
        accounts.filter(acc => acc.id !== currentAccountId);

    const total = accounts.length;

    const getPosition = (index) => {
        if (total === 1) return { x: 0, y: 0, zIndex: 3 };
        if (total === 2)
            return {
                x: index === 0 ? -20 : 0,
                y: 0,
                zIndex: index === 0 ? 1 : 3
            };

        if (index === 0) return { x: -20, y: 0, zIndex: 1 };
        if (index === 1) return { x: 20, y: 0, zIndex: 1 };

        return { x: 0, y: 0, zIndex: 3 };
    };

    return (
        <div className="account-bubbles">
            <AccountBubble
                account={activeAccount}
                isActive
                onClick={onToggleDropdown}
            />

            {otherAccounts.map((acc, index) => (
                <AccountBubble
                    key={acc.id}
                    account={acc}
                    position={getPosition(index)}
                    onClick={onToggleDropdown}
                />
            ))}
        </div>
    );
}