// src/hooks/useUserLocation.js
import { useState, useEffect } from 'react';

const useUserLocation = () => {
    const [userCity, setUserCity] = useState('Kyiv');
    const [userAddress, setUserAddress] = useState('Khreshchatyk, 22');

    useEffect(() => {
        fetch('https://ipapi.co/json/')
            .then(res => res.json())
            .then(data => {
                if (data.city) {
                    setUserCity(data.city);
                    const addresses = {
                        'Kyiv': 'Khreshchatyk, 22',
                        'Lviv': 'Svobody Ave, 7',
                        'Odesa': 'Derybasivska, 10',
                        'Kharkiv': 'Sumska St, 35',
                        'Dnipro': 'Dmytra Yavornytskoho Ave, 50',
                    };
                    setUserAddress(addresses[data.city] || 'city center');
                }
            })
            .catch(() => {
                setUserCity('Kyiv');
                setUserAddress('Khreshchatyk, 22');
            });
    }, []);

    return { userCity, userAddress };
};

export default useUserLocation;
