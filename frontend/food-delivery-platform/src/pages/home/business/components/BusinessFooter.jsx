// src/pages/components/BusinessFooter.jsx
import React from "react";

const BusinessFooter = ({ filteredLength, dishesLength }) => {
    return (
        <footer className="bh-footer">
            <div>Показано: {filteredLength} з {dishesLength}</div>
        </footer>
    );
};

export default BusinessFooter;