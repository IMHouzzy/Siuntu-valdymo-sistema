import { useState, useEffect, useRef, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../services/AuthContext";
import {
  FiUser,
  FiChevronDown,
  FiSettings,
  FiLogOut,
  FiBell,
  FiPackage,
  FiShoppingBag,
  FiRefreshCw,
  FiInfo,
  FiCheckCircle,
  FiShoppingCart,
  FiCheck,
  FiTrash2,
  FiGrid,
} from "react-icons/fi";
import "../styles/SignInButtons.css";
import { notificationsApi } from "../services/api";
// ── Helpers ───────────────────────────────────────────────────────────────────

function getInitials(name = "") {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return "U";
  if (parts.length === 1) return parts[0][0].toUpperCase();
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
}
function getDashboardPath(user) {
  const role = user?.companyRole ?? user?.activeCompany?.role ?? "";
    console.log("user object:", user, "resolved role:", role);
  if (role === "CLIENT") return "/client";
  if (role === "COURIER") return "/courier";
  return "/"; // admin, staff, or anything else
    
}
function formatRelativeTime(dateStr) {
  const date = new Date(dateStr);
  const now = new Date();
  const diffMs = now - date;
  const diffMin = Math.floor(diffMs / 60000);
  const diffHr = Math.floor(diffMin / 60);
  const diffDay = Math.floor(diffHr / 24);

  if (diffMin < 1) return "Ką tik";
  if (diffMin < 60) return `prieš ${diffMin} min.`;
  if (diffHr < 24) return `prieš ${diffHr} val.`;
  if (diffDay === 1) return "Vakar";
  if (diffDay < 7) return `prieš ${diffDay} d.`;
  return date.toLocaleDateString("lt-LT", { day: "2-digit", month: "short" });
}

function NotifIcon({ type }) {
  const props = { size: 14 };
  switch (type) {
    case "ORDER": return <FiShoppingBag {...props} />;
    case "SHIPMENT": return <FiPackage {...props} />;
    case "RETURN": return <FiRefreshCw {...props} />;
    case "INVOICE": return <FiCheckCircle {...props} />;
    default: return <FiInfo {...props} />;
  }
}

function notifAccentClass(type) {
  switch (type) {
    case "ORDER": return "sb-notif-item--order";
    case "SHIPMENT": return "sb-notif-item--shipment";
    case "RETURN": return "sb-notif-item--return";
    case "INVOICE": return "sb-notif-item--invoice";
    default: return "";
  }
}

// ── Main component ────────────────────────────────────────────────────────────

export default function SignInButtons() {
  const { logout, user, companyRole} = useAuth();
  const navigate = useNavigate();

  const [open, setOpen] = useState(false);
  const [notifOpen, setNotifOpen] = useState(false);

  const [notifications, setNotifications] = useState([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [loading, setLoading] = useState(false);
  const [fetched, setFetched] = useState(false);   // only fetch once per open

  const bellRef = useRef(null);
  const dropRef = useRef(null);

  const displayName = user?.fullName || user?.name || "";
  const initials = getInitials(displayName);

  // ── API helpers ─────────────────────────────────────────────────────────────


  const fetchUnreadCount = useCallback(async () => {
    try {
      const data = await notificationsApi.getUnreadCount();
      setUnreadCount(data.count ?? 0);
    } catch { /* silent */ }
  }, []);

  const fetchNotifications = useCallback(async () => {
    setLoading(true);
    try {
      const data = await notificationsApi.getAll(20);
      setNotifications(data.items ?? []);
      setUnreadCount((data.items ?? []).filter(n => !n.isRead).length);
      setFetched(true);
    } catch { /* silent */ }
    finally { setLoading(false); }
  }, []);

  // ── Poll unread count every 60 s ────────────────────────────────────────────
  useEffect(() => {
    fetchUnreadCount();
    const id = setInterval(fetchUnreadCount, 60_000);
    return () => clearInterval(id);
  }, [fetchUnreadCount]);

  // ── Load notifications when dropdown opens ──────────────────────────────────
  useEffect(() => {
    if (notifOpen && !fetched) fetchNotifications();
  }, [notifOpen, fetched, fetchNotifications]);

  // ── Close on outside click ───────────────────────────────────────────────────
  useEffect(() => {
    if (!notifOpen) return;
    const handle = (e) => {
      if (
        bellRef.current && !bellRef.current.contains(e.target) &&
        dropRef.current && !dropRef.current.contains(e.target)
      ) setNotifOpen(false);
    };
    document.addEventListener("mousedown", handle);
    return () => document.removeEventListener("mousedown", handle);
  }, [notifOpen]);

  // ── Actions ─────────────────────────────────────────────────────────────────

  const markRead = async (id) => {
    setNotifications(prev => prev.map(n => n.id_Notification === id ? { ...n, isRead: true } : n));
    setUnreadCount(c => Math.max(0, c - 1));
    try { await notificationsApi.markRead(id); } catch { /* silent */ }
  };

  const markAllRead = async () => {
    setNotifications(prev => prev.map(n => ({ ...n, isRead: true })));
    setUnreadCount(0);
    try { await notificationsApi.markAllRead(); } catch { /* silent */ }
  };
  const deleteNotif = async (e, id) => {
    e.stopPropagation();
    setNotifications(prev => {
      const n = prev.find(x => x.id_Notification === id);
      if (n && !n.isRead) setUnreadCount(c => Math.max(0, c - 1));
      return prev.filter(x => x.id_Notification !== id);
    });
    try { await notificationsApi.remove(id); } catch { /* silent */ }
  };

  const handleNotifClick = (n) => {
    if (!n.isRead) markRead(n.id_Notification);
    if (n.referenceType === "ORDER" && n.referenceId) {
      navigate(`/client/profile#orders?order=${n.referenceId}`);
    }

    if (n.referenceType === "RETURN") {
      navigate(`/client/profile#orders?tab=returns`);
    }

    if (n.referenceType === "SHIPMENT" && n.referenceId) {
      navigate(`/client/track/${n.referenceId}`);
    }
    setNotifOpen(false);
  };

  const handleLogout = () => {
    logout();
    setOpen(false);
    navigate("/login");
  };

  // ── Render ──────────────────────────────────────────────────────────────────

  return (
    <div className="sb-actions">

      {/* 🔔 NOTIFICATION BELL */}
      <div className="sb-bell-wrap" ref={bellRef}>
        <button
          className={`sb-bell-btn ${notifOpen ? "sb-bell-btn--active" : ""}`}
          onClick={() => setNotifOpen(v => !v)}
          aria-label="Pranešimai"
        >
          <FiBell size={18} />
          {unreadCount > 0 && (
            <span className="sb-bell-badge">
              {unreadCount > 99 ? "99+" : unreadCount}
            </span>
          )}
        </button>

        <div
          ref={dropRef}
          className={`sb-notif-dropdown ${notifOpen ? "sb-notif-dropdown--open" : ""}`}
        >
          {/* Header */}
          <div className="sb-notif-header">
            <div className="sb-notif-header-left">
              <span className="sb-notif-title">Pranešimai</span>
              {unreadCount > 0 && (
                <span className="sb-notif-count-badge">{unreadCount}</span>
              )}
            </div>
            {unreadCount > 0 && (
              <button className="sb-notif-mark-all" onClick={markAllRead} title="Pažymėti visus kaip skaitytus">
                <FiCheck size={12} />
                Visi perskaityti
              </button>
            )}
          </div>

          {/* Body */}
          <div className="sb-notif-body">
            {loading ? (
              <div className="sb-notif-state">
                <div className="sb-notif-spinner" />
                <span>Kraunama...</span>
              </div>
            ) : notifications.length === 0 ? (
              <div className="sb-notif-state">
                <FiBell size={24} className="sb-notif-empty-icon" />
                <span>Pranešimų nėra</span>
              </div>
            ) : (
              notifications.map(n => (
                <div
                  key={n.id_Notification}
                  className={`sb-notif-item ${!n.isRead ? "sb-notif-item--unread" : ""} ${notifAccentClass(n.type)}`}
                  onClick={() => handleNotifClick(n)}
                >
                  <div className="sb-notif-icon-wrap">
                    <NotifIcon type={n.type} />
                  </div>

                  <div className="sb-notif-content">
                    <p className="sb-notif-theme">{n.theme}</p>
                    <p className="sb-notif-text">{n.content}</p>
                    <span className="sb-notif-time">{formatRelativeTime(n.date)}</span>
                  </div>

                  <div className="sb-notif-actions">
                    {!n.isRead && (
                      <button
                        className="sb-notif-read-btn"
                        title="Pažymėti kaip perskaitytą"
                        onClick={(e) => { e.stopPropagation(); markRead(n.id_Notification); }}
                      >
                        <FiCheck size={11} />
                      </button>
                    )}
                    <button
                      className="sb-notif-del-btn"
                      title="Ištrinti"
                      onClick={(e) => deleteNotif(e, n.id_Notification)}
                    >
                      <FiTrash2 size={11} />
                    </button>
                  </div>
                </div>
              ))
            )}
          </div>

          {/* Footer */}
          {notifications.length > 0 && (
            <div className="sb-notif-footer">
              <button
                className="sb-notif-footer-btn"
                onClick={() => { setNotifOpen(false); navigate("/notifications"); }}
              >
                Visi pranešimai
              </button>
            </div>
          )}
        </div>
      </div>

      {/* 👤 USER MENU */}
      <div className="sb-user-wrap" onMouseLeave={() => setOpen(false)}>
        <button className="sb-user-btn" onClick={() => setOpen(v => !v)}>
          <div className="sb-avatar">
            {user ? <span>{initials}</span> : <FiUser size={14} />}
          </div>
          {user && <span className="sb-user-name">{displayName}</span>}
          <FiChevronDown
            size={14}
            className={`sb-chev ${open ? "sb-chev--open" : ""}`}
          />
        </button>

        <div className={`sb-dropdown ${open ? "sb-dropdown--open" : ""}`}>
          {user ? (
            <>
              <div className="sb-dropdown-top">
                <div className="sb-dropdown-avatar">{initials}</div>
                <div>
                  <div className="sb-dropdown-name">{displayName}</div>
                  <div className="sb-dropdown-email">{user?.email || ""}</div>
                </div>
              </div>
              {companyRole !== "CLIENT" && (
                <button
                  className="sb-dropdown-item"
                  onClick={() => { setOpen(false); navigate(getDashboardPath(user)); }}
                >
                  <FiGrid size={14} />
                  Darbo aplinka
                </button>
              )}
              <div className="sb-dropdown-divider" />

              <button
                className="sb-dropdown-item"
                onClick={() => { setOpen(false); navigate("/client/profile"); }}
              >
                <FiUser size={14} />
                Profilio informacija
              </button>
              <button
                className="sb-dropdown-item"
                onClick={() => { setOpen(false); navigate("/client/profile#notifications"); }}
              >
                <FiBell size={14} />
                Pranešimai
              </button>
              <button
                className="sb-dropdown-item"
                onClick={() => { setOpen(false); navigate("/client/profile#settings"); }}
              >
                <FiSettings size={14} />
                Nustatymai
              </button>
              <button
                className="sb-dropdown-item"
                onClick={() => { setOpen(false); navigate("/client/profile#orders"); }}
              >
                <FiShoppingCart size={14} />
                Mano užsakymai
              </button>



              <div className="sb-dropdown-divider" />

              <button className="sb-dropdown-logout" onClick={handleLogout}>
                <FiLogOut size={14} />
                Atsijungti
              </button>
            </>
          ) : (
            <div className="sb-dropdown-top">
              <div className="sb-dropdown-avatar"><FiUser size={14} /></div>
              <div>
                <div className="sb-dropdown-name">Svečias</div>
                <div className="sb-dropdown-email">Neprisijungęs vartotojas</div>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}