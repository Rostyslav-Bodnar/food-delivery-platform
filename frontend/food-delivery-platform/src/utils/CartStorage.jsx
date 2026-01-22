const CART_KEY = "cart";

export const getCart = () =>
    JSON.parse(localStorage.getItem(CART_KEY)) || [];

export const saveCart = (cart) =>
    localStorage.setItem(CART_KEY, JSON.stringify(cart));

export const addToCart = (dish, quantity) => {
    const cart = getCart();

    const existing = cart.find(i => i.id === dish.id);
    if (existing) {
        existing.quantity += quantity;
    } else {
        cart.push({
            id: dish.id,
            businessId: dish.businessId,
            name: dish.name,
            restaurant: dish.restaurant,
            price: dish.price,
            image: dish.image,
            quantity
        });
    }

    saveCart(cart);
};

export const updateCartItemQuantity = (id, quantity) => {
    saveCart(
        getCart().map(i => i.id === id ? { ...i, quantity } : i)
    );
};

export const removeCartItem = (id) => {
    saveCart(getCart().filter(i => i.id !== id));
};

export const clearCart = () => {
    localStorage.removeItem(CART_KEY);
};
