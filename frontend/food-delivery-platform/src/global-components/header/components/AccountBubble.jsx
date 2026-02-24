import { motion } from "framer-motion";

export function AccountBubble({
                                  account,
                                  isActive,
                                  onClick,
                                  position
                              }) {
    return (
        <motion.div
            className={`account-bubble ${isActive ? "active" : ""}`}
            title={account.accountType}
            onClick={onClick}
            animate={{
                x: position?.x ?? 0,
                y: position?.y ?? 0,
                scale: 1,
                zIndex: position?.zIndex ?? 3,
                opacity: isActive ? 1 : 0.7
            }}
            transition={{ type: "spring", stiffness: 200, damping: 20 }}
        >
            {account.imageUrl ? (
                <motion.img
                    src={account.imageUrl}
                    alt={account.accountType}
                    whileHover={{ scale: 1.1 }}
                />
            ) : (
                <span>
                    {account.name
                        ? account.name[0].toUpperCase()
                        : "U"}
                </span>
            )}
        </motion.div>
    );
}