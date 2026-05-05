// AuthContext.jsx
import { createContext, useContext, useState, useEffect, useCallback } from "react";

const AuthContext = createContext();

const API = (process.env.REACT_APP_API_URL || "/api").replace(/\/$/, "");

const apiFetch = (url, options = {}) =>
    fetch(url, { credentials: "include", ...options });

export function AuthProvider({ children }) {
    const [user,                setUser]                = useState(null);
    const [companies,           setCompanies]           = useState([]);
    const [activeCompany,       setActiveCompany]       = useState(null);
    const [companyRole,         setCompanyRole]         = useState("");
    const [loading,             setLoading]             = useState(true);
    const [companySwitchLocked, setCompanySwitchLocked] = useState(false);

    const applySession = (data) => {
        if (!data?.userId) {
            setUser(null);
            setCompanies([]);
            setActiveCompany(null);
            setCompanyRole("");
            return;
        }
        setUser({
            id:           data.userId,
            email:        data.email,
            fullName:     data.fullName,
            name:         data.fullName?.split(" ")[0] ?? "",
            isMasterAdmin: data.isMasterAdmin,
            authProvider: data.authProvider,
        });
        setCompanies(data.companies ?? []);
        setActiveCompany(data.activeCompany ?? null);
        setCompanyRole(data.companyRole ?? data.activeCompany?.role ?? "");
    };

    // Restore session on app load — if the cookie exists /me returns user info
    const restoreSession = useCallback(async () => {
        try {
            const res = await apiFetch(`${API}/auth/me`);
            if (res.ok) applySession(await res.json());
        } catch { /* network error — stay logged out */ }
        finally { setLoading(false); }
    }, []);

    useEffect(() => { restoreSession(); }, [restoreSession]);

    const login = async (email, password) => {
        const res = await apiFetch(`${API}/auth/login`, {
            method:  "POST",
            headers: { "Content-Type": "application/json" },
            body:    JSON.stringify({ email, password }),
        });
        if (!res.ok) {
            const text = await res.text().catch(() => "");
            throw new Error(text || "Prisijungimas nepavyko.");
        }
        const data = await res.json();
        applySession(data);
        return data;
    };

    const register = async (dto) => {
        const res = await apiFetch(`${API}/auth/register`, {  // ✅ was missing /api/
            method:  "POST",
            headers: { "Content-Type": "application/json" },
            body:    JSON.stringify(dto),
        });
        if (!res.ok) {
            const text = await res.text().catch(() => "");
            throw new Error(text || "Registracija nepavyko.");
        }
        applySession(await res.json());
    };

    const googleLogin = async (idToken) => {
        const res = await apiFetch(`${API}/auth/google`, {    // ✅ was missing /api/
            method:  "POST",
            headers: { "Content-Type": "application/json" },
            body:    JSON.stringify({ idToken }),
        });
        if (!res.ok) throw new Error("Google prisijungimas nepavyko.");
        const data = await res.json();
        applySession(data);
        return data;
    };

    const logout = async () => {
        try {
            await apiFetch(`${API}/auth/logout`, { method: "POST" }); // ✅ was missing /api/
        } catch { /* ignore */ }
        setUser(null);
        setCompanies([]);
        setActiveCompany(null);
        setCompanyRole("");
    };

    const switchCompany = async (companyId) => {
        if (companySwitchLocked) return;
        const res = await apiFetch(`${API}/auth/switch-company/${companyId}`, { // ✅ was missing /api/
            method: "POST",
        });
        if (!res.ok) {
            const text = await res.text().catch(() => "");
            throw new Error(text || "Switch company failed.");
        }
        applySession(await res.json());
    };

    return (
        <AuthContext.Provider
            value={{
                user,
                companies,
                activeCompany,
                activeCompanyId: activeCompany?.id_Company ?? 0,
                companyRole,
                loading,
                login,
                register,
                googleLogin,
                logout,
                switchCompany,
                token: null,
                companySwitchLocked,
                setCompanySwitchLocked,
            }}
        >
            {!loading && children}
        </AuthContext.Provider>
    );
}

export function useAuth() {
    return useContext(AuthContext);
}
