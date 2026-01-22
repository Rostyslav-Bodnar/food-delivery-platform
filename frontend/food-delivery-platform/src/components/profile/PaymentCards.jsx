import React from "react";
import "./styles/PaymentCards.css";

const PaymentCards = ({
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
                      }) => {
    return (
        <div className="payment-cards">
            <h3 className="payment-title">Bank cards</h3>

            {cardSuccess && (
                <div className="card-success">
                    {cardSuccess}
                </div>
            )}

            {paymentCards.map(card => (
                <div
                    key={card.id}
                    className={`payment-card ${activeCardId === card.id ? "active" : ""}`}
                    onClick={() => selectCard(card.id)}
                >
                    <p className="card-number">
                        •••• •••• •••• {card.last4}
                        <button
                            className="card-edit-btn"
                            onClick={(e) => {
                                e.stopPropagation();
                                startEditingCard(card);
                            }}
                        >
                            Edit
                        </button>
                        <button
                            className="card-delete-btn"
                            onClick={(e) => {
                                e.stopPropagation();
                                deleteCard(card.id);
                            }}
                        >
                            🗑Delete
                        </button>
                    </p>
                    <p className="card-meta">Expiry date: {card.expiryDate}</p>
                    <p className="card-meta">Owner: {card.cardHolder}</p>
                </div>
            ))}

            {editingCardId && (
                <div className="card-form-wrapper">
                    <form onSubmit={handleCardSubmit}>
                        <input
                            type="text"
                            name="cardHolder"
                            placeholder="Owner's name"
                            value={cardForm.cardHolder}
                            onChange={handleCardInputChange}
                            required
                            className="card-input"
                        />

                        {editingCardId === "new" && (
                            <input
                                type="text"
                                name="cardNumber"
                                placeholder="Card number"
                                value={cardForm.cardNumber}
                                onChange={handleCardInputChange}
                                required
                                maxLength="19"
                                className="card-input"
                            />
                        )}

                        <div className="card-row">
                            <input
                                type="text"
                                name="expiryDate"
                                placeholder="MMYY"
                                value={cardForm.expiryDate}
                                onChange={handleCardInputChange}
                                required
                                maxLength="4"
                                className="card-input"
                            />
                            <input
                                type="password"
                                name="cvv"
                                placeholder="CVV"
                                value={cardForm.cvv}
                                onChange={handleCardInputChange}
                                required={editingCardId === "new"}
                                maxLength="4"
                                className="card-input cvv"
                                autoComplete="off"
                            />
                        </div>

                        <button type="submit" className="card-submit">
                            {editingCardId === "new" ? "Add card" : "Save card"}
                        </button>

                        <button
                            type="button"
                            onClick={cancelCardEdit}
                            className="card-cancel"
                        >
                            Cancel
                        </button>
                    </form>
                </div>
            )}

            {!editingCardId && (
                <button className="add-card-btn" onClick={startAddingCard}>
                    Add Card
                </button>
            )}
        </div>
    );
};

export default PaymentCards;
