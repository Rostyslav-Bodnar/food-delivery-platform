export const usePopularScroll = () => {
    const scrollPopular = (direction) => {
        const container = document.querySelector(".popular-scroll");
        if (!container) return;

        const scrollAmount = 340;

        container.scrollBy({
            left: direction === "left" ? -scrollAmount : scrollAmount,
            behavior: "smooth"
        });
    };

    return { scrollPopular };
};