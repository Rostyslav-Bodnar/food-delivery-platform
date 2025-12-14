import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { getProfile } from '../api/User.jsx';
import './styles/HomePage.css';

// Підкомпоненти
import UnauthenticatedHome from '../components/UnauthenticatedHome';
import CustomerHomePage from '../components/CustomerHomePage.jsx';
import BusinessHomePage from "../components/BusinessHomePage.jsx";
import CourierHomePage from "../components/curier/CourierHomePage.jsx";

const HomePage = () => {
    const [isAuthenticated, setIsAuthenticated] = useState(false);
    const [accountType, setAccountType] = useState(null);
    const [userData, setUserData] = useState(null);
    const [loading, setLoading] = useState(true);
    const navigate = useNavigate();

    useEffect(() => {
        const checkAuth = async () => {
            const token = localStorage.getItem('accessToken');
            if (!token) {
                setIsAuthenticated(false);
                setLoading(false);
                return;
            }

            try {
                const userProfile = await getProfile();
                setIsAuthenticated(true);
                setUserData(userProfile);
                setAccountType(userProfile.currentAccount?.accountType);
                console.log(userProfile);
            } catch (error) {
                console.log('Token invalid or expired, logging out...');
                localStorage.removeItem('accessToken');
                localStorage.removeItem('accessTokenExpiresAt');
                setIsAuthenticated(false);
            } finally {
                setLoading(false);
            }
        };

        checkAuth();
    }, []);

    if (loading) {
        return (
            <div
                className="page-wrapper"
                style={{
                    display: 'flex',
                    justifyContent: 'center',
                    alignItems: 'center',
                    height: '100vh'
                }}
            >
                <div className="loading-spinner">Loading...</div>
            </div>
        );
    }

    if (!isAuthenticated) {
        return <UnauthenticatedHome />;
    }

    switch (accountType?.toLowerCase()) {
        case 'customer':
            return <CustomerHomePage /*currentAccountId={currentAccountId}*/ />;
        case 'business':
            return <BusinessHomePage userData={userData} />;
        case 'courier':
             return <CourierHomePage />;
        default:
            return <UnauthenticatedHome />;
    }
};

export default HomePage;
