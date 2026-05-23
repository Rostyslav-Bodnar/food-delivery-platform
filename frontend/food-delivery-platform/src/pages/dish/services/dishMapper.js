export const mapDishToViewModel = (data, reviews) => {
    return {
        id: data.id,
        name: data.name,
        businessId: data.businessDetails.id,
        image: data.imageUrl,
        price: data.price,
        cookingTime: data.cookingTime ? `${data.cookingTime} min` : "",
        description: data.description ?? "No description",
        category: data.categoryName ?? "Dish",
        weight: "300 g",
        rating: 4.5,
        reviewsCount: reviews?.length ?? 0,
        restaurant: data.businessDetails.name,
        popular: true,
        ingredients: data.ingredients?.map(i => i.name) ?? [],
        nutrition: { calories: 400, protein: 20, fats: 10, carbs: 60 },
        allergens: ["Gluten"]
    };
};
