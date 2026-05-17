
import { Link } from "react-router-dom";
import { FiPackage, FiSearch, FiShoppingBag, FiMail } from "react-icons/fi";
import "../styles/ClientFooter.css";
import Logo from "../images/Full_track_sync_logo2.png"
export default function ClientFooter({
  company = "TrackSync",
  year = new Date().getFullYear(),
  version,
}) {
  return (
    <footer className="cf-footer">
      <div className="cf-inner">

        {/* ── Brand ────────────────────────────────────── */}
        <div className="cf-brand">
          <div className="cf-brand-logo">
            <img src={Logo}/>
          </div>
          <p className="cf-brand-desc">
            Sekite savo siuntas realiuoju laiku ir valdykite užsakymus vienoje vietoje.
          </p>
        </div>

        {/* ── Links ────────────────────────────────────── */}
        <div className="cf-links-group">
          <div className="cf-links-title">Navigacija</div>
          <Link to="/client" className="cf-link">
            <FiSearch size={13} /> Sekti siuntą
          </Link>
          <Link to="/profile#orders" className="cf-link">
            <FiShoppingBag size={13} /> Mano užsakymai
          </Link>
        </div>

        {/* <div className="cf-links-group">
          <div className="cf-links-title">Pagalba</div>
          <a href="mailto:info@tracksync.lt" className="cf-link">
            <FiMail size={13} /> Susisiekti
          </a>
        </div> */}

      </div>

      {/* ── Bottom bar ───────────────────────────────────── */}
      <div className="cf-bottom">
        <span className="cf-copy">© {year} {company}. Visos teisės saugomos.</span>
        {version && <span className="cf-version">v{version}</span>}
      </div>
    </footer>
  );
}