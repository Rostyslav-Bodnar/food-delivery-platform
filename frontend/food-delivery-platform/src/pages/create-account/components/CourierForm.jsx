import AccountFormBase from "./AccountFormBase";

export default function CourierForm() {
    return (
        <AccountFormBase
            endpoint="courier"
            submitText="Create Courier Account"
            initialState={{
                firstName: "",
                lastName: "",
                phone: "",
                address: "",
                description: "",
                photoFile: null,
                photoPreview: ""
            }}
            buildAccountPayload={(data) => ({
                name: data.firstName,
                surname: data.lastName,
                phoneNumber: data.phone,
                address: data.address,
                description: data.description,
                imageFile: data.photoFile,
                accountType: 2
            })}
        >
            {(formData, changeField) => (
                <>
                    <div className="account-form-header">
                        <input name="firstName" placeholder="First Name" value={formData.firstName} onChange={changeField} required />
                        <input name="lastName" placeholder="Last Name" value={formData.lastName} onChange={changeField} required />
                        <input name="phone" placeholder="Phone Number" value={formData.phone} onChange={changeField} required />
                    </div>

                    <input name="address" placeholder="Address" value={formData.address} onChange={changeField} required />
                    <textarea name="description" placeholder="Description" value={formData.description} onChange={changeField} />
                </>
            )}
        </AccountFormBase>
    );
}
