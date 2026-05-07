import React, { useMemo, useState, useEffect, useLayoutEffect, useRef } from "react";
import { createPortal } from "react-dom";
import "../styles/SmartForm.css";
import SearchSelect from "./SearchSelect";
import { FaArrowLeft, FaArrowRight } from "react-icons/fa";
import { FiTrash2, FiImage, FiRefreshCw } from "react-icons/fi";
import { lockerApi, companiesApi } from "../services/api";
const ASSET_BASE = (process.env.REACT_APP_API_URL || "/api").replace(/\/api\/?$/, "");


// ─── Overlay root — one fixed div at z-index 99999, all portals go here ──────
// This escapes every stacking context because it's a direct child of <body>
// with position:fixed, which is unaffected by ancestor transforms/overflow.
function getOverlayRoot() {
  let el = document.getElementById("sf-overlay-root");
  if (!el) {
    el = document.createElement("div");
    el.id = "sf-overlay-root";
    el.style.cssText = [
      "position:fixed",
      "inset:0",
      "pointer-events:none",
      "z-index:99999",
      "overflow:visible",
    ].join(";");
    document.body.appendChild(el);
  }
  return el;
}

// ─── Leaflet helper ───────────────────────────────────────────────────────────
let _L = null;
async function getLeaflet() {
  if (_L) return _L;
  const mod = await import("leaflet");
  _L = mod.default ?? mod;
  delete _L.Icon.Default.prototype._getIconUrl;
  _L.Icon.Default.mergeOptions({
    iconRetinaUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png",
    iconUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png",
    shadowUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png",
  });
  if (!document.getElementById("leaflet-css")) {
    const link = document.createElement("link");
    link.id = "leaflet-css"; link.rel = "stylesheet";
    link.href = "https://unpkg.com/leaflet@1.9.4/dist/leaflet.css";
    document.head.appendChild(link);
  }
  return _L;
}

async function geocodeAddress(address) {
  if (!address) return null;
  try {
    const res = await fetch(
      `https://nominatim.openstreetmap.org/search?format=json&limit=1&q=${encodeURIComponent(address)}`,
      { headers: { "Accept-Language": "lt,en" } }
    );
    const data = await res.json();
    if (!data?.[0]) return null;
    return { lat: parseFloat(data[0].lat), lng: parseFloat(data[0].lon) };
  } catch { return null; }
}
// ─── LogoUploaderWidget ───────────────────────────────────────────────────────
function LogoUploaderWidget({ currentUrl, companyId, isCreate, onPendingFile, onChange }) {
  const fileRef = useRef(null);
  const [preview, setPreview] = useState(null);
  const [uploading, setUploading] = useState(false);

  const displaySrc = preview
    || (currentUrl
      ? (currentUrl.startsWith("http") ? currentUrl : `${ASSET_BASE}${currentUrl}`)
      : null);

  const handleFile = async (file) => {
    if (!file) return;
    setPreview(URL.createObjectURL(file));

    if (isCreate) {
      // Can't upload yet — store the File object for parent to upload after save
      onPendingFile?.(file);
      return;
    }

    setUploading(true);
    try {
      const form = new FormData();
      form.append("file", file);
      const data = await companiesApi.uploadLogo(companyId, form);
      onChange?.(data.imageUrl);
      setPreview(null); // let the saved URL drive the display
    } catch (e) {
      alert(e.message || "Nepavyko įkelti logotipo");
      setPreview(null);
    } finally {
      setUploading(false);
    }
  };

  const clear = () => {
    setPreview(null);
    onChange?.("");
    onPendingFile?.(null);
  };

  return (
    <div className="sf-logo-wrap">
      <div
        className={`sf-logo-drop${uploading ? " is-uploading" : ""}${displaySrc ? " has-image" : ""}`}
        onClick={() => !uploading && fileRef.current?.click()}
        onDragOver={(e) => e.preventDefault()}
        onDrop={(e) => { e.preventDefault(); handleFile(e.dataTransfer.files[0]); }}
      >
        {displaySrc ? (
          <>
            <img src={displaySrc} alt="Logo" className="sf-logo-img" />
            <div className="sf-logo-overlay">
              <span><FiRefreshCw size={20} /></span> Keisti
            </div>
          </>
        ) : (
          <div className="sf-logo-placeholder">
            <span className="sf-logo-icon"><FiImage size={45} /></span>
            <span>{uploading ? "Keliama…" : "Spustelėkite arba vilkite logotipą čia"}</span>
            <span className="sf-logo-hint">JPG, PNG, WebP, SVG</span>
          </div>
        )}
        {uploading && <div className="sf-logo-spinner-wrap"><div className="sf-map-spinner" /></div>}
      </div>
      {displaySrc && (
        <button type="button" className="sf-logo-clear" onClick={clear}>
          <FiTrash2 size={15} /> Pašalinti logotipą
        </button>
      )}
      <input
        ref={fileRef}
        type="file"
        accept="image/jpeg,image/png,image/webp,image/svg+xml"
        style={{ display: "none" }}
        onChange={(e) => handleFile(e.target.files[0])}
      />
    </div>
  );
}
// ─── MapWidget ────────────────────────────────────────────────────────────────
function MapWidget({ address }) {
  const mapElRef = useRef(null);
  const mapRef = useRef(null);
  const markerRef = useRef(null);
  const [status, setStatus] = useState("loading");

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setStatus("loading");
      try {
        const L = await getLeaflet();
        if (cancelled || !mapElRef.current) return;
        if (!mapRef.current) {
          const map = L.map(mapElRef.current, { center: [54.9, 23.9], zoom: 7, zoomControl: true, dragging: true, scrollWheelZoom: true, doubleClickZoom: false });
          L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", { attribution: "© OpenStreetMap", maxZoom: 19 }).addTo(map);
          mapRef.current = map;
        }
        if (!address) { setStatus("notfound"); return; }
        const coords = await geocodeAddress(address);
        if (cancelled) return;
        if (!coords) { setStatus("notfound"); return; }
        if (markerRef.current) { markerRef.current.setLatLng([coords.lat, coords.lng]); }
        else { markerRef.current = _L.marker([coords.lat, coords.lng]).addTo(mapRef.current); }
        mapRef.current.setView([coords.lat, coords.lng], 14);
        setStatus("ready");
      } catch { if (!cancelled) setStatus("error"); }
    })();
    return () => { cancelled = true; };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [address]);

  return (
    <div className="sf-map-wrap">
      <div ref={mapElRef} className="sf-map" />
      {status === "loading" && (<div className="sf-map-overlay"><div className="sf-map-spinner" /><span>Ieškoma adreso…</span></div>)}
      <div className="sf-map-hint">
        {status === "notfound" && "⚠️ Adresas nerastas žemėlapyje"}
        {status === "error" && "⚠️ Nepavyko užkrauti žemėlapio"}
        {status === "ready" && `📍 ${address}`}
      </div>
    </div>
  );
}

