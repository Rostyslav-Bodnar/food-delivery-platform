import usePhotoUpload from "../hooks/usePhotoUpload"
import { useAccountFormState } from "../hooks/useAccountFormState"
import { useAccountSubmit } from "../hooks/useAccountSubmit"
import { useToast } from "../../../global-components/toast/ToastContext";
import "../styles/AccountForm.css"
import {useEffect} from "react";

export default function AccountFormBase({
                                            initialState,
                                            buildAccountPayload,
                                            endpoint,
                                            submitText,
                                            children
                                        }) {
    const toast = useToast();

    const {
        formData,
        setFormData,
        changeField
    } = useAccountFormState(initialState)

    const {
        handleSubmit,
        submitting,
        error,
        clearError
    } = useAccountSubmit({
        endpoint,
        buildAccountPayload,
        formData
    })

    const {
        dropRef,
        onDrop,
        onDragOver,
        onDragLeave,
        onFileChange,
        removePhoto
    } = usePhotoUpload(setFormData)

    useEffect(() => {
        if (!error) return;

        toast.addToast({
            message: error
        });

        clearError();
    }, [error, toast, clearError]);

    return (
        <form className="account-form" onSubmit={handleSubmit}>
            {children(formData, changeField)}

            <div
                ref={dropRef}
                className={`file-input-wrapper file-label ${
                    formData.photoPreview ? "has-preview" : ""
                }`}
                onDrop={onDrop}
                onDragOver={onDragOver}
                onDragLeave={onDragLeave}
                onClick={() =>
                    dropRef.current
                        ?.querySelector('input[type="file"]')
                        ?.click()
                }
                role="button"
            >
                {formData.photoPreview ? (
                    <>
                        <img
                            src={formData.photoPreview}
                            alt="preview"
                            className="drop-preview"
                        />
                        <div className="preview-actions">
                            <button
                                type="button"
                                className="remove-address-btn"
                                onClick={removePhoto}
                            >
                                Remove
                            </button>
                        </div>
                    </>
                ) : (
                    <div className="upload-placeholder">
                        <div className="upload-title">
                            Drag photo here or click to select
                        </div>
                        <div className="upload-subtitle">
                            PNG / JPG, up to 5MB
                        </div>
                    </div>
                )}

                <input
                    type="file"
                    accept="image/*"
                    className="file-input"
                    onChange={onFileChange}
                />
            </div>

            <button type="submit" disabled={submitting}>
                {submitting ? "Submitting..." : submitText}
            </button>
        </form>
    )
}