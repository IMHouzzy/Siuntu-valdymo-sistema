
import { useState } from "react";
import { Link, useNavigate, useLocation } from "react-router-dom";
import { useAuth } from "../services/AuthContext";
import {
  FiPackage, FiShoppingBag, FiUser, FiLogOut,
  FiChevronDown, FiSearch, FiMenu, FiX
} from "react-icons/fi";
import "../styles/ClientHeader.css";
import SignInProfile from "./SignInButtons"
import Logo from "../images/Full_track_sync_logo2.png"
function getInitials(name = "") {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return "U";
  if (parts.length === 1) return parts[0][0].toUpperCase();
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
}

export default function ClientHeader() {
  const { logout, user } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [menuOpen, setMenuOpen] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);

  const displayName = user?.fullName || user?.name || "";
  const initials = getInitials(displayName);

  const handleLogout = () => {
    logout();
    setMenuOpen(false);
    navigate("/login");
  };

  const isActive = (path) => location.pathname === path || location.pathname.startsWith(path + "/");

  return (
    <header className="ch-header">
      <div className="ch-inner">

        {/*  Logo  */}
        <Link to="/client" className="ch-logo">
          <img src={Logo} />
        </Link>

        {/*  Nav  */}
        <nav className={`ch-nav ${mobileOpen ? "ch-nav--open" : ""}`}>
          <Link
            to="/client"
            className={`ch-nav-link ${isActive("/client") && location.pathname === "/client" ? "ch-nav-link--active" : ""}`}
            onClick={() => setMobileOpen(false)}
          >
            <FiSearch size={15} />
            Sekti siuntą
          </Link>
          <Link
            to="/client/profile#orders"
            className={`ch-nav-link ${isActive("/client/profile#orders") ? "ch-nav-link--active" : ""}`}
            onClick={() => setMobileOpen(false)}
          >
            <FiShoppingBag size={15} />
            Mano užsakymai
          </Link>
        </nav>

        {/*  Right  */}
        <div className="ch-right">
          <SignInProfile />

          {/* Mobile hamburger */}
          <button className="ch-mobile-toggle" onClick={() => setMobileOpen((v) => !v)}>
            {mobileOpen ? <FiX size={20} /> : <FiMenu size={20} />}
          </button>
        </div>
      </div>
    </header>
  );
}