// src/pages/BusinessHomePage.jsx
import React from "react";
import "../styles/BusinessHomePage.css";
import BusinessSidebar from "../../sidebars/BusinessSidebar.jsx";
import DishComponent from "./components/DishComponent";
import BusinessHeader from "./components/BusinessHeader.jsx";
import DishesContent from "./components/DishesContent.jsx";
import BusinessFooter from "./components/BusinessFooter.jsx";
import DeleteConfirm from "./components/DeleteConfirm.jsx";
import useFetchDishes from "./hooks/useFetchDishes";
import useDishMutations from "./hooks/useDishMutations";
import useDishFilters from "./hooks/useDishFilters";
import useFilteredDishes from "./hooks/useFilteredDishes";
import useDishModal from "./hooks/useDishModal";
import useDeleteConfirm from "./hooks/useDeleteConfirm";

export default function BusinessHomePage({ userData }) {
    const { dishes, loading, error } = useFetchDishes(userData);
    const { handleCreate, handleUpdate, handleDelete } = useDishMutations(dishes);
    const { q, setQ, category, setCategory, onlyPopular, setOnlyPopular, sortBy, setSortBy } = useDishFilters();
    const { filtered } = useFilteredDishes(dishes, q, category, onlyPopular, sortBy);
    const { isModalOpen, setIsModalOpen, editing, openCreate, openEdit } = useDishModal();
    const { showDeleteConfirm, toDelete, setShowDeleteConfirm, setToDelete } = useDeleteConfirm();

    return (
        <div className="bh-page">
            <BusinessSidebar userData={userData} />
            {/* MAIN */}
            <main className="bh-main">
                <BusinessHeader
                    q={q}
                    setQ={setQ}
                    category={category}
                    setCategory={setCategory}
                    sortBy={sortBy}
                    setSortBy={setSortBy}
                    onlyPopular={onlyPopular}
                    setOnlyPopular={setOnlyPopular}
                    openCreate={openCreate}
                />
                <DishesContent
                    loading={loading}
                    error={error}
                    filtered={filtered}
                    openEdit={openEdit}
                    setToDelete={setToDelete}
                    setShowDeleteConfirm={setShowDeleteConfirm}
                />
                <BusinessFooter
                    filteredLength={filtered.length}
                    dishesLength={dishes.length}
                />
            </main>
            {/* MODAL */}
            <DishComponent
                open={isModalOpen}
                onClose={() => setIsModalOpen(false)}
                onCreate={handleCreate}
                onUpdate={handleUpdate}
                editing={editing}
                userData={userData}
            />
            {/* DELETE CONFIRM */}
            {showDeleteConfirm && toDelete && (
                <DeleteConfirm
                    toDelete={toDelete}
                    setShowDeleteConfirm={setShowDeleteConfirm}
                    handleDelete={handleDelete}
                />
            )}
        </div>
    );
}