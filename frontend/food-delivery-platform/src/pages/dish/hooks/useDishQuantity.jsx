import { useState } from "react";

export const useDishQuantity = (initial = 1) => {
    const [quantity, setQuantity] = useState(initial);

    const increase = () => setQuantity(q => q + 1);
    const decrease = () => setQuantity(q => (q > 1 ? q - 1 : 1));

    return {
        quantity,
        setQuantity,
        increase,
        decrease
    };
};
