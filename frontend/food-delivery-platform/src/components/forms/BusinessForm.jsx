import AccountFormBase from "./AccountFormBase";

export default function BusinessForm() {
    return (
        <AccountFormBase
            endpoint="business"
            submitText="Create business account"
            initialState={{
                companyName: "",
                description: "",
                photoFile: null,
                photoPreview: ""
            }}
            buildAccountPayload={(data) => ({
                name: data.companyName,
                description: data.description,
                imageFile: data.photoFile,
                accountType: 1
            })}
        >
            {(formData, changeField) => (
                <>
                    <input
                        name="companyName"
                        placeholder="Name of the business"
                        value={formData.companyName}
                        onChange={changeField}
                        required
                    />

                    <textarea
                        name="description"
                        placeholder="Short description"
                        value={formData.description}
                        onChange={changeField}
                        required
                    />
                </>
            )}
        </AccountFormBase>
    );
}
