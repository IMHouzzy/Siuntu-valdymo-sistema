// pages/client/ClientOrdersPage.jsx
import { useState, useEffect, useCallback } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import {
  FiPackage, FiChevronDown, FiChevronRight, FiEdit2,
  FiRotateCcw, FiTruck, FiCheckCircle, FiClock,
  FiAlertCircle, FiXCircle, FiBox,
  FiMapPin, FiPhone, FiUser, FiCalendar, FiShoppingBag,
  FiX, FiSave, FiExternalLink, FiFileText,
} from "react-icons/fi";
import { createPortal } from "react-dom";
import React from "react";
// FIXED: correct path — ReturnFormModal lives in components/, not in pages/client/
import ReturnFormModal from "./ReturnFormModal";
import "../styles/ClientOrdersPage.css";
import '../styles/validation-styles.css';
import { lockerApi, clientApi, companiesApi } from "../services/api";

const API = (process.env.REACT_APP_API_URL || "/api").replace(/\/api\/?$/, "");

// ── Status helpers ────────────────────────────────────────────────────────────
const ORDER_STATUS = {
  1: { label: "Laukia patvirtinimo", color: "var(--color-warning)", icon: FiClock },
  2: { label: "Atšauktas", color: "var(--color-danger)", icon: FiXCircle },
  3: { label: "Įvykdytas", color: "var(--color-accent)", icon: FiCheckCircle },
  4: { label: "Vykdomas", color: "var(--color-secondary)", icon: FiPackage },
  5: { label: "Išsiųstas", color: "var(--color-primary)", icon: FiTruck },
};

const RETURN_STATUS = {
  1: { label: "Sukurtas", color: "var(--color-secondary)", icon: FiClock },
  2: { label: "Įvykdytas", color: "var(--color-accent)", icon: FiCheckCircle },
  3: { label: "Vertinamas", color: "var(--color-warning)", icon: FiAlertCircle },
  4: { label: "Patvirtintas", color: "var(--color-accent)", icon: FiCheckCircle },
  5: { label: "Atmestas", color: "var(--color-danger)", icon: FiXCircle },
  6: { label: "Etiketės paruoštos", color: "var(--color-primary)", icon: FiFileText },
};

function getOrderStatus(id) { return ORDER_STATUS[id] ?? { label: String(id), color: "var(--color-text-muted)", icon: FiClock }; }
function getReturnStatus(id) {
  const statuses = {
    1: { label: "Sukurtas", color: "var(--color-secondary)", icon: FiClock },
    2: { label: "Vertinamas", color: "var(--color-warning)", icon: FiAlertCircle },
    3: { label: "Gauta", color: "var(--color-secondary)", icon: FiPackage },
    4: { label: "Užbaigta", color: "var(--color-accent)", icon: FiCheckCircle },
    5: { label: "Patvirtintas", color: "var(--color-accent)", icon: FiCheckCircle },
    6: { label: "Atmestas", color: "var(--color-danger)", icon: FiXCircle },
    7: { label: "Etiketės paruoštos", color: "var(--color-primary)", icon: FiFileText },
  };
  return statuses[id] ?? RETURN_STATUS[id] ?? { label: String(id), color: "var(--color-text-muted)", icon: FiClock };
}

function fmtDate(d) {
  if (!d) return "—";
  const dt = new Date(d);
  return isNaN(dt) ? "—" : dt.toLocaleDateString("lt-LT");
}
function fmtDateTime(d) {
  if (!d) return "—";
  return new Date(d).toLocaleString("lt-LT", {
    year: "numeric", month: "2-digit", day: "2-digit",
    hour: "2-digit", minute: "2-digit",
  });
}
function fmtEur(v) { return `€${Number(v ?? 0).toFixed(2)}`; }

// ── Status badge ──────────────────────────────────────────────────────────────
function StatusBadge({ statusId, type = "order" }) {
  const cfg = type === "return" ? getReturnStatus(statusId) : getOrderStatus(statusId);
  const Icon = cfg.icon;
  return (
    <span className="uh-badge" style={{ "--badge-color": cfg.color }}>
      <Icon size={11} /> {cfg.label}
    </span>
  );
}

