export function ImageDropzone({
                                  imagePreview,
                                  dropRef,
                                  onDrop,
                                  onDragOver,
                                  onDragLeave,
                                  onSelectFile
                              }) {
    return (
        <div
            ref={dropRef}
            className="bh-droparea"
            onDrop={onDrop}
            onDragOver={onDragOver}
            onDragLeave={onDragLeave}
            onClick={() =>
                dropRef.current?.querySelector('input[type="file"]')?.click()
            }
        >
            {imagePreview ? (
                <img src={imagePreview} alt="preview" />
            ) : (
                <div style={{ textAlign: "center" }}>
                    <div style={{ fontWeight: 700 }}>
                        Drag or select an image
                    </div>
                    <div style={{ fontSize: 13 }}>
                        PNG / JPG, up to 5MB
                    </div>
                </div>
            )}
            <input
                type="file"
                accept="image/*"
                style={{ display: "none" }}
                onChange={onSelectFile}
            />
        </div>
    );
}