import React, { useMemo, useState, useEffect } from "react";
import { NavLink, useLocation } from "react-router-dom";
import {
  FiBox,
  FiUsers,
  FiShoppingCart,
  FiChevronDown,
  FiMenu,
  FiBriefcase,
  FiLock,
  FiPackage,
  FiRotateCcw,
  FiRefreshCw,
} from "react-icons/fi";
import { LuLayoutDashboard } from "react-icons/lu";
import "../styles/SidebarLeft.css";

import TrackSyncBig from "../images/TrackSync_Big.png";
import TrackSyncSmall from "../images/TrackSync_Small.png";
import { useAuth } from "../services/AuthContext";

const LS_KEY = "sidebar_open_groups_v1";
const ASSET_BASE = (process.env.REACT_APP_API_URL || "/api").replace(/\/api\/?$/, "");

function normalizeRole(r) {
  return String(r || "").trim().toUpperCase();
}

export default function SidebarLeft({ collapsed, onToggle }) {
  const location = useLocation();
  const { companies, activeCompany, switchCompany, companySwitchLocked, user } = useAuth();
  const [companyOpen, setCompanyOpen] = useState(false);

  const isMaster = !!user?.isMasterAdmin;

  // role aktyvioj įmonėj (ateina iš JWT claim companies[])
  const myCompanyRole = useMemo(() => {
    if (!Array.isArray(companies) || !activeCompany?.id) return "";
    const found = companies.find((c) => String(c.id_Company) === String(activeCompany.id));
    return normalizeRole(found?.role);
  }, [companies, activeCompany?.id]);

  // Staff -> nemato Companies grupės. Admin/Owner -> mato. Master -> mato.
  const canSeeCompaniesGroup = useMemo(() => {
    if (isMaster) return true;
    return myCompanyRole === "ADMIN" || myCompanyRole === "OWNER";
  }, [isMaster, myCompanyRole]);

  // tik master kuria įmones
  const canCreateCompany = isMaster;

  const nav = useMemo(() => {
    const base = [
      { type: "link", id: "Suvestinė", label: "Suvestinė", icon: <LuLayoutDashboard />, to: "/" },
      {
        type: "group",
        id: "Užsakymai",
        label: "Užsakymai",
        icon: <FiShoppingCart />,
        children: [
          { label: "Užsakymų sąrašas", to: "/orderList" },
          { label: "Kurti užsakymą", to: "/orderAdd" },
        ],
      },
      {
        type: "group",
        id: "Prekės",
        label: "Prekės",
        icon: <FiBox />,
        children: [
          { label: "Prekių sąrašas", to: "/productList" },
          { label: "Kurti prekę", to: "/productAdd" },
        ],
      },
      { type: "link", id: "Siuntos", label: "Siuntos", icon: <FiPackage />, to: "/shipmentsList" },
      { type: "link", id: "Grąžinimai", label: "Grąžinimai", icon: <FiRotateCcw />, to: "/returnsList" },
      {
        type: "group",
        id: "Naudotojai",
        label: "Naudotojai",
        icon: <FiUsers />,
        children: [
          { label: "Naudotojų sąrašas", to: "/usersList" },
          { label: "Kurti naudotoją", to: "/userAdd" },
        ],
      },

    ];
    if (activeCompany?.id_Company) {
      base.push({
        type: "link",
        id: "Būtent sinchronizacija",
        label: "Būtent sinchronizacija",
        icon: <FiRefreshCw />,
        to: `/admin/company/${activeCompany.id_Company}/butent-sync`,
      });
    }
    if (canSeeCompaniesGroup) {
      base.push({
        type: "group",
        id: "Įmonės",
        label: "Įmonės",
        icon: <FiBriefcase />,
        children: [
          { label: "Įmonių sąrašas", to: "/companiesList" },
          ...(canCreateCompany ? [{ label: "Kurti įmonę", to: "/companyAdd" }] : []),
        ],
      });
    }


    return base;
  }, [canSeeCompaniesGroup, canCreateCompany]);

  const [openGroups, setOpenGroups] = useState(() => {
    try {
      const raw = localStorage.getItem(LS_KEY);
      if (raw) return new Set(JSON.parse(raw));
    } catch { }
    return new Set();
  });

  // Company switching: tik master + >1 įmonė + ne lock
  const canSwitchCompany = useMemo(() => {
    return (
      isMaster &&
      Array.isArray(companies) &&
      companies.length > 1 &&
      !companySwitchLocked
    );
  }, [isMaster, companies, companySwitchLocked]);

  useEffect(() => {
    if (collapsed || !canSwitchCompany) setCompanyOpen(false);
  }, [collapsed, canSwitchCompany]);

  useEffect(() => {
    const path = location.pathname;

    const activeGroup = nav.find(
      (x) =>
        x.type === "group" &&
        x.children?.some((c) => path === c.to || path.startsWith(c.to + "/"))
    );

    if (!activeGroup) return;

    setOpenGroups((prev) => {
      if (prev.has(activeGroup.id)) return prev;
      const next = new Set(prev);
      next.add(activeGroup.id);
      return next;
    });
  }, [location.pathname, nav]);

  useEffect(() => {
    try {
      localStorage.setItem(LS_KEY, JSON.stringify(Array.from(openGroups)));
    } catch { }
  }, [openGroups]);

  const toggleGroup = (id) => {
    setOpenGroups((prev) => {
      const next = new Set(prev);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  };

  const handleGroupClick = (id) => {
    if (collapsed) {
      onToggle?.();
      setOpenGroups((prev) => {
        const next = new Set(prev);
        next.add(id);
        return next;
      });
      return;
    }
    toggleGroup(id);
  };

  const companyTitle = companySwitchLocked
    ? "Negalima keisti įmonės redaguojant/kuriant"
    : canSwitchCompany
      ? "Keisti įmonę"
      : undefined;

  return (
    <aside className={`sidebar ${collapsed ? "is-collapsed" : ""}`}>
      <div className="sidebar-top">
        <button
          className="sidebar-collapse-btn"
          onClick={onToggle}
          aria-label={collapsed ? "Expand sidebar" : "Collapse sidebar"}
          title={collapsed ? "Expand" : "Collapse"}
          type="button"
        >
          <FiMenu />
        </button>

        <div className="sidebar-logo">
          <img className="sidebar-logo-small" src={TrackSyncSmall} alt="TrackSync" draggable="false" />
          <img
            className={`sidebar-logo-big ${collapsed ? "is-hidden" : "is-visible"}`}
            src={TrackSyncBig}
            alt="TrackSync"
            draggable="false"
          />
        </div>
      </div>

      <nav className="sidebar-nav">
        {nav.map((item) => {
          if (item.type === "link") {
            return (
              <NavLink
                key={item.id}
                to={item.to}
                className={({ isActive }) => `nav-item ${isActive ? "is-active" : ""}`}
                title={collapsed ? item.label : undefined}
              >
                <span className="nav-icon">{item.icon}</span>
                {!collapsed && <span className="nav-label">{item.label}</span>}
              </NavLink>
            );
          }

          const isOpen = openGroups.has(item.id);

          return (
            <div key={item.id} className="nav-group">
              <button
                className="nav-item nav-group-btn"
                onClick={() => handleGroupClick(item.id)}
                title={collapsed ? item.label : undefined}
                type="button"
              >
                <span className="nav-icon">{item.icon}</span>
                {!collapsed && (
                  <>
                    <span className="nav-label">{item.label}</span>
                    <span className={`chev ${isOpen ? "is-open" : ""}`}>
                      <FiChevronDown />
                    </span>
                  </>
                )}
              </button>

              {!collapsed && isOpen && (
                <div className="nav-sub">
                  {item.children.map((c) => (
                    <NavLink
                      key={c.to}
                      to={c.to}
                      className={({ isActive }) => `nav-sub-item ${isActive ? "is-active" : ""}`}
                    >
                      {c.label}
                    </NavLink>
                  ))}
                </div>
              )}
            </div>
          );
        })}
      </nav>

      {/* Bottom: company select (tik master) */}
      <div className="sidebar-bottom" onMouseLeave={() => setCompanyOpen(false)}>
        <div className="sidebar-bottom-left">
          <div className="sidebar-bottom-image">
            <img
              src={
                activeCompany?.image
                  ? `${ASSET_BASE}${activeCompany.image}`
                  : TrackSyncSmall
              }
              alt={activeCompany?.name || "Company"}
              draggable="false"
            />
          </div>
        </div>

        {!collapsed && (
          <div className="sidebar-bottom-right">
            <div className={`sb-select ${(!canSwitchCompany || collapsed) ? "is-disabled" : ""}`}>
              <button
                type="button"
                className={`sb-select-trigger ${companyOpen ? "is-open" : ""}`}
                onClick={() => {
                  if (!canSwitchCompany || collapsed) return;
                  setCompanyOpen((v) => !v);
                }}
                disabled={!canSwitchCompany || collapsed}
                title={companyTitle}
                aria-haspopup="listbox"
                aria-expanded={companyOpen ? "true" : "false"}
              >
                <div className="sb-select-value">
                  <div className="sb-select-name">{activeCompany?.name || "— Įmonė —"}</div>
                  <div className="sb-select-meta">
                    {activeCompany?.code || (activeCompany?.id ? `ID: ${activeCompany.id}` : "—")}
                  </div>
                </div>

                {!collapsed && (
                  <>
                    {companySwitchLocked ? (
                      <FiLock className="sb-select-lock" size={16} />
                    ) : canSwitchCompany ? (
                      <FiChevronDown className={`sb-select-chev ${companyOpen ? "open" : ""}`} size={16} />
                    ) : null}
                  </>
                )}
              </button>

              {canSwitchCompany && companyOpen && !collapsed && (
                <div className="sb-select-menu" role="listbox">
                  {companies.map((c) => {
                    const isActive = String(c.id_Company) === String(activeCompany?.id);
                    return (
                      <button
                        key={c.id_Company}
                        type="button"
                        role="option"
                        aria-selected={isActive ? "true" : "false"}
                        className={`sb-select-item ${isActive ? "is-active" : ""}`}
                        onClick={async () => {
                          try {
                            if (isActive) {
                              setCompanyOpen(false);
                              return;
                            }
                            await switchCompany(c.id_Company);
                            setCompanyOpen(false);
                          } catch (e) {
                            console.error(e);
                            alert("Nepavyko pakeisti įmonės");
                          }
                        }}
                      >
                        <div className="sb-select-item-name">{c.name}</div>
                        <div className="sb-select-item-meta">
                          {c.companyCode || c.code || `ID: ${c.id_Company}`}
                        </div>
                      </button>
                    );
                  })}
                </div>
              )}
            </div>
          </div>
        )}
      </div>
    </aside>
  );
}
