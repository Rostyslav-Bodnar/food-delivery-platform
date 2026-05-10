import { CategoryList } from "../../../../../../constants/category";
import { ImageDropzone } from "./ImageDropzone";

export function DishPageOne({
                                form,
                                change,
                                setPage,
                                onClose,
                                imageUpload
                            }) {
    return (
        <>
            <div className="modal-row">
                <label>Name</label>
                <input
                    value={form.name}
                    onChange={e => change("name", e.target.value)}
                />
            </div>

            <div className="modal-row">
                <label>Description</label>
                <textarea
                    value={form.description}
                    onChange={e => change("description", e.target.value)}
                />
            </div>

            <select
                className="bh-select"
                value={form.category}
                onChange={e => change("category", Number(e.target.value))}
            >
                {CategoryList.map(cat => (
                    <option key={cat.id} value={cat.id}>
                        {cat.name}
                    </option>
                ))}
            </select>

            <ImageDropzone
                imagePreview={form.imagePreview}
                {...imageUpload}
            />

            <div className="modal-row row-actions">
                <button className="btn ghost" onClick={onClose}>
                    Cancel
                </button>
                <button
                    className="btn primary"
                    onClick={() => setPage(2)}
                    disabled={!form.name.trim()}
                >
                    Next →
                </button>
            </div>
        </>
    );
}