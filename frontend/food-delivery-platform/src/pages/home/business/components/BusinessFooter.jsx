// src/pages/components/BusinessFooter.jsx
import React from "react";

const BusinessFooter = ({ filteredLength, dishesLength }) => {
    return (
        <footer className="bh-footer">
            <div>Showing: {filteredLength} of {dishesLength}</div>
        </footer>
    );
};

export default BusinessFooter;