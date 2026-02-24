// src/hooks/useDishFilters.js
import { useState } from "react";

const useDishFilters = () => {
    const [q, setQ] = useState("");
    const [category, setCategory] = useState("all");
    const [onlyPopular, setOnlyPopular] = useState(false);
    const [sortBy, setSortBy] = useState("name");

    return { q, setQ, category, setCategory, onlyPopular, setOnlyPopular, sortBy, setSortBy };
};

export default useDishFilters;