// src/pages/components/DeliveryPaymentGrid.jsx
import React from 'react';
import { Truck, Store, DollarSign, CreditCard } from 'lucide-react';
import "../styles/DeliveryPaymentGrid.css";

const DeliveryPaymentGrid = ({ settings, updateSettingsFor }) => {
    return (
        <div className="delivery-payment-grid">
            <div>
                <h3><Truck size={22} /> Delivery method</h3>
                <div className="options-group">
                    <label className={settings.deliveryType === 'delivery' ? 'active' : ''}>
                        <input
                            type="radio"
                            checked={settings.deliveryType === 'delivery'}
                            onChange={() => updateSettingsFor({ deliveryType: 'delivery' })}
                        />
                        <Truck size={22} /> Delivery
                    </label>
                    <label className={settings.deliveryType === 'pickup' ? 'active' : ''}>
                        <input
                            type="radio"
                            checked={settings.deliveryType === 'pickup'}
                            onChange={() => updateSettingsFor({ deliveryType: 'pickup' })}
                        />
                        <Store size={22} /> Pickup
                    </label>
                </div>
            </div>

            <div>
                <h3><CreditCard size={22} /> Payment method</h3>
                <div className="options-group">
                    <label className={settings.paymentType === 'cash' ? 'active' : ''}>
                        <input
                            type="radio"
                            checked={settings.paymentType === 'cash'}
                            onChange={() => updateSettingsFor({ paymentType: 'cash' })}
                        />
                        <DollarSign size={22} /> Cash on delivery
                    </label>
                    <label className={settings.paymentType === 'card' ? 'active' : ''}>
                        <input
                            type="radio"
                            checked={settings.paymentType === 'card'}
                            onChange={() => updateSettingsFor({ paymentType: 'card' })}
                        />
                        <CreditCard size={22} /> Online card payment
                    </label>
                </div>
            </div>
        </div>
    );
};

export default DeliveryPaymentGrid;