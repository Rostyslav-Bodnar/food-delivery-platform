import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { getProfile } from '../api/User.jsx';
import './styles/HomePage.css';

// Підкомпоненти
import UnauthenticatedHome from '../components/UnauthenticatedHome';
import CustomerHomePage from '../components/CustomerHomePage.jsx';
// (за потреби згодом можна буде додати інші типи, наприклад BusinessHomePage, CourierHomePage)

const HomePage = () => {
    const [isAuthenticated, setIsAuthenticated] = useState(false);
    const [accountType, setAccountType] = useState(null);
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
                const userData = await getProfile(); // ✅ Отримуємо UserDto
                setIsAuthenticated(true);
                setAccountType(userData.currentAccount?.accountType); // "customer", "business", "courier"
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

    // ✅ Відображення відповідної домашньої сторінки
    switch (accountType?.toLowerCase()) {
        case 'customer':
            return <CustomerHomePage />;
        // case 'business':
        //     return <BusinessHomePage />;
        // case 'courier':
        //     return <CourierHomePage />;
        default:
            return <UnauthenticatedHome />;
    }
};

export default HomePage;
