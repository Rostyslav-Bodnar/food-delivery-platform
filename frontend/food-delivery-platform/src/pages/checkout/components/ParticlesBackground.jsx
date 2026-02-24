// src/pages/components/ParticlesBackground.jsx
import React from 'react';
import { motion } from 'framer-motion';
import "../styles/ParticlesBackground.css";

const ParticlesBackground = () => {
    return (
        <div className="particles">
            {[...Array(6)].map((_, i) => (
                <motion.div
                    key={i}
                    className="particle"
                    initial={{ y: -100, x: Math.random() * window.innerWidth }}
                    animate={{ y: window.innerHeight + 100 }}
                    transition={{ duration: 15 + Math.random() * 10, repeat: Infinity, ease: "linear", delay: Math.random() * 5 }}
                />
            ))}
        </div>
    );
};

export default ParticlesBackground;