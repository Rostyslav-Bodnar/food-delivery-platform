export function useAccountSwitcher(
    currentAccountId,
    switchAccount,
    closeDropdown
) {
    const handleSelectAccount = async (account) => {
        if (account.id === currentAccountId) {
            closeDropdown();
            return;
        }

        await switchAccount(account.id);
        closeDropdown();
    };

    return { handleSelectAccount };
}