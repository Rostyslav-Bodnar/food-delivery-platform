import { useState } from "react";
import { Activity, ArrowRightLeft } from "lucide-react";
import RoleSidebar from "../sidebars/RoleSidebar.jsx";
import RangeToggles from "./components/RangeToggles.jsx";
import KpiGrid from "./components/KpiGrid.jsx";
import {
    DishRevenueChart,
    IncomeChart,
    OutcomeBreakdownChart
} from "./components/DashboardCharts.jsx";
import PayoutsTable from "./components/PayoutsTable.jsx";
import ManualPayoutButton from "./components/ManualPayoutButton.jsx";
import useBusinessDashboard from "./hooks/useBusinessDashboard.jsx";
import "./styles/BusinessDashboardPage.css";

export default function BusinessDashboardPage() {
    const businessId = localStorage.getItem("currentAccountId");
    const businessName = localStorage.getItem("currentAccountName") ?? "Your business";

    const [windowDays, setWindowDays] = useState(30);
    const { dashboard, dishRevenue, loading, error, refresh } = useBusinessDashboard(
        businessId,
        windowDays
    );

    const currency = dashboard?.currency ?? "USD";
    const availableBalance = dashboard?.balance?.available ?? 0;

    return (
        <div className="business-dashboard-shell">
            <RoleSidebar />

            <main className="business-dashboard-page">
                <section className="dashboard-hero">
                    <div>
                        <span className="dashboard-hero__eyebrow">
                            <Activity size={14} />
                            Business analytics
                        </span>
                        <h1>Income &amp; outcome</h1>
                        <p>
                            Live snapshot of money in and money out for{" "}
                            <strong>{businessName}</strong>, read straight from your
                            Stripe Connect account.
                        </p>
                    </div>

                    <RangeToggles value={windowDays} onChange={setWindowDays} />
                </section>

                {loading && !dashboard ? (
                    <DashboardSkeleton />
                ) : error ? (
                    <DashboardError />
                ) : dashboard ? (
                    <>
                        <KpiGrid
                            summary={dashboard.summary}
                            balance={dashboard.balance}
                            currency={currency}
                        />

                        <section className="dashboard-charts-grid">
                            <article className="dashboard-card dashboard-card--wide">
                                <header className="dashboard-card__header">
                                    <div>
                                        <span className="dashboard-card__eyebrow">Income over time</span>
                                        <h2>Gross vs. net by day</h2>
                                    </div>
                                </header>
                                <IncomeChart data={dashboard.timeSeries} currency={currency} />
                            </article>

                            <article className="dashboard-card">
                                <header className="dashboard-card__header">
                                    <div>
                                        <span className="dashboard-card__eyebrow">
                                            <ArrowRightLeft size={12} />
                                            Outcome
                                        </span>
                                        <h2>Where your gross goes</h2>
                                    </div>
                                </header>
                                <OutcomeBreakdownChart
                                    data={dashboard.outcomeBreakdown}
                                    currency={currency}
                                />
                            </article>
                        </section>

                        <article className="dashboard-card">
                            <header className="dashboard-card__header">
                                <div>
                                    <span className="dashboard-card__eyebrow">Top dishes</span>
                                    <h2>Revenue per dish</h2>
                                </div>
                                <span className="dashboard-card__sub">Delivered orders only</span>
                            </header>
                            <DishRevenueChart data={dishRevenue} currency={currency} />
                        </article>

                        <article className="dashboard-card">
                            <header className="dashboard-card__header">
                                <div>
                                    <span className="dashboard-card__eyebrow">Bank payouts</span>
                                    <h2>Recent settlements</h2>
                                </div>
                                <ManualPayoutButton
                                    businessId={businessId}
                                    available={availableBalance}
                                    currency={currency}
                                    onPayoutCreated={refresh}
                                />
                            </header>
                            <PayoutsTable payouts={dashboard.payouts} />
                        </article>
                    </>
                ) : null}
            </main>
        </div>
    );
}

const DashboardSkeleton = () => (
    <div className="dashboard-skeleton">
        <div className="dashboard-skeleton__row" />
        <div className="dashboard-skeleton__row" />
        <div className="dashboard-skeleton__row tall" />
    </div>
);

const DashboardError = () => (
    <div className="dashboard-error">
        <h3>Dashboard unavailable</h3>
        <p>
            We couldn't load your Stripe data. If you just finished onboarding,
            give it a minute and refresh — Stripe needs to provision your
            account before the first balance transactions show up.
        </p>
    </div>
);
