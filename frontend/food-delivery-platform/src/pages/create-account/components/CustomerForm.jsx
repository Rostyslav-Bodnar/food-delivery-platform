import AccountFormBase from "./AccountFormBase";

export default function CustomerForm() {
    return (
        <AccountFormBase
            endpoint="customer"
            submitText="Create Customer Account"
            initialState={{
                firstName: "",
                lastName: "",
                phone: "",
                address: "",
                photoFile: null,
                photoPreview: ""
            }}
            buildAccountPayload={(data) => ({
                name: data.firstName,
                surname: data.lastName,
                phoneNumber: data.phone,
                address: data.address,
                imageFile: data.photoFile,
                accountType: 0
            })}
        >
            {(formData, changeField) => (
                <>
                    <input name="firstName" placeholder="First Name" value={formData.firstName} onChange={changeField} required />
                    <input name="lastName" placeholder="Last Name" value={formData.lastName} onChange={changeField} required />
                    <input name="phone" placeholder="Phone Number" value={formData.phone} onChange={changeField} required />
                    <input name="address" placeholder="Address" value={formData.address} onChange={changeField} required />
                </>
            )}
        </AccountFormBase>
    );
}
