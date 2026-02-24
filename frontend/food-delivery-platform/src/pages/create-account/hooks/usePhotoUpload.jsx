import { useEffect, useRef } from "react";

export default function usePhotoUpload(setFormData) {
    const dropRef = useRef(null);
    const prevUrlRef = useRef(null);

    useEffect(() => {
        return () => {
            if (prevUrlRef.current) {
                URL.revokeObjectURL(prevUrlRef.current);
                prevUrlRef.current = null;
            }
        };
    }, []);

    const handleFileSelect = (file) => {
        if (!file || !file.type.startsWith("image/")) return;

        if (prevUrlRef.current) {
            URL.revokeObjectURL(prevUrlRef.current);
            prevUrlRef.current = null;
        }

        const url = URL.createObjectURL(file);
        prevUrlRef.current = url;

        setFormData(prev => ({
            ...prev,
            photoFile: file,
            photoPreview: url
        }));
    };

    const removePhoto = (e) => {
        e?.preventDefault();
        if (prevUrlRef.current) {
            URL.revokeObjectURL(prevUrlRef.current);
            prevUrlRef.current = null;
        }
        setFormData(prev => ({
            ...prev,
            photoFile: null,
            photoPreview: ""
        }));
    };

    const onDrop = (e) => {
        e.preventDefault();
        const file = e.dataTransfer?.files?.[0];
        handleFileSelect(file);
        dropRef.current?.classList?.remove("dragover");
    };

    const onDragOver = (e) => {
        e.preventDefault();
        dropRef.current?.classList?.add("dragover");
    };

    const onDragLeave = () => {
        dropRef.current?.classList?.remove("dragover");
    };

    const onFileChange = (e) => {
        handleFileSelect(e.target.files?.[0]);
    };

    return {
        dropRef,
        onDrop,
        onDragOver,
        onDragLeave,
        onFileChange,
        removePhoto
    };
}
