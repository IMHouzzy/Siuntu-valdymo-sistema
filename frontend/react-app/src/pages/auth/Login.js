import "../../styles/Auth.css";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { useState } from "react";
import { GoogleLogin } from "@react-oauth/google";
import { useAuth } from "../../services/AuthContext";
import Logo from "../../images/Full_track_sync_logo2.png";
import { authValidation } from "./authValidation";

function getRedirectPath(data) {
    const role = data?.companyRole ?? data?.activeCompany?.role ?? "";
    if (role === "CLIENT") return "/client";
    if (role === "COURIER") return "/courier";
    return "/";
}

function Login() {
    const location = useLocation();
    const navigate = useNavigate();
    const { login, googleLogin } = useAuth();

    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    
    const [errors, setErrors] = useState({});
    const [touched, setTouched] = useState({});
    const [serverError, setServerError] = useState("");
    const [loading, setLoading] = useState(false);

    // Real-time validation
    const validateField = (fieldName, value) => {
        const formData = {
            email,
            password,
            [fieldName]: value,
        };
        
        const allErrors = authValidation.validateLoginForm(formData);
        
        setErrors(prev => ({
            ...prev,
            [fieldName]: allErrors[fieldName] || null,
        }));
    };

    const handleBlur = (fieldName) => {
        setTouched(prev => ({ ...prev, [fieldName]: true }));
    };

    const handleLogin = async (e) => {
        e.preventDefault();
        setServerError("");

        // Mark all fields as touched
        setTouched({ email: true, password: true });

        // Validate all fields
        const validationErrors = authValidation.validateLoginForm({ email, password });
        setErrors(validationErrors);

        // Stop if there are validation errors
        if (Object.keys(validationErrors).length > 0) {
            return;
        }

        setLoading(true);
        try {
            const data = await login(email, password);
            navigate(getRedirectPath(data));
        } catch (err) {
            setServerError(err.message || "Prisijungimas nepavyko.");
        } finally {
            setLoading(false);
        }
    };

    const handleGoogleLogin = async (credentialResponse) => {
        try {
            const data = await googleLogin(credentialResponse.credential);
            navigate(getRedirectPath(data));
        } catch (err) {
            setServerError(err.message || "Google prisijungimas nepavyko.");
        }
    };

    return (
        <div className="login-container">
            <div className="login-wrapper">
                <img className="login-logo" src={Logo} alt="Logo" />
                <div className="login-select">
                    <Link
                        to="/login"
                        className={location.pathname === "/login" ? "login-select-button active" : "login-select-button"}>
                        Prisijungimas
                    </Link>
                    <Link
                        to="/register"
                        className={location.pathname === "/register" ? "register-select-button active" : "register-select-button"}>
                        Registracija
                    </Link>
                </div>

                <form className="login-form" onSubmit={handleLogin}>
                    <div className="form-group">
                        <input
                            type="email"
                            placeholder="El. paštas"
                            value={email}
                            onChange={e => {
                                setEmail(e.target.value);
                                if (touched.email) validateField("email", e.target.value);
                            }}
                            onBlur={() => handleBlur("email")}
                            className={touched.email && errors.email ? "error" : ""}
                        />
                        {touched.email && errors.email && (
                            <span className="error-text">{errors.email}</span>
                        )}
                    </div>

                    <div className="form-group">
                        <input
                            type="password"
                            placeholder="Slaptažodis"
                            value={password}
                            onChange={e => {
                                setPassword(e.target.value);
                                if (touched.password) validateField("password", e.target.value);
                            }}
                            onBlur={() => handleBlur("password")}
                            className={touched.password && errors.password ? "error" : ""}
                        />
                        {touched.password && errors.password && (
                            <span className="error-text">{errors.password}</span>
                        )}
                    </div>

                    <div className="forgot-password">
                        <Link to="/forgot-password">Pamiršote slaptažodį?</Link>
                    </div>

                    {serverError && (
                        <p style={{ color: "var(--color-danger)", fontSize: "0.8rem", margin: "4px 0" }}>
                            {serverError}
                        </p>
                    )}

                    <button
                        type="submit"
                        className="login-button"
                        disabled={loading || Object.keys(errors).some(key => errors[key])}
                    >
                        {loading ? "Jungiamasi…" : "Prisijungti"}
                    </button>
                </form>

                <div className="other-login">
                    <div className="or-divider">
                        <span>Arba prisijunkite su</span>
                    </div>
                    <GoogleLogin
                        onSuccess={handleGoogleLogin}
                        onError={() => setServerError("Google prisijungimas nepavyko.")}
                    />
                </div>
            </div>
        </div>
    );
}

export default Login;