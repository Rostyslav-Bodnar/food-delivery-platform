import React, { useEffect } from "react";
import "../styles/BusinessHomePage.css";

import BusinessSidebar from "../../sidebars/BusinessSidebar.jsx";
import DishComponent from "./components/dish/DishComponent";
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
import useOnboardingRedirect from "./handlers/handleOnboardingRedirect.jsx";

export default function BusinessHomePage({ userData }) {

    const { dishes, loading, error } = useFetchDishes(userData);
    const { handleCreate, handleUpdate, handleDelete } =
        useDishMutations(dishes);

    const {
        q,
        setQ,
        category,
        setCategory,
        onlyPopular,
        setOnlyPopular,
        sortBy,
        setSortBy,
    } = useDishFilters();

    const { filtered } = useFilteredDishes(
        dishes,
        q,
        category,
        onlyPopular,
        sortBy
    );

    const { isModalOpen, setIsModalOpen, editing, openCreate, openEdit } =
        useDishModal();

    const { showDeleteConfirm, toDelete, setShowDeleteConfirm, setToDelete } =
        useDeleteConfirm();

    const {
        handleOnboardingRedirect,
        loadingOnboarding,
        error: onboardingError,
        retry,
        clearError,
    } = useOnboardingRedirect();

    const isNotOnboarded =
        !userData?.currentAccount?.stripeOnboardedAt;

    return (
        <div className="bh-page">
            <BusinessSidebar
                userData={userData}
                disabled={isNotOnboarded}
            />

            <main className="bh-main">
                {/* =========================
                    ONBOARDING BLOCK
                ========================= */}
                {isNotOnboarded && (
                    <div className="onboarding-box">
                        <h3>
                            Your business account is not fully set up.
                        </h3>
                        <p>
                            Please complete onboarding to access your
                            business dashboard.
                        </p>

                        <button
                            className="onboarding-button"
                            onClick={() =>
                                handleOnboardingRedirect(
                                    userData.currentAccount.id
                                )
                            }
                            disabled={loadingOnboarding}
                        >
                            {loadingOnboarding
                                ? "Redirecting..."
                                : "Complete Onboarding"}
                        </button>
                    </div>
                )}

                {/* =========================
                    MAIN APP
                ========================= */}
                {!isNotOnboarded && (
                    <>
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
                            setShowDeleteConfirm={
                                setShowDeleteConfirm
                            }
                        />

                        <BusinessFooter
                            filteredLength={filtered.length}
                            dishesLength={dishes.length}
                        />

                        <DishComponent
                            open={isModalOpen}
                            onClose={() =>
                                setIsModalOpen(false)
                            }
                            onCreate={handleCreate}
                            onUpdate={handleUpdate}
                            editing={editing}
                            userData={userData}
                        />

                        {showDeleteConfirm && toDelete && (
                            <DeleteConfirm
                                toDelete={toDelete}
                                setShowDeleteConfirm={
                                    setShowDeleteConfirm
                                }
                                handleDelete={handleDelete}
                            />
                        )}
                    </>
                )}
            </main>
        </div>
    );
}