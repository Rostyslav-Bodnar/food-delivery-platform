import React from "react";
import { FileText } from "lucide-react";

export default function NotesSection({ notes }) {
    if (!notes) return null;

    return (
        <section className="od-section">
            <h4><FileText size={16} /> Customer's notes</h4>

            <div className="od-card">
                <p>{notes}</p>
            </div>
        </section>
    );
}