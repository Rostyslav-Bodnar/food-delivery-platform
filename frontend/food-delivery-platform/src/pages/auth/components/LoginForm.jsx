import "../styles/AuthForm.css"
import useLoginForm from "../hooks/useLoginForm"
import { useToast } from "../../../global-components/toast/ToastContext";
import { useEffect } from "react";

const LoginForm = ({ onSwitch }) => {
    const toast = useToast();

    const {
        formData,
        formError,
        systemError,
        submitting,
        handleChange,
        handleSubmit,
        clearSystemError
    } = useLoginForm();

    useEffect(() => {
        if (!systemError) return;

        toast.addToast({
            message: systemError
        });

        clearSystemError();
    }, [systemError, toast, clearSystemError]);

    return (
        <>
            <h2>Welcome back</h2>

            <form className="auth-form" onSubmit={handleSubmit}>
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
                    {submitting ? "Signing in..." : "Login"}
                </button>
            </form>

            <p className="login-text">
                Don’t have an account?
                <button className="link-btn" onClick={onSwitch}>
                    Register
                </button>
            </p>
        </>
    );
};

export default LoginForm;