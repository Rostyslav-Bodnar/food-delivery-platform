// src/hooks/useUserLocation.js
import { useState, useEffect } from 'react';

const useUserLocation = () => {
    const [userCity, setUserCity] = useState('Київ');
    const [userAddress, setUserAddress] = useState('Хрещатик, 22');

    useEffect(() => {
        fetch('https://ipapi.co/json/')
            .then(res => res.json())
            .then(data => {
                if (data.city) {
                    setUserCity(data.city);
                    const addresses = {
                        'Київ': 'Хрещатик, 22',
                        'Львів': 'просп. Свободи, 7',
                        'Одеса': 'Дерибасівська, 10',
                        'Харків': 'вул. Сумська, 35',
                        'Дніпро': 'просп. Дмитра Яворницького, 50',
                    };
                    setUserAddress(addresses[data.city] || 'центр міста');
                }
            })
            .catch(() => {
                setUserCity('Київ');
                setUserAddress('Хрещатик, 22');
            });
    }, []);

    return { userCity, userAddress };
};

export default useUserLocation;