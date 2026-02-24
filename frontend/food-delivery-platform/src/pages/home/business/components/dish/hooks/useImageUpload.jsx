import { useRef } from "react";

export function useImageUpload(setForm) {
    const dropRef = useRef(null);
    const prevObjectUrlRef = useRef(null);

    const handleFiles = (file) => {
        if (!file) return;

        if (prevObjectUrlRef.current)
            URL.revokeObjectURL(prevObjectUrlRef.current);

        const url = URL.createObjectURL(file);
        prevObjectUrlRef.current = url;

        setForm(prev => ({
            ...prev,
            imageFile: file,
            imagePreview: url
        }));
    };

    const onDrop = (e) => {
        e.preventDefault();
        const f = e.dataTransfer?.files?.[0];
        if (f?.type.startsWith("image/")) handleFiles(f);
        dropRef.current?.classList.remove("dragover");
    };

    const onDragOver = (e) => {
        e.preventDefault();
        dropRef.current?.classList.add("dragover");
    };

    const onDragLeave = () =>
        dropRef.current?.classList.remove("dragover");

    const onSelectFile = (e) => {
        const f = e.target.files?.[0];
        if (f?.type.startsWith("image/")) handleFiles(f);
    };

    return {
        dropRef,
        onDrop,
        onDragOver,
        onDragLeave,
        onSelectFile
    };
}