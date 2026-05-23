import { CalendarDays } from "lucide-react";

const OPTIONS = [
    { id: 7, label: "7 days" },
    { id: 30, label: "30 days" },
    { id: 90, label: "90 days" },
    { id: 365, label: "1 year" }
];

export default function RangeToggles({ value, onChange }) {
    return (
        <div className="range-toggles">
            <span className="range-toggles__icon">
                <CalendarDays size={16} />
            </span>
            {OPTIONS.map(opt => (
                <button
                    key={opt.id}
                    type="button"
                    className={`range-toggle ${value === opt.id ? "is-active" : ""}`}
                    onClick={() => onChange(opt.id)}
                >
                    {opt.label}
                </button>
            ))}
        </div>
    );
}
