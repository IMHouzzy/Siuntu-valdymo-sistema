// pages/Shipments/ShipmentFormPage.jsx

import React, { useEffect, useState, useMemo } from "react";
import { useNavigate, useParams } from "react-router-dom";
import FormPageLayout from "../../components/FormPageLayout";
import SmartForm from "../../components/SmartForm";
import ShipmentLabels from "../../components/ShipmentLabels";
import "../../styles/ShipmentRegistration.css";
import { FiTruck, FiUser, FiPackage, FiMapPin, FiArrowLeft } from "react-icons/fi";
import { useAuth } from "../../services/AuthContext";
import { shipmentsApi, companiesApi } from "../../services/api";
import { validateShipment } from "./shipmentValidation";

export default function ShipmentFormPage() {
  const [patchValues, setPatchValues] = useState({});
  const { orderId } = useParams();
  const navigate = useNavigate();
  const { activeCompanyId } = useAuth();

  const [order, setOrder] = useState(null);
  const [couriers, setCouriers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [submitError, setSubmitError] = useState(null);

  const [selectedCourier, setSelectedCourier] = useState(null);
  const [createdShipment, setCreatedShipment] = useState(null);
  const prevCourierIdRef = React.useRef(null);

  // ── Fetch ──────────────────────────────────────────────────────────────────
  useEffect(() => {
    if (!activeCompanyId) return;
    Promise.all([
      shipmentsApi.getOrderForShipment(orderId),
      companiesApi.getCouriers(activeCompanyId),
    ])
      .then(([ord, crs]) => {
        setOrder(ord);
        setCouriers(Array.isArray(crs) ? crs : []);

        // Pre-select courier that was chosen at order creation time
        if (ord.snapshotCourierId && Array.isArray(crs)) {
          const preselected = crs.find(
            (c) => (c.id ?? c.id_Courier) === ord.snapshotCourierId
          );
          if (preselected) setSelectedCourier(preselected);
        }
      })
      .catch(e => setError(e.message))
      .finally(() => setLoading(false));
  }, [orderId, activeCompanyId]);

  // ── Derived flags ──────────────────────────────────────────────────────────
  const needsLockerPicker = !!selectedCourier?.supportsLockers;

  // ── Initial values from order snapshot ────────────────────────────────────
  const initialValues = useMemo(() => {
    if (!order) return {};

    const isLocker = order.snapshotDeliveryMethod === "LOCKER";

    return {
      courierId: order.snapshotCourierId ?? null,
      _needsLocker: false, // Will be updated via onValuesChange

      deliveryCoords: isLocker && order.snapshotLockerId
        ? {
          lockerId: order.snapshotLockerId,
          name: order.snapshotLockerName ?? "",
          id: order.snapshotLockerId,
          street: order.snapshotLockerAddress ?? "",
          city: "",
          lat: order.snapshotLat ?? 0,
          lng: order.snapshotLng ?? 0,
        }
        : null,

      street: (!isLocker ? order.snapshotDeliveryAddress : "") ?? "",
      city: (!isLocker ? order.snapshotCity : "") ?? "",
      country: (!isLocker ? order.snapshotCountry : "") ?? "",
      postalCode: "",

      shippingDate: new Date().toISOString().slice(0, 10),
      estimatedDeliveryDate: "",
      _clientName: "",
      _clientPhone: "",
      _clientEmail: "",
      _products: [],
      packages: [{ weight: "" }],
    };
  }, [order]);

  // ── onValuesChange ─────────────────────────────────────────────────────────
  const handleValuesChange = (vals) => {
    setTimeout(() => {
      const c = couriers.find((c) => (c.id ?? c.id_Courier) === vals.courierId);
      setSelectedCourier(c ?? null);

      const courierChanged = prevCourierIdRef.current !== vals.courierId;
      prevCourierIdRef.current = vals.courierId;

      const patch = {};
      
      // Set the _needsLocker flag for validation
      patch._needsLocker = !!c?.supportsLockers;
      
      if (courierChanged) patch.deliveryCoords = null;
      
      if (c?.deliveryTermDays && vals.shippingDate) {
        const d = new Date(vals.shippingDate);
        d.setDate(d.getDate() + c.deliveryTermDays);
        patch.estimatedDeliveryDate = d.toISOString().slice(0, 10);
      }
      setPatchValues(patch);
    }, 0);
  };

  // ── Fields ─────────────────────────────────────────────────────────────────
  const fields = useMemo(() => {
    if (!order) return [];
    const client = order.client ?? {};

    return [
      // 1. Courier cards
      { type: "section", title: <span><FiTruck size={18} /> Kurjeris</span> },
      {
        name: "courierId",
        label: null,
        type: "courier-cards",
        required: true,
        colSpan: 2,
        couriers,
        onSelect: (c) => { setTimeout(() => setSelectedCourier(c), 0); },
      },

      // 2. Delivery location
      {
        type: "section",
        title: <span><FiMapPin size={18} /> Pristatymo vieta</span>,
        subtitle: needsLockerPicker
          ? "Pasirinkite paštomatą arba siuntų tašką"
          : "Pristatymo adresas (paimtas iš užsakymo — redaguokite jei reikia keisti)",
      },

      // Locker picker
      ...(needsLockerPicker ? [{
        name: "deliveryCoords",
        label: null,
        type: "locker-picker",
        required: true,
        colSpan: 2,
        courierType: selectedCourier?.type,
        companyId: activeCompanyId,
      }] : []),

      // Address fields + map for home delivery / custom
      ...(!needsLockerPicker ? [
        { 
          name: "street", 
          label: "Gatvė", 
          placeholder: "pvz. Studentų g. 50", 
          colSpan: 1, 
          required: true 
        },
        {
          name: "city", 
          label: "Miestas", 
          placeholder: "pvz. Kaunas", 
          required: true,
        },
        { 
          name: "country", 
          label: "Šalis", 
          placeholder: "pvz. Lietuva", 
          required: true 
        },
        {
          name: "postalCode", 
          label: "Pašto kodas", 
          placeholder: "pvz. LT-44001", 
          required: true,
        },
        {
          name: "deliveryCoords",
          label: null,
          type: "map",
          colSpan: 2,
          getAddress: (vals) => [vals.street, vals.city, vals.country].filter(Boolean).join(", "),
        },
      ] : []),

      // Dates
      { 
        name: "shippingDate", 
        label: "Siuntimo data", 
        type: "date", 
        required: true 
      },
      { 
        name: "estimatedDeliveryDate", 
        label: "Numatoma pristatymo", 
        type: "display", 
        placeholder: "—" 
      },

      // 3. Recipient info (read-only display)
      { type: "section", title: <span><FiUser size={18} /> Gavėjas</span> },
      {
        name: "_clientName", 
        label: "Vardas pavardė", 
        type: "display", 
        colSpan: 2,
        render: () => `${client.name ?? ""} ${client.surname ?? ""}`.trim() || "—",
      },
      {
        name: "_clientPhone", 
        label: "Telefonas", 
        type: "display",
        render: () => client.phoneNumber || "—",
      },
      {
        name: "_clientEmail", 
        label: "El. paštas", 
        type: "display",
        render: () => client.email || "—",
      },

      // 4. Products + packages
      { type: "section", title: <span><FiPackage size={18} /> Prekės</span> },
      { 
        name: "_products", 
        label: null, 
        type: "product-view", 
        colSpan: 2, 
        getItems: () => order.items ?? [] 
      },
      { type: "section", title: <span><FiPackage size={18} /> Pakuotės</span> },
      { 
        name: "packages", 
        type: "packages", 
        colSpan: 2,
        required: true,
      },
      
      // Hidden field for validation
      { name: "_needsLocker", type: "display", visible: () => false },
    ];
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [order, couriers, needsLockerPicker, selectedCourier, activeCompanyId]);

  // ── States ──────────────────────────────────────────────────────────────────
  if (loading) return (
    <FormPageLayout title="Registruoti siuntą" actions={<button className="od-back-btn" onClick={() => navigate(-1)}><FiArrowLeft size={16} /> Grįžti</button>}>
      <div className="shp-state"><div className="shp-spinner" /><span>Kraunama…</span></div>
    </FormPageLayout>
  );

  if (error || !order) return (
    <FormPageLayout title="Registruoti siuntą" actions={<button className="od-back-btn" onClick={() => navigate(-1)}><FiArrowLeft size={16} /> Grįžti</button>}>
      <div className="shp-state shp-state--error">⚠️ {error ?? "Užsakymas nerastas."}</div>
    </FormPageLayout>
  );

  if (createdShipment) return (
    <FormPageLayout title={`Siuntos etiketės — #${createdShipment.trackingNumber}`}>
      <ShipmentLabels
        shipmentId={createdShipment.shipmentId}
        trackingNumber={createdShipment.trackingNumber}
        packages={createdShipment.packages}
        onClose={() => navigate("/orderlist")}
      />
    </FormPageLayout>
  );

  // ── Submit ──────────────────────────────────────────────────────────────────
  const handleSubmit = async (values) => {
    setSubmitError(null);
    const pkgs = Array.isArray(values.packages) ? values.packages : [{ weight: "" }];

    const coords = values.deliveryCoords;
    const deliveryLat = coords?.lat ?? coords?.latitude ?? coords?.position?.lat ?? null;
    const deliveryLng = coords?.lng ?? coords?.longitude ?? coords?.position?.lng ?? null;

    const body = {
      orderId: Number(orderId),
      courierId: values.courierId ?? null,
      shippingDate: values.shippingDate || null,
      estimatedDeliveryDate: values.estimatedDeliveryDate || null,
      deliveryLat,
      deliveryLng,
      packageCount: pkgs.length,
      lockerId: coords?.lockerId ?? null,
      recipientPostalCode: values.postalCode || null,
      recipientCity: values.city || null,
      recipientStreet: values.street || null,
      recipientCountry: values.country || null,
      senderStreet: order?.shippingStreet || order?.shippingAddress || null,
      senderCity: order?.shippingCity || null,
      senderPostalCode: order?.shippingPostalCode || null,
      packageWeightKg: pkgs[0]?.weight ? Number(pkgs[0].weight) : null,
      packageWeights: pkgs.map((p) => p.weight ? Number(p.weight) : null),
    };

    try {
      const res = await shipmentsApi.create(body);
      setCreatedShipment(res);
    } catch (err) {
      setSubmitError(err.message ?? "Serverio klaida. Bandykite dar kartą.");
      throw err;
    }
  };

  // ── Render ──────────────────────────────────────────────────────────────────
  return (
    <FormPageLayout
      title={`Registruoti siuntą — Užsakymas #${order.id_Orders ?? orderId}`}
      actions={
        <button className="od-back-btn" onClick={() => navigate(-1)}>
          <FiArrowLeft size={16} /> Grįžti
        </button>
      }
    >
      {submitError && (
        <div style={{
          background: "#fef2f2", color: "#b91c1c", border: "1px solid #fca5a5",
          borderRadius: 6, padding: "10px 14px", marginBottom: 16, fontSize: "0.875rem",
        }}>
          {submitError}
        </div>
      )}
      <SmartForm
        fields={fields}
        initialValues={initialValues}
        patchValues={patchValues}
        submitLabel="Registruoti siuntą"
        cancelLabel="Atšaukti"
        onCancel={() => navigate(-1)}
        onSubmit={handleSubmit}
        onValuesChange={handleValuesChange}
        validate={validateShipment}
      />
    </FormPageLayout>
  );
}