// src/hooks/usePaymentCards.js
import { useEffect, useState } from "react";
import { useUser } from "../../../context/UserContext";

const usePaymentCards = () => {
    const { user, accounts, currentAccountId } = useUser();

    const [paymentCards, setPaymentCards] = useState([]);
    const [activeCardId, setActiveCardId] = useState(null);
    const [editingCardId, setEditingCardId] = useState(null);
    const [cardForm, setCardForm] = useState({
        cardNumber: "",
        expiryDate: "",
        cvv: "",
        cardHolder: ""
    });
    const [cardSuccess, setCardSuccess] = useState(null);

    useEffect(() => {
        if (user && accounts && currentAccountId) {
            const currentAccount = accounts.find(a => a.id === currentAccountId);
            if (currentAccount) {
                if (paymentCards.length === 0 && Array.isArray(currentAccount.paymentCards)) {
                    const cards = currentAccount.paymentCards.map((card, index) => ({
                        id: card.id || `card-${index}`,
                        last4: card.last4 || "0000",
                        expiryDate: card.expiryDate || "MM/YY",
                        cardHolder: card.cardHolder || "CARD HOLDER",
                    }));
                    setPaymentCards(cards);
                    if (cards.length > 0) {
                        setActiveCardId(cards[0].id);
                    }
                }
            }
        }
    }, [user, accounts, currentAccountId, paymentCards.length]);

    const formatCardNumber = (value) =>
        value.replace(/\D/g, "").match(/.{1,4}/g)?.join(" ") || "";

    const handleCardInputChange = (e) => {
        const { name, value } = e.target;
        let formatted = value;
        if (name === "cardNumber") formatted = formatCardNumber(value);
        if (name === "expiryDate") formatted = value.replace(/\D/g, "").slice(0, 4);
        if (name === "cvv") formatted = value.replace(/\D/g, "").slice(0, 4);
        if (name === "cardHolder") formatted = value.toUpperCase();
        setCardForm(prev => ({ ...prev, [name]: formatted }));
    };

    const startAddingCard = () => {
        setEditingCardId("new");
        setCardForm({ cardNumber: "", expiryDate: "", cvv: "", cardHolder: "" });
    };

    const startEditingCard = (card) => {
        setEditingCardId(card.id);
        setCardForm({
            cardNumber: "",
            expiryDate: card.expiryDate.replace("/", ""),
            cvv: "",
            cardHolder: card.cardHolder
        });
    };

    const deleteCard = (id) => {
        setPaymentCards(prev => prev.filter(c => c.id !== id));
        if (activeCardId === id) {
            const remaining = paymentCards.filter(c => c.id !== id);
            setActiveCardId(remaining[0]?.id || null);
        }
        setCardSuccess("Card was deleted");
        setTimeout(() => setCardSuccess(null), 3000);
    };

    const handleCardSubmit = (e) => {
        e.preventDefault();
        const cleanNumber = cardForm.cardNumber.replace(/\s/g, "");
        if (editingCardId === "new" && cleanNumber.length !== 16) return;

        const formattedExpiry = cardForm.expiryDate.length === 4
            ? `${cardForm.expiryDate.slice(0, 2)}/${cardForm.expiryDate.slice(2)}`
            : "MM/YY";

        const cardData = {
            last4: cleanNumber.slice(-4) || paymentCards.find(c => c.id === editingCardId)?.last4 || "0000",
            expiryDate: formattedExpiry,
            cardHolder: cardForm.cardHolder.trim() || "CARD HOLDER",
        };

        if (editingCardId === "new") {
            const newCard = { id: Date.now().toString(), ...cardData };
            setPaymentCards(prev => [...prev, newCard]);
            setActiveCardId(newCard.id);
            setCardSuccess("Card was added!");
        } else {
            setPaymentCards(prev => prev.map(c => c.id === editingCardId ? { ...c, ...cardData } : c));
            setCardSuccess("Card was updated!");
        }

        setEditingCardId(null);
        setCardForm({ cardNumber: "", expiryDate: "", cvv: "", cardHolder: "" });
        setTimeout(() => setCardSuccess(null), 3000);
    };

    const cancelCardEdit = () => {
        setEditingCardId(null);
        setCardForm({ cardNumber: "", expiryDate: "", cvv: "", cardHolder: "" });
    };

    const selectCard = (id) => setActiveCardId(id);

    return {
        paymentCards,
        activeCardId,
        editingCardId,
        cardForm,
        cardSuccess,
        selectCard,
        startEditingCard,
        deleteCard,
        handleCardInputChange,
        handleCardSubmit,
        cancelCardEdit,
        startAddingCard,
    };
};

export default usePaymentCards;