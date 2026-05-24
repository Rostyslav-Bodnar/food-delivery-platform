import React from "react";
import { Link } from "react-router-dom";
import {
    ArrowRight,
    Bike,
    ChefHat,
    CreditCard,
    MapPin,
    ShoppingBag,
    Sparkles,
    Utensils
} from "lucide-react";
import "../styles/HomePage.css";

const UnauthenticatedHome = () => {
    return (
        <div className="unauth-home">

            {/* ------- HERO ------- */}
            <section className="unauth-hero">
                <span className="unauth-hero__eyebrow">
                    <Sparkles size={14} />
                    Food delivery, reimagined
                </span>

                <h1 className="unauth-hero__title">
                    Your favorite meals,<br />
                    delivered <span className="unauth-hero__title-accent">in minutes</span>
                </h1>

                <p className="unauth-hero__lead">
                    Order from top local restaurants and follow every step on the
                    live map. One platform for diners, restaurants, and couriers.
                </p>

                <div className="unauth-hero__cta">
                    <Link to="/auth" className="unauth-cta unauth-cta--primary">
                        Get started
                        <ArrowRight size={18} />
                    </Link>
                    <Link to="/auth" className="unauth-cta unauth-cta--ghost">
                        I already have an account
                    </Link>
                </div>

                {/* Decorative orbiting glow blobs — pure CSS */}
                <div className="unauth-hero__orb unauth-hero__orb--a" aria-hidden="true" />
                <div className="unauth-hero__orb unauth-hero__orb--b" aria-hidden="true" />
            </section>

            {/* ------- FEATURE CARDS ------- */}
            <section className="unauth-features">
                <article className="unauth-feature">
                    <div className="unauth-feature__icon">
                        <MapPin size={22} strokeWidth={2.2} />
                    </div>
                    <h3>Live tracking</h3>
                    <p>
                        Watch your courier approach in real time. No more guessing
                        where your food is.
                    </p>
                </article>

                <article className="unauth-feature">
                    <div className="unauth-feature__icon">
                        <CreditCard size={22} strokeWidth={2.2} />
                    </div>
                    <h3>Secure payments</h3>
                    <p>
                        Pay with card or cash on delivery. Card payments run through
                        Stripe — 3D Secure included.
                    </p>
                </article>

                <article className="unauth-feature">
                    <div className="unauth-feature__icon">
                        <ShoppingBag size={22} strokeWidth={2.2} />
                    </div>
                    <h3>Smart cart</h3>
                    <p>
                        Mix dishes from multiple restaurants and pay for each one
                        separately, in a single checkout.
                    </p>
                </article>
            </section>

            {/* ------- ROLE PITCH ------- */}
            <section className="unauth-roles">
                <div className="unauth-roles__heading">
                    <span className="unauth-roles__eyebrow">More than just delivery</span>
                    <h2>One platform, three roles</h2>
                    <p>
                        Whether you're hungry, running a restaurant, or driving
                        across town — there's an account type made for you.
                    </p>
                </div>

                <div className="unauth-roles__grid">
                    <article className="unauth-role">
                        <div className="unauth-role__icon"><Utensils size={20} /></div>
                        <h3>Customer</h3>
                        <p>Browse, order, track. Save addresses and payment cards for one-tap checkout.</p>
                    </article>

                    <article className="unauth-role">
                        <div className="unauth-role__icon"><ChefHat size={20} /></div>
                        <h3>Restaurant</h3>
                        <p>Manage your menu, accept orders, and watch revenue land directly in your bank — onboarded by Stripe Connect.</p>
                    </article>

                    <article className="unauth-role">
                        <div className="unauth-role__icon"><Bike size={20} /></div>
                        <h3>Courier</h3>
                        <p>Accept deliveries nearby, follow turn-by-turn routes, and get paid weekly to your account.</p>
                    </article>
                </div>

                <div className="unauth-roles__footer">
                    <Link to="/auth" className="unauth-cta unauth-cta--primary">
                        Join now
                        <ArrowRight size={18} />
                    </Link>
                </div>
            </section>
        </div>
    );
};

export default UnauthenticatedHome;