function InfoRow({ icon: Icon, label, value }) {
  if (!value) return null;
  return (
    <div className="uh-info-row">
      <Icon size={13} className="uh-info-icon" />
      <span className="uh-info-label">{label}</span>
      <span className="uh-info-value">{value}</span>
    </div>
  );
}

// ── Edit Contact Modal ────────────────────────────────────────────────────────
function CourierCard({ courier, selected, onSelect }) {
  const id = courier.id_Courier;
  const isLocker = courier.supportsLockers;
  return (
    <button
      type="button"
      className={`ecm-courier-card${selected ? " ecm-courier-card--selected" : ""}`}
      onClick={() => onSelect(courier)}
    >
      <div className="ecm-courier-card-name">{courier.name}</div>
      <div className="ecm-courier-card-meta">
        {isLocker ? (
          <span className="ecm-courier-tag ecm-courier-tag--locker">
            <FiBox size={10} /> Paštomatas
          </span>
        ) : (
          <span className="ecm-courier-tag ecm-courier-tag--home">
            <FiMapPin size={10} /> Į namus
          </span>
        )}
        {courier.deliveryPrice != null && (
          <span className="ecm-courier-price">€{Number(courier.deliveryPrice).toFixed(2)}</span>
        )}
        {courier.deliveryTermDays != null && (
          <span className="ecm-courier-days">{courier.deliveryTermDays} d.</span>
        )}
      </div>
    </button>
  );
}

// Simple locker map picker for client
// Uses the same /api/companies/{id}/courier-provider/{type}/lockers endpoint
function ClientLockerPicker({ companyId, courierType, selected, onSelect }) {
  const [lockers, setLockers] = useState([]);
  const [loading, setLoading] = useState(false);
  const [search, setSearch] = useState("");
  const [error, setError] = useState(null);

  useEffect(() => {
    if (!companyId || !courierType) return;
    setLoading(true); setError(null);
    lockerApi.getLockers(companyId, courierType)
      .then(setLockers)
      .catch(e => setError(e.message))
      .finally(() => setLoading(false));
  }, [companyId, courierType]);

  const filtered = lockers.filter(l =>
    !search || l.name?.toLowerCase().includes(search.toLowerCase()) ||
    l.address?.toLowerCase().includes(search.toLowerCase()) ||
    l.city?.toLowerCase().includes(search.toLowerCase())
  );

  if (loading) return (
    <div className="ecm-locker-loading">
      <span className="uh-spinner-sm" /> Kraunami paštomatai…
    </div>
  );

  if (error) return (
    <div className="ecm-locker-error">
      <FiAlertCircle size={13} /> Nepavyko įkelti paštomatų: {error}
    </div>
  );

  return (
    <div className="ecm-locker-picker">
      <input
        className="uh-form-input ecm-locker-search"
        placeholder="Ieškoti paštomato..."
        value={search}
        onChange={e => setSearch(e.target.value)}
      />
      {selected && (
        <div className="ecm-locker-selected">
          <FiCheckCircle size={13} style={{ color: "var(--color-accent)" }} />
          <strong>Pasirinkta:</strong> {selected.name} — {selected.address}
        </div>
      )}
      <div className="ecm-locker-list">
        {filtered.length === 0 && (
          <div className="ecm-locker-empty">Paštomatų nerasta.</div>
        )}
        {filtered.map(l => (
          <button
            key={l.id ?? l.lockerId}
            type="button"
            className={`ecm-locker-item${selected?.id === (l.id ?? l.lockerId) ? " ecm-locker-item--selected" : ""}`}
            onClick={() => onSelect({
              id: l.id ?? l.lockerId,
              lockerId: l.id ?? l.lockerId,
              name: l.name,
              address: l.address,
              city: l.city,
              lat: l.lat ?? l.latitude,
              lng: l.lng ?? l.longitude,
            })}
          >
            <div className="ecm-locker-item-name">{l.name}</div>
            <div className="ecm-locker-item-addr">{l.address}{l.city ? `, ${l.city}` : ""}</div>
          </button>
        ))}
      </div>
    </div>
  );
}

