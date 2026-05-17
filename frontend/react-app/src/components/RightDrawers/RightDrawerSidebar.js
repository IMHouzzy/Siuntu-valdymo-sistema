import React, { useEffect } from "react";
import { FiX } from "react-icons/fi";
import "../../styles/RightDrawerSidebar.css";

/**
 * RightDrawer — shared shell, styled per variant.
 *
 * Props
 * ─────
 * open        boolean
 * onClose     () => void
 * title       string | ReactNode
 * subtitle    string | ReactNode
 * sections    Array<{ title, rows: [{label, value}], emptyText }>
 * actions     ReactNode   — icon buttons rendered in the header
 * hero        ReactNode   — optional full-width block rendered between header and body (avatar, image strip, etc.)
 * variant     "order" | "shipment" | "product" | "user" | "company"
 * width       number (px)
 */
export default function RightDrawer({
  open,
  title,
  subtitle,
  sections = [],
  onClose,
  actions,
  hero,
  variant = "default",
  width = 480,
}) {
  useEffect(() => {
    if (!open) return;
    const onKey = (e) => { if (e.key === "Escape") onClose?.(); };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [open, onClose]);

  if (!open) return null;

  return (
    <div className="rd-overlay" onClick={onClose}>
      <aside
        className={`rd rd--${variant}`}
        style={{ width: `min(${width}px, 92vw)` }}
        onClick={(e) => e.stopPropagation()}
        role="dialog"
        aria-modal="true"
      >
        {/* ── Header ── */}
        <div className="rd-header rd-header--minimal">
          <button className="rd-close" onClick={onClose} title="Uždaryti">
            <FiX size={18} />
          </button>
        </div>

        {/* ── Hero slot (avatar, image strip, map preview…) ── */}

        <div className="rd-hero">{hero}</div>


        {/* ── Body ── */}
        <div className="rd-body">
          {sections.map((sec, idx) => (
            <section className="rd-section" key={`${sec.title}-${idx}`}>
              {sec.title
                ? <h3 className="rd-section-title">{sec.title}</h3>
                : null}

              {sec.rows?.length ? (
                <div className="rd-rows">
                  {sec.rows.map((r, i) => (
                    <div className={`rd-row${r.fullWidth ? " rd-row--full" : ""}`} key={`row-${i}`}>
                      {r.label
                        ? <span className="rd-label">{r.label}</span>
                        : null}
                      <span className={`rd-value${(!r.label || r.fullWidth) ? " rd-value--full" : ""}`}>
                        {r.value ?? "—"}
                      </span>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="rd-empty">{sec.emptyText ?? "Nėra duomenų"}</div>
              )}
            </section>
          ))}

        </div>

      </aside>
    </div>
  );
}