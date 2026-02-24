export const mapDishToViewModel = (data, reviews) => {
    return {
        id: data.id,
        name: data.name,
        businessId: data.businessDetails.id,
        image: data.imageUrl,
        price: data.price,
        cookingTime: data.cookingTime ? `${data.cookingTime} хв` : "",
        description: data.description ?? "Опис відсутній",
        category: data.categoryName ?? "Страва",
        weight: "300 г",
        rating: 4.5,
        reviewsCount: reviews?.length ?? 0,
        restaurant: data.businessDetails.name,
        popular: true,
        ingredients: data.ingredients?.map(i => i.name) ?? [],
        nutrition: { calories: 400, protein: 20, fats: 10, carbs: 60 },
        allergens: ["Глютен"]
    };
};
