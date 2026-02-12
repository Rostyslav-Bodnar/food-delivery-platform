export const CategoryMap = {
    0: "Drink",
    1: "Soup",
    2: "Salad",
    3: "Pizza",
    4: "Burger",
    5: "Pasta",
    6: "Sushi",
    7: "Dessert",
    8: "Breakfast",
    9: "Grill",
    10: "SideDish",
    11: "Sauce",
    12: "Vegan",
    13: "KidsMenu",
    14: "SpecialOffer",
};

export const CategoryList = Object.entries(CategoryMap).map(([id, name]) => ({
    id: Number(id),
    name
}));