function EditContactModal({ order, onClose, onSaved }) {
  const DELIVERY_TYPES = [
    { value: "HOME", label: "Į namus / biurą" },
    { value: "LOCKER", label: "Į paštomatą" },
  ];

  const initDeliveryType = order.snapshotDeliveryMethod === "LOCKER" ? "LOCKER" : "HOME";

  const [deliveryType, setDeliveryType] = useState(initDeliveryType);
  const [couriers, setCouriers] = useState([]);
  const [loadingCouriers, setLoadingCouriers] = useState(true);
  const [selectedCourierId, setSelectedCourierId] = useState(null); // ← store ID only, not object
  const [selectedLocker, setSelectedLocker] = useState(
    order.snapshotLockerId
      ? {
          id: order.snapshotLockerId,
          lockerId: order.snapshotLockerId,
          name: order.snapshotLockerName ?? "",
          address: order.snapshotLockerAddress ?? "",
        }
      : null
  );

  const [form, setForm] = useState({
    DeliveryAddress: order.snapshotDeliveryAddress || "",
    City: order.snapshotCity || "",
    Country: order.snapshotCountry || "",
    Phone: order.snapshotPhone || "",
  });

  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [fieldErrors, setFieldErrors] = useState({});

  const companyId = order.companyId ?? order.fk_Companyid_Company;

  // Load couriers

    useEffect(() => {
    setLoadingCouriers(true);
    companiesApi.getCouriers(companyId)
      .then(data => {
        const list = Array.isArray(data) ? data : [];
        
        // ── DEBUG: log what we get to find the correct ID field ───────────
        console.log("Couriers from API:", list);
        console.log("Order snapshotCourierId:", order.snapshotCourierId);

        const normalised = list.map(c => ({
          ...c,
          // Try every possible ID field the API might return
          id_Courier: c.id_Courier ?? c.id ?? c.courierId ?? c.Id,
          supportsLockers: c.supportsLockers ?? c.type?.endsWith("_PARCEL") ?? false,
        }));
        
        console.log("Normalised couriers:", normalised.map(c => ({ id_Courier: c.id_Courier, name: c.name })));
        
        setCouriers(normalised);

        if (order.snapshotCourierId) {
          // Compare as both number and string to handle type mismatches
          const snapshotId = order.snapshotCourierId;
          const pre = normalised.find(c => 
            c.id_Courier === snapshotId ||
            String(c.id_Courier) === String(snapshotId)
          );
          
          console.log("Pre-selected courier:", pre);
          
          if (pre) {
            setSelectedCourierId(pre.id_Courier);
            setDeliveryType(pre.supportsLockers ? "LOCKER" : "HOME");
          }
        }
      })
      .catch((e) => { console.error("Couriers load failed:", e); })
      .finally(() => setLoadingCouriers(false));
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [companyId]);

  // Derive the actual selected courier object from ID + current filtered list
  // This is the key fix: selectedCourier is always derived, never stale
  const filteredCouriers = couriers.filter(c =>
    deliveryType === "LOCKER" ? c.supportsLockers : !c.supportsLockers
  );

  // Derive selected courier — if it's not in filteredCouriers, it's null
  const selectedCourier = filteredCouriers.find(c => c.id_Courier === selectedCourierId) ?? null;

  // When delivery type changes, check if current selection is still valid
  // If not, clear it — but we do this via derivation above, so just clear the locker too
  const prevDeliveryTypeRef = React.useRef(deliveryType);
  useEffect(() => {
    if (prevDeliveryTypeRef.current !== deliveryType) {
      prevDeliveryTypeRef.current = deliveryType;
      // selectedCourier is already null if invalid (derived above)
      // but we need to clear the locker if switching away from LOCKER
      setSelectedLocker(null);
      // If the current ID is not valid for new type, clear it
      const stillValid = filteredCouriers.some(c => c.id_Courier === selectedCourierId);
      if (!stillValid) {
        setSelectedCourierId(null);
      }
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [deliveryType]);

  const validate = () => {
    const errors = {};
    if (!selectedCourier) errors.courier = "Pasirinkite kurjerį";
    if (deliveryType === "HOME") {
      if (!form.DeliveryAddress?.trim()) errors.DeliveryAddress = "Įveskite pristatymo adresą";
      if (!form.City?.trim()) errors.City = "Įveskite miestą";
      if (!form.Country?.trim()) errors.Country = "Įveskite šalį";
      if (form.Phone?.trim()) {
        const digits = form.Phone.replace(/\D/g, "");
        if (digits.length < 8) errors.Phone = "Telefono numeris per trumpas";
      }
    }
    if (deliveryType === "LOCKER" && !selectedLocker) errors.locker = "Pasirinkite paštomatą";
    return errors;
  };

  const save = async () => {
    const errors = validate();
    if (Object.keys(errors).length > 0) {
      setFieldErrors(errors);
      setError("Užpildykite visus privalomus laukus");
      return;
    }
    setSaving(true);
    setError("");
    setFieldErrors({});
    try {
      const dto = {
        DeliveryMethod: deliveryType,
        CourierId: selectedCourier?.id_Courier ?? null,
        Phone: form.Phone?.trim() || null,
        DeliveryAddress: deliveryType === "HOME" ? form.DeliveryAddress?.trim() : null,
        City: deliveryType === "HOME" ? form.City?.trim() : null,
        Country: deliveryType === "HOME" ? form.Country?.trim() : null,
        LockerId: deliveryType === "LOCKER" ? selectedLocker?.lockerId : null,
        LockerName: deliveryType === "LOCKER" ? selectedLocker?.name : null,
        LockerAddress: deliveryType === "LOCKER" ? selectedLocker?.address : null,
        DeliveryLat: deliveryType === "LOCKER" ? selectedLocker?.lat : null,
        DeliveryLng: deliveryType === "LOCKER" ? selectedLocker?.lng : null,
      };
      await clientApi.updateContact(order.id_Orders, dto);
      onSaved();
      onClose();
    } catch (err) {
      setError(err.message ?? "Serverio klaida. Bandykite dar kartą.");
    } finally {
      setSaving(false);
    }
  };

  const updateField = (key, value) => {
    setForm(f => ({ ...f, [key]: value }));
    if (fieldErrors[key]) setFieldErrors(e => ({ ...e, [key]: null }));
  };

  const field = (key, label, placeholder, required = false) => (
    <div className="uh-form-field">
      <label className="uh-form-label">
        {label}
        {required && <span className="uh-required">*</span>}
      </label>
      <input
        className={`uh-form-input${fieldErrors[key] ? " uh-form-input--error" : ""}`}
        value={form[key]}
        onChange={e => updateField(key, e.target.value)}
        placeholder={placeholder}
      />
      {fieldErrors[key] && (
        <div className="uh-field-error">
          <FiAlertCircle size={11} /> {fieldErrors[key]}
        </div>
      )}
    </div>
  );

  return createPortal(
    <div
      className="uh-modal-overlay"
      onClick={e => e.target === e.currentTarget && onClose()}
    >
      <div className="uh-modal uh-modal--wide">
        <div className="uh-modal-header">
          <span className="uh-modal-title">
            <FiTruck size={15} /> Pasirinkite pristatymą
          </span>
          <button className="uh-modal-close" onClick={onClose}><FiX size={16} /></button>
        </div>

        <div className="uh-modal-body">
          <p className="uh-modal-note">
            Pasirinkite pristatymo būdą ir užpildykite pristatymo duomenis.
            Jūsų profilio adresas liks nepakitęs.
          </p>

          {/* ── Delivery type toggle ──────────────────────────── */}
          <div className="ecm-section-label">
            <FiTruck size={13} /> Pristatymo būdas
          </div>
          <div className="ecm-type-toggle">
            {DELIVERY_TYPES.map(dt => (
              <button
                key={dt.value}
                type="button"
                className={`ecm-type-btn${deliveryType === dt.value ? " ecm-type-btn--active" : ""}`}
                onClick={() => setDeliveryType(dt.value)}
              >
                {dt.value === "HOME" ? <FiMapPin size={14} /> : <FiBox size={14} />}
                {dt.label}
              </button>
            ))}
          </div>

          {/* ── Courier selection ─────────────────────────────── */}
          <div className="ecm-section-label">
            <FiTruck size={13} /> Kurjeris
            <span className="uh-required">*</span>
          </div>

          {loadingCouriers ? (
            <div className="ecm-loading">
              <span className="uh-spinner-sm" /> Kraunami kurjeriai…
            </div>
          ) : filteredCouriers.length === 0 ? (
            <div className="ecm-empty-couriers">
              Šiam pristatymo būdui kurjerių nerasta.
            </div>
          ) : (
            <div className="ecm-courier-cards">
              {filteredCouriers.map(c => (
                <CourierCard
                  key={c.id_Courier}
                  courier={c}
                  // ← uses derived selectedCourier, so only one can ever be selected
                  selected={selectedCourier?.id_Courier === c.id_Courier}
                  onSelect={courier => {
                    setSelectedCourierId(courier.id_Courier);
                    setSelectedLocker(null);
                    if (fieldErrors.courier) {
                      setFieldErrors(e => ({ ...e, courier: null }));
                    }
                  }}
                />
              ))}
            </div>
          )}
          {fieldErrors.courier && (
            <div className="uh-field-error">
              <FiAlertCircle size={11} /> {fieldErrors.courier}
            </div>
          )}

          {/* ── Home delivery address ─────────────────────────── */}
          {deliveryType === "HOME" && (
            <>
              <div className="ecm-section-label" style={{ marginTop: 16 }}>
                <FiMapPin size={13} /> Pristatymo adresas
              </div>
              {field("DeliveryAddress", "Gatvė, namas, butas", "pvz. Studentų g. 50", true)}
              {field("City", "Miestas", "pvz. Vilnius", true)}
              {field("Country", "Šalis", "Lietuva", true)}
              {field("Phone", "Telefono numeris", "+370...")}
            </>
          )}

          {/* ── Locker picker ─────────────────────────────────── */}
          {deliveryType === "LOCKER" && selectedCourier && (
            <>
              <div className="ecm-section-label" style={{ marginTop: 16 }}>
                <FiBox size={13} /> Paštomatas
                <span className="uh-required">*</span>
              </div>
              <ClientLockerPicker
                companyId={companyId}
                courierType={selectedCourier.type}
                selected={selectedLocker}
                onSelect={locker => {
                  setSelectedLocker(locker);
                  if (fieldErrors.locker) {
                    setFieldErrors(e => ({ ...e, locker: null }));
                  }
                }}
              />
              {fieldErrors.locker && (
                <div className="uh-field-error">
                  <FiAlertCircle size={11} /> {fieldErrors.locker}
                </div>
              )}
              <div style={{ marginTop: 12 }}>
                {field("Phone", "Telefono numeris (kurjerio pranešimams)", "+370...")}
              </div>
            </>
          )}

          {deliveryType === "LOCKER" && !selectedCourier && (
            <div className="ecm-locker-hint">
              <FiAlertCircle size={13} /> Pirmiausia pasirinkite kurjerį.
            </div>
          )}

          {error && (
            <div className="uh-form-error">
              <FiAlertCircle size={13} /> {error}
            </div>
          )}
        </div>

        <div className="uh-modal-footer">
          <button className="uh-btn uh-btn--ghost" onClick={onClose} disabled={saving}>
            Atšaukti
          </button>
          <button className="uh-btn uh-btn--primary" onClick={save} disabled={saving}>
            {saving
              ? <span className="uh-spinner-sm" />
              : <><FiSave size={13} /> Išsaugoti</>
            }
          </button>
        </div>
      </div>
    </div>,
    document.body
  );
}

// ── Order Detail Panel ────────────────────────────────────────────────────────
function OrderDetail({ orderId, onReturnCreated }) {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [editContact, setEditContact] = useState(false);
  const [showReturn, setShowReturn] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try { setData(await clientApi.getOrderDetail(orderId)); }
    catch (err) { console.error("Order detail load failed:", err); }
    finally { setLoading(false); }
  }, [orderId]);

  useEffect(() => { load(); console.log("Loaded order detail:", data); }, [load]);

  if (loading) return <div className="uh-detail-loading"><span className="uh-spinner" /></div>;
  if (!data) return <div className="uh-detail-empty">Nepavyko įkelti užsakymo.</div>;

  const {
    totalAmount, deliveryPrice, paymentMethod, statusName,
    snapshotDeliveryAddress, snapshotCity, snapshotCountry, snapshotPhone,
    snapshotDeliveryMethod, snapshotLockerName, snapshotLockerAddress,
    products, shipment, existingReturn, canReturn, hasLabels,
  } = data;

  // FIXED: properly distinguish locker vs home delivery for client view
  const isLocker = snapshotDeliveryMethod === "LOCKER";

  return (
    <div className="uh-detail">

      {/* ── Order info ───────────────────────────────────────── */}
      <div className="uh-detail-section">
        <div className="uh-detail-section-header"><FiCalendar size={13} /> Užsakymo informacija</div>
        <div className="uh-info-rows">
          <InfoRow icon={FiCalendar} label="Data" value={fmtDate(data.ordersDate)} />
          <InfoRow icon={FiPackage} label="Statusas" value={statusName} />
          {paymentMethod && <InfoRow icon={FiShoppingBag} label="Apmokėjimas" value={paymentMethod} />}
        </div>
      </div>

      {/* ── Delivery address ─────────────────────────────────── */}
      <div className="uh-detail-section">
        <div className="uh-detail-section-header">
          <FiUser size={13} />
          {isLocker ? "Pristatymas į paštomatą" : "Pristatymo duomenys"}
          {/* Only show edit button for home delivery before labels are generated */}
          {/* Show edit for status 1 (awaiting) or home delivery before labels */}
          {(data.status === 1 || (!isLocker && !hasLabels)) && (
            <button className="uh-btn-icon" onClick={() => setEditContact(true)} title="Redaguoti">
              <FiEdit2 size={13} />
            </button>
          )}
          {hasLabels && (
            <span className="uh-locked-hint">
              <FiAlertCircle size={11} /> Etiketės sukurtos – redagavimas užblokuotas
            </span>
          )}
        </div>
        <div className="uh-info-rows">
          {isLocker ? (
            // Locker delivery — show locker name + address from snapshot
            <>
              <InfoRow icon={FiBox} label="Paštomatas" value={snapshotLockerName} />
              <InfoRow icon={FiMapPin} label="Adresas" value={snapshotLockerAddress} />
            </>
          ) : (
            // Home delivery — show address from snapshot
            <>
              <InfoRow
                icon={FiMapPin}
                label="Adresas"
                value={[snapshotDeliveryAddress, snapshotCity, snapshotCountry].filter(Boolean).join(", ") || "—"}
              />
              <InfoRow icon={FiPhone} label="Telefonas" value={snapshotPhone} />
            </>
          )}
        </div>
      </div>

      {/* ── Products ─────────────────────────────────────────── */}
      <div className="uh-detail-section">
        <div className="uh-detail-section-header"><FiBox size={13} /> Produktai</div>
        <div className="uh-products-list">
          {products.map(op => (
            <div key={op.id_OrdersProduct} className="uh-product-row">
              {op.product.imageUrl
                ? <img src={`${API}${op.product.imageUrl}`} alt={op.product.name} className="uh-product-img" />
                : <div className="uh-product-img uh-product-img--placeholder"><FiBox size={16} /></div>
              }
              <div className="uh-product-info">
                <div className="uh-product-name">{op.product.name}</div>
                <div className="uh-product-meta">
                  {op.quantity} {op.product.unit} × {fmtEur(op.unitPrice)}
                  {!op.product.canReturn && <span className="uh-no-return-tag">Negrąžintinas</span>}
                </div>
              </div>
              <div className="uh-product-total">{fmtEur(op.quantity * op.unitPrice)}</div>
            </div>
          ))}
        </div>
        <div className="uh-detail-totals">
          <span>Pristatymas: {fmtEur(deliveryPrice)}</span>
          <strong>Iš viso: {fmtEur(totalAmount)}</strong>
        </div>
      </div>

      {/* ── Shipment ─────────────────────────────────────────── */}
      {shipment && (
        <div className="uh-detail-section">
          <div className="uh-detail-section-header"><FiTruck size={13} /> Siunta</div>
          <div className="uh-info-rows">
            <InfoRow icon={FiCalendar} label="Išsiųsta" value={fmtDate(shipment.shippingDate)} />
            <InfoRow icon={FiCalendar} label="Numatoma" value={fmtDate(shipment.estimatedDeliveryDate)} />
            <InfoRow icon={FiTruck} label="Kurjeris" value={shipment.courierName} />
            <InfoRow icon={FiMapPin} label="Paštomatas" value={shipment.providerLockerId} />
          </div>
          {shipment.latestStatus && (
            <div className="uh-shipment-status">
              <FiClock size={12} />
              {shipment.latestStatus.typeName} · {fmtDateTime(shipment.latestStatus.date)}
            </div>
          )}
          {shipment.packages?.length > 0 && (
            <div className="uh-packages">
              {shipment.packages.map((p, i) => (
                <div key={p.id_Package} className="uh-package-chip">
                  <FiPackage size={11} />
                  <span>{p.trackingNumber || `Paketas ${i + 1}`}</span>

                </div>
              ))}
            </div>
          )}
          {shipment.packages?.[0]?.trackingNumber && (
            <a href={`/client/track/${encodeURIComponent(shipment.packages[0].trackingNumber)}`} className="uh-track-link">
              <FiExternalLink size={12} /> Sekti siuntą
            </a>
          )}
        </div>
      )}

      {/* ── Return ───────────────────────────────────────────── */}
      <div className="uh-detail-section">
        <div className="uh-detail-section-header"><FiRotateCcw size={13} /> Grąžinimas</div>
        {existingReturn ? (
          <div className="uh-return-chip">
            {(() => {
              const cfg = getReturnStatus(existingReturn.fk_ReturnStatusTypeid_ReturnStatusType);
              const Icon = cfg.icon;
              return (
                <>
                  <Icon size={12} style={{ color: cfg.color }} />
                  <span>#{existingReturn.id_Returns}</span>
                  <StatusBadge statusId={existingReturn.fk_ReturnStatusTypeid_ReturnStatusType} type="return" />
                  <span className="uh-return-chip-date">{fmtDate(existingReturn.date)}</span>
                </>
              );
            })()}
          </div>
        ) : canReturn ? (
          <button className="uh-btn uh-btn--outline uh-btn--sm" onClick={() => setShowReturn(true)}>
            <FiRotateCcw size={13} /> Pateikti grąžinimą
          </button>
        ) : (
          <p className="uh-detail-empty-text">Grąžinimas negalimas šiam užsakymui.</p>
        )}
      </div>

      {editContact && (
        <EditContactModal order={data} onClose={() => setEditContact(false)} onSaved={load} />
      )}
      {showReturn && (
        <ReturnFormModal
          order={data}
          onClose={() => setShowReturn(false)}
          onCreated={async () => { await load(); onReturnCreated?.(); }}
        />
      )}
    </div>
  );
}

// ── Order Card ────────────────────────────────────────────────────────────────
function OrderCard({ order, onReturnCreated, forceOpen }) {
  const [expanded, setExpanded] = useState(forceOpen || false);
  const [shouldRender, setShouldRender] = useState(expanded);

  useEffect(() => {
    if (expanded) {
      setShouldRender(true);
    } else {
      const t = setTimeout(() => setShouldRender(false), 350);
      return () => clearTimeout(t);
    }
  }, [expanded]);

  return (
    <div className={`uh-order-card${expanded ? " uh-order-card--open" : ""}`}>
      <button className="uh-order-card-header" onClick={() => setExpanded(e => !e)}>
        <div className="uh-order-card-left">
          <div className="uh-order-id">
            <FiShoppingBag size={13} /> Užsakymas #{order.id_Orders}
          </div>
          <div className="uh-order-meta">
            <span><FiCalendar size={11} /> {fmtDate(order.ordersDate)}</span>
            <span>{order.itemCount} prек.</span>
            <span>{fmtEur(order.totalAmount)}</span>
            {order.hasLabels && <span className="uh-label-dot"><FiFileText size={11} /> Etiketės</span>}
            {order.hasReturn && <span className="uh-return-dot"><FiRotateCcw size={11} /> Grąžinimas</span>}
          </div>
        </div>
        <div className="uh-order-card-right">
          <StatusBadge statusId={order.status} />
          {expanded ? <FiChevronDown size={15} /> : <FiChevronRight size={15} />}
        </div>
      </button>

      <div className={`uh-order-expand${expanded ? " uh-order-expand--open" : ""}`}>
        <div className="uh-order-expand-inner">
          <div className="uh-order-card-body">
            {shouldRender && (
              <OrderDetail orderId={order.id_Orders} onReturnCreated={onReturnCreated} />
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

// ── Main Page ─────────────────────────────────────────────────────────────────
export default function ClientOrdersPage({ openOrderId, defaultTab = "orders" }) {
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [activeTab, setActiveTab] = useState(defaultTab);
  const [returns, setReturns] = useState([]);
  const [loadingReturns, setLoadingReturns] = useState(false);
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const orderToOpen = searchParams.get("order");

  const loadOrders = useCallback(async () => {
    setLoading(true); setError(null);
    try { setOrders(await clientApi.getOrders()); }
    catch (e) {
      setError(e.message === "Prisijunkite iš naujo.");
    } finally { setLoading(false); }
  }, []);

  const loadReturns = useCallback(async () => {
    setLoadingReturns(true);
    try { setReturns(await clientApi.getReturns()); }
    catch { /* silent — returns tab is optional */ }
    finally { setLoadingReturns(false); }
  }, []);

  useEffect(() => { loadOrders(); }, [loadOrders]);
  useEffect(() => { if (activeTab === "returns") loadReturns(); }, [activeTab, loadReturns]);

  return (
    <div className="uh-page">
      <div className="uh-tabs">
        <button className={`uh-tab${activeTab === "orders" ? " uh-tab--active" : ""}`}
          onClick={() => setActiveTab("orders")}>
          <FiShoppingBag size={14} /> Užsakymai
          {orders.length > 0 && <span className="co-tab-count">{orders.length}</span>}
        </button>
        <button className={`uh-tab${activeTab === "returns" ? " uh-tab--active" : ""}`}
          onClick={() => setActiveTab("returns")}>
          <FiRotateCcw size={14} /> Grąžinimai
          {returns.length > 0 && <span className="uh-tab-count">{returns.length}</span>}
        </button>
      </div>

      <div className="uh-content">
        {activeTab === "orders" && (
          <>
            {loading && <div className="uh-state"><span className="uh-spinner" /><span>Kraunami užsakymai…</span></div>}
            {!loading && error && <div className="uh-state uh-state--error"><FiAlertCircle size={28} /><span>{error}</span></div>}
            {!loading && orders.length === 0 && (
              <div className="uh-state uh-state--empty"><FiShoppingBag size={36} /><span>Jūs dar neturite užsakymų.</span></div>
            )}
            {!loading && !error && orders.length > 0 && (
              <div className="uh-orders-list">
                {orders.map(o => (
                  <OrderCard
                    key={o.id_Orders}
                    order={o}
                    onReturnCreated={loadOrders}
                    forceOpen={
                      String(openOrderId) === String(o.id_Orders) ||
                      String(orderToOpen) === String(o.id_Orders)
                    }
                  />
                ))}
              </div>
            )}
          </>
        )}

        {activeTab === "returns" && (
          <>
            {loadingReturns && <div className="uh-state"><span className="uh-spinner" /><span>Kraunami grąžinimai…</span></div>}
            {!loadingReturns && returns.length === 0 && (
              <div className="uh-state uh-state--empty"><FiRotateCcw size={36} /><span>Grąžinimų nėra.</span></div>
            )}
            {!loadingReturns && returns.length > 0 && (
              <div className="uh-returns-all">
                {returns.map(r => (
                  <div key={r.id_Returns} className="uh-return-card">
                    <div className="uh-return-card-header">
                      <span className="uh-return-card-id"><FiRotateCcw size={13} /> Grąžinimas #{r.id_Returns}</span>
                      <StatusBadge statusId={r.fk_ReturnStatusTypeid_ReturnStatusType} type="return" />
                    </div>
                    <div className="uh-return-card-meta">
                      <span><FiCalendar size={11} /> {fmtDate(r.date)}</span>
                      {r.orderId && <span>Užsakymas #{r.orderId}</span>}
                      <span>{r.itemCount} prек.</span>
                      <span className="uh-return-method-tag">{r.returnMethod}</span>
                    </div>
                    {r.clientNote && <p className="uh-return-card-note">"{r.clientNote}"</p>}
                    {r.employeeNote && (
                      <div className="uh-employee-note">
                        <FiUser size={11} /> <strong>Darbuotojo komentaras:</strong> {r.employeeNote}
                      </div>
                    )}
                    {r.returnShipment?.packages?.length > 0 && (
                      <div className="uh-return-labels">
                        <span className="uh-return-labels-title"><FiFileText size={11} /> Grąžinimo etiketės:</span>
                        {r.returnShipment.packages.map((p, i) =>
                          p.labelFile ? (
                            <a key={p.id_Package} href={`${API}${p.labelFile}`}
                              target="_blank" rel="noopener noreferrer" className="uh-label-btn">
                              <FiFileText size={11} /> Paketas {i + 1}
                            </a>
                          ) : null
                        )}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
}
