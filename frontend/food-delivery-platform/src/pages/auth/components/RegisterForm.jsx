import "../styles/AuthForm.css"
import useRegisterForm from "../hooks/useRegisterForm"
import { useToast } from "../../../global-components/toast/ToastContext";
import { useEffect } from "react";

const RegisterForm = ({ onSwitch }) => {
    const toast = useToast();

    const {
        formData,
        formError,
        systemError,
        submitting,
        handleChange,
        handleSubmit,
        clearSystemError
    } = useRegisterForm();

    useEffect(() => {
        if (!systemError) return;

        toast.addToast({
            message: systemError
        });

        clearSystemError();
    }, [systemError, toast, clearSystemError]);

    return (
        <>
            <h2>Create account</h2>

            <form className="auth-form" onSubmit={handleSubmit}>
                <div className="fullname-inputs">
                    <input
                        name="name"
                        placeholder="First name"
                        value={formData.name}
                        onChange={handleChange}
                    />
                    <input
                        name="surname"
                        placeholder="Last name"
                        value={formData.surname}
                        onChange={handleChange}
                    />
                </div>

                <input
                    name="email"
                    placeholder="Email"
                    value={formData.email}
                    onChange={handleChange}
                />

                <input
                    name="password"
                    type="password"
                    placeholder="Password"
                    value={formData.password}
                    onChange={handleChange}
                />

                {formError && (
                    <div className="form-error">
                        {formError}
                    </div>
                )}

                <button type="submit" disabled={submitting}>
                    {submitting ? "Creating account..." : "Register"}
                </button>
            </form>

            <p className="login-text">
                Already have an account?
                <button className="link-btn" onClick={onSwitch}>
                    Login
                </button>
            </p>
        </>
    );
};

export default RegisterForm;