// ─── LockerMap ────────────────────────────────────────────────────────────────
function LockerMap({ locker }) {
  const containerRef = useRef(null);
  const mapRef = useRef(null);
  const markerRef = useRef(null);

  useEffect(() => {
    if (!containerRef.current) return;

    let cancelled = false;

    (async () => {
      const L = await getLeaflet();
      if (cancelled || !containerRef.current) return;

      if (!mapRef.current) {
        mapRef.current = L.map(containerRef.current, {
          zoomControl: true,
          dragging: true,
          scrollWheelZoom: false,
          doubleClickZoom: false,
        });

        L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
          attribution: "© OpenStreetMap",
          maxZoom: 19,
        }).addTo(mapRef.current);

        // Default center (Lithuania)
        mapRef.current.setView([55.1694, 23.8813], 7);
      }

      if (locker?.lat && locker?.lng) {
        const latlng = [locker.lat, locker.lng];

        if (!markerRef.current) {
          markerRef.current = L.marker(latlng).addTo(mapRef.current);
        } else {
          markerRef.current.setLatLng(latlng);
        }

        markerRef.current.bindPopup(
          `<strong>${locker.name}</strong><br/>${locker.street}, ${locker.city}`
        );

        mapRef.current.setView(latlng, 15);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [locker]);

  return <div ref={containerRef} className="sf-locker-map" />;
}
// ─── LockerPortalPanel ────────────────────────────────────────────────────────
// Renders into #sf-overlay-root (position:fixed, z-index:99999 in <body>).
// The panel itself uses pointer-events:auto so it receives clicks.
// Position is measured from the trigger button's getBoundingClientRect —
// which is in viewport coordinates, matching the fixed overlay root.
function LockerPortalPanel({ triggerRef, children, onClose }) {
  const [rect, setRect] = useState(null);

  useLayoutEffect(() => {
    if (!triggerRef.current) return;
    const update = () => setRect(triggerRef.current.getBoundingClientRect());
    update();
    window.addEventListener("scroll", update, true);
    window.addEventListener("resize", update);
    return () => {
      window.removeEventListener("scroll", update, true);
      window.removeEventListener("resize", update);
    };
  }, [triggerRef]);

  useEffect(() => {
    const handler = (e) => {
      if (triggerRef.current?.contains(e.target)) return;
      if (document.getElementById("sf-locker-portal")?.contains(e.target)) return;
      onClose();
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, [onClose, triggerRef]);

  if (!rect) return null;

  const panelStyle = {
    position: "absolute",   // relative to the fixed overlay root (= viewport coords)
    top: rect.bottom + 4,
    left: rect.left,
    width: rect.width,
    pointerEvents: "auto",      // re-enable pointer events for the panel
  };

  return createPortal(
    <div id="sf-locker-portal" className="sf-locker-panel" style={panelStyle}>
      {children}
    </div>,
    getOverlayRoot()
  );
}

// ─── LockerPickerWidget ───────────────────────────────────────────────────────
function LockerPickerWidget({ companyId, courierType, value, onChange }) {
  const [lockers, setLockers] = useState([]);
  const [search, setSearch] = useState("");
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const triggerRef = useRef(null);

  const selected = useMemo(() => {
    if (!value) return null;
    return lockers.find((l) => String(l.id) === String(value.lockerId)) || value;
  }, [lockers, value]);

  useEffect(() => {
    if (!companyId || !courierType) return;
    setLoading(true); setError(null);
    lockerApi.getLockers(companyId, courierType)
      .then(setLockers)
      .catch(e => setError(e.message))
      .finally(() => setLoading(false));
  }, [companyId, courierType]);

  const filtered = lockers.filter((l) => {
    const term = search.toLowerCase();
    return !term || [l.name, l.street, l.city, l.postalCode].some((v) => v?.toLowerCase().includes(term));
  });

  const select = (locker) => {
    setSearch("");
    setOpen(false);

    onChange?.({
      ...locker,
      lockerId: locker.id,
      lat: Number(locker.lat),
      lng: Number(locker.lng)
    });
  };

  if (loading) return <div className="sf-locker-state"><div className="sf-locker-spinner" /><span>Kraunami paštomatai…</span></div>;
  if (error) return <div className="sf-locker-state sf-locker-state--error">⚠ {error}</div>;

  return (
    <div className="sf-locker-wrap">
      <button ref={triggerRef} type="button"
        className={`sf-locker-trigger${open ? " is-open" : ""}${selected ? " has-value" : ""}`}
        onClick={() => { setOpen((o) => !o); if (!open) setSearch(""); }}>
        {selected ? (
          <span className="sf-locker-trigger-value">
            <span className="sf-locker-trigger-name">{selected.name}</span>
            <span className="sf-locker-trigger-addr">{[selected.street, selected.city].filter(Boolean).join(", ")}</span>
          </span>
        ) : <span className="sf-locker-trigger-placeholder">— Pasirinkite paštomatą —</span>}
        <span className="sf-locker-chevron">{open ? "▲" : "▼"}</span>
      </button>

      {open && (
        <LockerPortalPanel triggerRef={triggerRef} onClose={() => setOpen(false)}>
          <div className="sf-locker-search-wrap">
            <input autoFocus className="sf-locker-search" placeholder="Ieškoti pagal miestą ar adresą…"
              value={search} onChange={(e) => setSearch(e.target.value)} />
            {search && <button className="sf-locker-clear" type="button" onClick={() => setSearch("")}>✕</button>}
          </div>
          <div className="sf-locker-options">
            {filtered.length === 0
              ? <div className="sf-locker-empty">Nerasta paštomatų</div>
              : filtered.map((l) => (
                <div key={l.id}
                  className={`sf-locker-option${selected?.id === l.id ? " is-selected" : ""}`}
                  onMouseDown={(e) => { e.preventDefault(); select(l); }}>
                  <div className="sf-locker-option-row">
                    <span className="sf-locker-option-name">{l.name}</span>
                    <span className="sf-locker-option-badge">{l.lockerType === "PickupStation" ? "Paštomatas" : "Siuntų taškas"}</span>
                  </div>
                  <div className="sf-locker-option-addr">{[l.street, l.city, l.postalCode].filter(Boolean).join(", ")}</div>
                </div>
              ))}
          </div>
          <div className="sf-locker-footer">{filtered.length} iš {lockers.length} taškų</div>
        </LockerPortalPanel>
      )}

      <div className="sf-locker-map-wrap">
        <LockerMap locker={selected} />

        {selected && (
          <div className="sf-locker-caption">
            📍 <strong>{selected.name}</strong>{" — "}
            {[selected.street, selected.city, selected.postalCode].filter(Boolean).join(", ")}
          </div>
        )}
      </div>

    </div>
  );
}

// ─── ProductViewWidget ────────────────────────────────────────────────────────
function ProductViewWidget({ items }) {
  if (!items?.length) return <div className="sf-pv-empty">Nėra prekių</div>;
  const total = items.reduce((s, it) => s + (it.quantity ?? 1) * (it.unitPrice ?? 0) + (it.vatValue ?? 0), 0);
  return (
    <div className="sf-pv-list">
      {items.map((it, idx) => {
        const product = it.product ?? {};
        const images = product.images ?? [];
        const primary = images.find((i) => i.isPrimary) ?? images[0];
        const imgSrc = primary?.url ? (primary.url.startsWith("http") ? primary.url : `${ASSET_BASE}${primary.url}`) : null;
        return (
          <div key={it.id_OrdersProduct ?? idx} className="sf-pv-row">
            <div className="sf-pv-img-wrap">{imgSrc ? <img src={imgSrc} alt={product.name} className="sf-pv-img" /> : <div className="sf-pv-img-ph">📷</div>}</div>
            <div className="sf-pv-info">
              <span className="sf-pv-name">{product.name ?? "—"}</span>
              <span className="sf-pv-meta">
                {it.quantity} {product.unit ?? "vnt"} × €{Number(it.unitPrice ?? 0).toFixed(2)}
                {it.vatValue ? <span className="sf-pv-vat"> (+€{Number(it.vatValue).toFixed(2)} PVM)</span> : null}
              </span>
            </div>
            <div className="sf-pv-total">€{(Number(it.quantity ?? 1) * Number(it.unitPrice ?? 0)).toFixed(2)}</div>
          </div>
        );
      })}
      <div className="sf-pv-footer"><span>Iš viso</span><span className="sf-pv-sum">€{total.toFixed(2)}</span></div>
    </div>
  );
}

// ─── SmartForm ────────────────────────────────────────────────────────────────
export default function SmartForm({
  fields, initialValues = {}, patchValues = null,
  submitLabel = "Save", cancelLabel = "Cancel",
  onSubmit, onCancel, onValuesChange, validate, customFieldRenderers = {}, logoUploaderContext = {},
}) {
  const [values, setValues] = useState(() => ({ ...initialValues }));
  const [touched, setTouched] = useState({});
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => { setValues({ ...initialValues }); setTouched({}); }, [initialValues]);

  useEffect(() => {
    if (!patchValues) return;
    setValues((prev) => {
      let changed = false; const next = { ...prev };
      for (const [k, v] of Object.entries(patchValues)) {
        if (touched[k]) continue;
        if (next[k] !== v) { next[k] = v; changed = true; }
      }
      return changed ? next : prev;
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [patchValues]);

  // FIXED: Call onValuesChange in useEffect after state update, not during
  const prevValuesRef = useRef(values);
  useEffect(() => {
    if (prevValuesRef.current !== values) {
      prevValuesRef.current = values;
      onValuesChange?.(values);
    }
  }, [values, onValuesChange]);

  const setField = (name, value) => setValues((prev) => ({ ...prev, [name]: value }));
  const markTouched = (name) => setTouched((p) => ({ ...p, [name]: true }));

  const setArrayRowField = (arrName, idx, fieldName, value) =>
    setValues((prev) => {
      const arr = Array.isArray(prev[arrName]) ? prev[arrName] : [];
      return { ...prev, [arrName]: arr.map((row, i) => i === idx ? { ...row, [fieldName]: value } : row) };
    });

  const addArrayRow = (arrName, emptyRow) =>
    setValues((prev) => {
      const arr = Array.isArray(prev[arrName]) ? prev[arrName] : [];
      return { ...prev, [arrName]: [...arr, { ...emptyRow }] };
    });

  const removeArrayRow = (arrName, idx, minRows = 0) =>
    setValues((prev) => {
      const arr = Array.isArray(prev[arrName]) ? prev[arrName] : [];
      if (arr.length <= minRows) return prev;
      return { ...prev, [arrName]: arr.filter((_, i) => i !== idx) };
    });

  // Built-in field validation (from field.required and field.validate)
  // Built-in field validation (from field.required and field.validate)
  const builtInErrors = useMemo(() => {
    const next = {};
    const isEmpty = (v) =>
      v == null ||
      v === "" ||
      (Array.isArray(v) && v.length === 0) ||
      (typeof v === "number" && Number.isNaN(v));

    for (const f of fields) {
      const visible = f.visible ? f.visible(values) : true;
      if (!visible) continue;

      // Handle array fields
      if (f.type === "array") {
        const arr = Array.isArray(values[f.name]) ? values[f.name] : [];
        if (f.required && arr.length === 0) {
          next[f.name] = "Privaloma įvesti";
        }
        // Validate each row
        arr.forEach((row, idx) => {
          (f.rowFields ?? []).forEach((rf) => {
            if (!(rf.visible ? rf.visible(row, values) : true)) return;
            const v = row?.[rf.name];
            if (rf.required && isEmpty(v)) {
              next[`${f.name}[${idx}].${rf.name}`] = "Privaloma įvesti";
              return;
            }
            if (typeof rf.validate === "function") {
              const msg = rf.validate(v, row, values);
              if (msg) next[`${f.name}[${idx}].${rf.name}`] = msg;
            }
          });
        });
        continue;
      }

      // Skip special types that don't need validation
      if (f.type === "section" || f.type === "spacer" || f.type === "map"
        || f.type === "product-view" || f.type === "display"
        || f.type === "locker-picker" || f.type === "courier-cards"
        || f.type === "packages" || f.type === "logo-uploader") continue;

      if (!f.name) continue;

      const v = values[f.name];
      if (f.required && isEmpty(v)) {
        next[f.name] = "Privaloma įvesti";
        continue;
      }
      if (typeof f.validate === "function") {
        const msg = f.validate(v, values);
        if (msg) next[f.name] = msg;
      }
    }
    return next;
  }, [fields, values]);

  // Custom validation (from validate prop)
  const customErrors = useMemo(() => {
    return validate ? validate(values) : {};
  }, [validate, values]);

  // Combine ALL errors
  const allErrors = useMemo(() => {
    return { ...builtInErrors, ...customErrors };
  }, [builtInErrors, customErrors]);

  // ✅ FIXED: Check ALL errors before allowing submit
  const canSubmit = useMemo(() => {
    return Object.keys(allErrors).length === 0;
  }, [allErrors]);



  const handleSubmit = async (e) => {
    e.preventDefault();

    // Mark all fields as touched
    const t = {};
    fields.forEach((f) => {
      if (f.type === "array") {
        const arr = Array.isArray(values[f.name]) ? values[f.name] : [];
        (f.rowFields ?? []).forEach((rf) => {
          arr.forEach((_, idx) => {
            if (rf.name) t[`${f.name}[${idx}].${rf.name}`] = true;
          });
        });
      }
      else if (f.name) t[f.name] = true;
    });

    // Mark all error fields as touched
    setTouched((prev) => {
      const touched = { ...prev, ...t };
      Object.keys(allErrors).forEach((k) => (touched[k] = true));
      return touched;
    });

    // ✅ PREVENT submission if ANY errors exist
    if (Object.keys(allErrors).length > 0) {
      console.warn("Form validation failed:", allErrors);

      // Optional: Scroll to first error
      const firstErrorField = Object.keys(allErrors)[0];
      const element = document.querySelector(`[name="${firstErrorField}"]`);
      if (element) {
        element.scrollIntoView({ behavior: 'smooth', block: 'center' });
        element.focus();
      }

      return; // Stop here - don't submit
    }

    try {
      setSubmitting(true);
      await onSubmit?.(values);
    }
    catch (error) {
      console.error("Form submission error:", error);
      alert(error.message || "Nepavyko išsaugoti. Bandykite dar kartą.");
    }
    finally {
      setSubmitting(false);
    }
  };

  const renderInput = (f, value, err, disabled, onChange, onBlur) => {
    if (customFieldRenderers[f.type]) return customFieldRenderers[f.type](f, value, onChange);
    if (f.type === "locker-picker") return <LockerPickerWidget companyId={f.companyId} courierType={f.courierType} value={value} onChange={onChange} />;

    // ── Courier cards ─────────────────────────────────────────────────────
    if (f.type === "courier-cards") {
      const couriers = f.couriers ?? [];
      const badgeMap = {
        DPD_PARCEL: { label: "Paštomatas", color: "#e63946" },
        DPD_HOME: { label: "Pristatymas", color: "#e63946" },
      };
      const badge = (type) => {
        if (badgeMap[type]) return badgeMap[type];
        if (type?.includes("EXPRESS")) return { label: "LP Express", color: "#2b7a0b" };
        if (type?.includes("OMNIVA")) return { label: "Omniva", color: "#f4a261" };
        return { label: "Kurjeris", color: "#4361ee" };
      };
      return (
        <div className="sf-courier-cards">
          {couriers.map((c) => {
            const id = c.id ?? c.id_Courier;
            const b = badge(c.type);
            const active = value === id;
            return (
              <button key={id} type="button"
                className={`sf-courier-card${active ? " is-active" : ""}`}
                onClick={() => { onChange(id); if (f.onSelect) f.onSelect(c); }}>
                {active && <span className="sf-courier-check">✓</span>}
                <span className="sf-courier-badge" style={{ background: b.color }}>{b.label}</span>
                <span className="sf-courier-name">{c.name}</span>
                <div className="sf-courier-meta">
                  {c.deliveryPrice != null && <span>€{Number(c.deliveryPrice).toFixed(2)}</span>}
                  {c.deliveryTermDays && <span>{c.deliveryTermDays} d.</span>}
                  {c.isOwn && <span className="sf-courier-own">★ Jūsų</span>}
                </div>
              </button>
            );
          })}
        </div>
      );
    }
    if (f.type === "logo-uploader") {
      const { isCreate, companyId: ctxCompanyId } = logoUploaderContext;
      return (
        <LogoUploaderWidget
          currentUrl={value}
          companyId={f.companyId ?? ctxCompanyId}
          isCreate={isCreate}
          onPendingFile={f.onPendingFile}
          onChange={onChange}
        />
      );
    }
    // ── Packages widget ───────────────────────────────────────────────────
    if (f.type === "packages") {
      const list = Array.isArray(value) ? value : [{ weight: "" }];
      const add = () => onChange([...list, { weight: "" }]);
      const remove = (i) => list.length > 1 && onChange(list.filter((_, idx) => idx !== i));
      const setW = (i, w) => onChange(list.map((p, idx) => idx === i ? { ...p, weight: w } : p));
      return (
        <div className="sf-packages">
          <div className="sf-packages-header">
            <button type="button" className="sf-pkg-add" onClick={add}>+ Pridėti pakuotę</button>
          </div>
          {list.map((pkg, i) => (
            <div key={i} className="sf-package-row">
              <span className="sf-package-num">{i + 1}</span>
              <div className="sf-field sf-package-weight">
                <label className="sf-label">Svoris (kg)</label>
                <input className="sf-input" type="number" min="0.1" step="0.1" placeholder="1.0"
                  value={pkg.weight} onChange={(e) => setW(i, e.target.value)} />
              </div>
              {list.length > 1 && (
                <button type="button" className="sf-pkg-remove" onClick={() => remove(i)}><FiTrash2 /></button>
              )}
            </div>
          ))}
        </div>
      );
    }
    if (f.type === "map") { const addr = typeof f.getAddress === "function" ? f.getAddress(values) : (f.address ?? ""); return <MapWidget address={addr} />; }
    if (f.type === "product-view") { const items = typeof f.getItems === "function" ? f.getItems(values) : (value ?? []); return <ProductViewWidget items={items} />; }
    if (f.type === "display") { const content = typeof f.render === "function" ? f.render(value, values) : value; return <div className={`sf-display ${!content ? "is-placeholder" : ""}`}>{content || f.placeholder || "—"}</div>; }
    if (f.type === "file") return <input className={`sf-input ${err ? "is-error" : ""}`} type="file" multiple={!!f.multiple} accept={f.accept ?? "image/*"} disabled={disabled} onBlur={onBlur} onChange={(e) => { const files = Array.from(e.target.files ?? []); onChange(f.multiple ? files : (files[0] ?? null)); }} />;
    if (f.type === "images") {
      const list = Array.isArray(value) ? value : [];
      const move = (from, to) => { if (to < 0 || to >= list.length) return; const copy = [...list]; const [item] = copy.splice(from, 1); copy.splice(to, 0, item); onChange(copy); };
      const removeAt = (idx) => { const copy = [...list]; copy.splice(idx, 1); onChange(copy); };
      const addFiles = (files) => onChange([...list, ...files.map((file, i) => ({ type: "new", file, tempKey: `${Date.now()}-${i}-${file.name}`, previewUrl: URL.createObjectURL(file) }))]);
      return (
        <div className="sf-images">
          <input className={`sf-input ${err ? "is-error" : ""}`} type="file" multiple accept={f.accept ?? "image/*"} disabled={disabled} onBlur={onBlur} onChange={(e) => { const files = Array.from(e.target.files ?? []); if (files.length) addFiles(files); e.target.value = ""; }} />
          {list.length === 0 ? <div className="sf-images-empty">Nuotraukų nėra</div> : (
            <div className="sf-images-grid">
              {list.map((img, idx) => (
                <div key={img.type === "existing" ? `ex-${img.id}` : img.tempKey} className="sf-img-card">
                  <div className="sf-img-top"><div className="sf-img-index">{idx + 1}</div>{idx === 0 ? <div className="sf-img-badge">Pagrindinė</div> : null}</div>
                  <img className="sf-img-thumb" src={img.type === "existing" ? (img.url?.startsWith("http") ? img.url : `${ASSET_BASE}${img.url}`) : img.previewUrl} alt={`img-${idx}`} />
                  <div className="sf-img-actions">
                    <button type="button" className="sf-btn sf-btn-ghost" onClick={() => move(idx, idx - 1)} disabled={idx === 0}><FaArrowLeft /></button>
                    <button type="button" className="sf-btn sf-btn-ghost" onClick={() => move(idx, idx + 1)} disabled={idx === list.length - 1}><FaArrowRight /></button>
                    <button type="button" className="sf-btn sf-btn-ghost danger" onClick={() => removeAt(idx)}>Ištrinti</button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      );
    }
    if (f.type === "textarea") return <textarea className={`sf-input ${err ? "is-error" : ""}`} placeholder={f.placeholder} value={value} disabled={disabled} onBlur={onBlur} onChange={(e) => onChange(e.target.value)} rows={4} />;
    if (f.type === "select") {
      return (
        <select className={`sf-input ${err ? "is-error" : ""}`} value={value} disabled={disabled} onBlur={onBlur} onChange={(e) => onChange(e.target.value)}>
          <option value="">— Pasirinkite —</option>
          {(f.options ?? []).map((opt) => <option key={String(opt.value)} value={opt.value}>{opt.label}</option>)}
        </select>
      );
    }
    if (f.type === "searchselect") return <SearchSelect value={value} options={f.options ?? []} placeholder={f.placeholder ?? "Pasirinkite..."} onChange={(val) => onChange(val)} />;
    if (f.type === "checkbox") return <label className="sf-check"><input type="checkbox" checked={!!value} disabled={disabled} onChange={(e) => onChange(e.target.checked)} onBlur={onBlur} /><span>{f.help ?? ""}</span></label>;
    return (
      <input
        className={`sf-input ${err ? "is-error" : ""}`}
        type={f.type || "text"}
        step={f.type === "number" ? "any" : undefined}
        min={f.type === "number" ? 0 : undefined}
        placeholder={f.placeholder}
        value={value}
        disabled={disabled}
        onBlur={onBlur}
        onChange={(e) => {
          const raw = e.target.value;
          if (f.type === "number") {
            onChange(raw === "" ? "" : Math.max(0, Number(raw)));
          } else {
            onChange(raw);
          }
        }}
      />
    );
  };

  return (
    <form className="sf" onSubmit={handleSubmit}>
      <div className="sf-grid">
        {fields.map((f, idx) => {
          const show = f.visible ? f.visible(values) : true;
          if (!show) return null;
          if (f.type === "section") {
            return (
              <div key={`section-${idx}`} className="sf-block span-2">
                <div className="sf-section-title">{f.title}</div>
                {f.subtitle ? <div className="sf-section-sub">{f.subtitle}</div> : null}
                <div className="sf-divider span-2" />
              </div>
            );
          }
          if (f.type === "logo-uploader") {
            return (
              <div key={f.name ?? `logo-${idx}`} className="sf-field span-2">
                {f.label ? <label className="sf-label">{f.label}</label> : null}
                <LogoUploaderWidget
                  currentUrl={values[f.name]}
                  companyId={f.companyId ?? logoUploaderContext.companyId}
                  isCreate={logoUploaderContext.isCreate}
                  onPendingFile={f.onPendingFile}
                  onChange={(val) => setField(f.name, val)}
                />
              </div>
            );
          }
          if (f.type === "spacer") return <div key={`spacer-${idx}`} className={`sf-field ${f.colSpan === 2 ? "span-2" : ""}`} />;
          if (f.type === "array") {
            const arr = Array.isArray(values[f.name]) ? values[f.name] : [];
            const rowFields = f.rowFields ?? []; const minRows = f.minRows ?? 0;
            const arrayErr = touched[f.name] ? allErrors[f.name] : null;
            return (
              <div key={f.name} className="sf-field span-2">
                <label className="sf-label">{f.label}{f.required ? <span className="sf-req">*</span> : null}</label>
                {arrayErr ? <div className="sf-error">{arrayErr}</div> : null}
                <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
                  {arr.map((row, rowIdx) => (
                    <div key={`${f.name}-row-${rowIdx}`} style={{ display: "grid", gridTemplateColumns: "1fr 1fr auto", gap: 10, alignItems: "start" }}>
                      <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr 1fr 1fr auto", gap: 10 }}>
                        {rowFields.map((rf) => {
                          if (!(rf.visible ? rf.visible(row, values) : true)) return null;
                          const key = `${f.name}[${rowIdx}].${rf.name}`;
                          const rowErr = touched[key] ? allErrors[key] : null;
                          const dis = rf.disabled ? rf.disabled(row, values) : false;
                          return (
                            <div key={key} className={`sf-field ${rf.colSpan === 2 ? "span-2" : ""}`} style={rf.colSpan === 2 ? { gridColumn: "span 2" } : undefined}>
                              <label className="sf-label">{rf.label}{rf.required ? <span className="sf-req">*</span> : null}</label>
                              {renderInput(rf, row?.[rf.name] ?? "", rowErr, dis, (val) => setArrayRowField(f.name, rowIdx, rf.name, val), () => setTouched((p) => ({ ...p, [key]: true })))}
                              {rowErr ? <div className="sf-error">{rowErr}</div> : null}
                            </div>
                          );
                        })}
                      </div>
                      <button type="button" className="sf-btn sf-btn-ghost" style={{ height: 42, marginTop: 22 }} onClick={() => removeArrayRow(f.name, rowIdx, minRows)} disabled={arr.length <= minRows}>Šalinti</button>
                    </div>
                  ))}
                  <button type="button" className="sf-btn sf-btn-ghost" onClick={() => addArrayRow(f.name, f.emptyRow ?? {})}>{f.addLabel ?? "+ Pridėti"}</button>
                </div>
              </div>
            );
          }
          if (customFieldRenderers[f.type]) {
            return (
              <div key={f.name ?? `custom-${idx}`} className={`sf-field ${f.colSpan === 2 ? "span-2" : ""}`}>
                {f.label ? <label className="sf-label">{f.label}</label> : null}
                {customFieldRenderers[f.type](f, values[f.name], (val) => setField(f.name, val))}
              </div>
            );
          }
          if (f.type === "locker-picker") {
            return (
              <div key={f.name ?? `locker-${idx}`} className="sf-field span-2">
                {f.label ? <label className="sf-label">{f.label}</label> : null}
                <LockerPickerWidget companyId={f.companyId} courierType={f.courierType} value={values[f.name]} onChange={(val) => setField(f.name, val)} />
              </div>
            );
          }
          if (f.type === "courier-cards") {
            return (
              <div key={f.name ?? `cc-${idx}`} className="sf-field span-2">
                {f.label ? <label className="sf-label">{f.label}</label> : null}
                {renderInput(f, values[f.name], null, false, (val) => setField(f.name, val), () => { })}
              </div>
            );
          }
          if (f.type === "packages") {
            return (
              <div key={f.name ?? `pkg-${idx}`} className="sf-field span-2">
                {f.label ? <label className="sf-label">{f.label}</label> : null}
                {renderInput(f, values[f.name], null, false, (val) => setField(f.name, val), () => { })}
              </div>
            );
          }
          if (f.type === "map") { const addr = typeof f.getAddress === "function" ? f.getAddress(values) : (f.address ?? ""); return <div key={`map-${idx}`} className="sf-field span-2">{f.label ? <label className="sf-label">{f.label}</label> : null}<MapWidget address={addr} /></div>; }
          if (f.type === "product-view") { const items = typeof f.getItems === "function" ? f.getItems(values) : (values[f.name] ?? []); return <div key={`pv-${idx}`} className="sf-field span-2">{f.label ? <label className="sf-label">{f.label}</label> : null}<ProductViewWidget items={items} /></div>; }
          if (!f.name) return null;
          const disabled = f.disabled ? f.disabled(values) : false;
          const value = typeof f.getValue === "function" ? f.getValue(values) : (values[f.name] ?? "");
          const err = touched[f.name] ? allErrors[f.name] : null;
          return (
            <div key={f.name} className={`sf-field ${f.colSpan === 2 ? "span-2" : ""}`}>
              {f.label ? <label className="sf-label">{f.label}{f.required ? <span className="sf-req">*</span> : null}</label> : null}
              {renderInput(f, value, err, disabled, (val) => setField(f.name, val), () => markTouched(f.name))}
              {err ? <div className="sf-error">{err}</div> : null}
            </div>
          );
        })}
      </div>
      <div className="sf-actions">
        <button type="button" className="sf-btn sf-btn-ghost" onClick={onCancel}>{cancelLabel}</button>
        <button type="submit" className="sf-btn" disabled={!canSubmit || submitting}>{submitting ? "Išsaugoma..." : submitLabel}</button>
      </div>
    </form>
  );
}
