// Pantalla de Suscripciones (módulo Customers + datos de Financials para el plan).
if (Ui.init("/subscriptions/")) {
  const customerSel = document.getElementById("customer");
  const planSel = document.getElementById("plan");
  const rows = document.getElementById("rows");
  const empty = document.getElementById("empty");
  let plans = [];

  document.getElementById("startDate").value = new Date().toISOString().slice(0, 10);
  const planName = (id) => (plans.find((p) => p.id === id) || {}).name || id;

  async function loadLists() {
    const [c, p] = await Promise.all([
      Api.request("customers", "/api/v1/customers?status=Active"),
      Api.request("financials", "/api/v1/plans")]);
    if (!c.ok) { Ui.show(Api.errorMessage(c), "err"); return; }
    if (!p.ok) { Ui.show(Api.errorMessage(p), "err"); return; }
    plans = p.data;
    customerSel.replaceChildren(...c.data.map((x) => Ui.el("option", { value: x.id }, `${x.name} (${x.identificationType} ${x.identificationNumber})`)));
    planSel.replaceChildren(...plans.map((x) => Ui.el("option", { value: x.id }, x.name)));
    await loadSubscriptions();
  }

  async function loadSubscriptions() {
    if (!customerSel.value) { rows.replaceChildren(); empty.hidden = false; return; }
    const res = await Api.request("customers", `/api/v1/customers/${customerSel.value}/subscriptions`);
    if (!res.ok) { Ui.show(Api.errorMessage(res), "err"); return; }
    rows.replaceChildren(...res.data.map((s) => Ui.el("tr", {},
      Ui.el("td", {}, planName(s.externalPlanId)),
      Ui.el("td", {}, String(s.maxBeneficiaries)),
      Ui.el("td", {}, s.startDate),
      Ui.el("td", {}, s.endDate || "—"),
      Ui.el("td", {}, Ui.el("span", { class: `status ${s.status === "Active" ? "Active" : "Inactive"}` }, Ui.label(s.status))),
      Ui.el("td", {},
        Ui.el("button", { class: "secondary", type: "button", onclick: () => openBeneficiaries(s) }, "Beneficiarios"), " ",
        s.status === "Active" ? Ui.el("button", { class: "secondary", type: "button", onclick: () => cancel(s.id) }, "Cancelar") : ""))));
    empty.hidden = res.data.length > 0;
  }

  async function cancel(id) {
    Ui.show("");
    const res = await Api.request("customers", `/api/v1/subscriptions/${id}/cancel`, { method: "PATCH" });
    await loadSubscriptions();
    if (res.ok) Ui.show("Suscripción cancelada.", "ok"); else Ui.show(Api.errorMessage(res), "err");
  }

  document.getElementById("form").addEventListener("submit", async (e) => {
    e.preventDefault();
    Ui.show("");
    const body = {
      customerId: customerSel.value,
      externalPlanId: document.getElementById("fakePlan").checked ? crypto.randomUUID() : planSel.value,
      maxBeneficiaries: Number(document.getElementById("maxBeneficiaries").value),
      startDate: document.getElementById("startDate").value
    };
    const res = await Api.request("customers", "/api/v1/subscriptions", { method: "POST", body });
    await loadSubscriptions();
    if (res.ok) Ui.show("Suscripción creada: Customers validó el plan con Financials.", "ok");
    else Ui.show(Api.errorMessage(res), "err");
  });

  customerSel.addEventListener("change", loadSubscriptions);
  loadLists();

  // ---- Beneficiarios de la suscripción elegida (Customers) ----
  const benCard = document.getElementById("benCard");
  const benRows = document.getElementById("benRows");
  let currentSub = null;
  const SUBJECT = { Human: "Persona", Pet: "Mascota" };

  async function openBeneficiaries(sub) {
    currentSub = sub;
    document.getElementById("benPlan").textContent = planName(sub.externalPlanId);
    benCard.hidden = false;
    await loadBeneficiaries();
    benCard.scrollIntoView({ behavior: "smooth" });
  }

  async function loadBeneficiaries() {
    const base = `/api/v1/subscriptions/${currentSub.id}`;
    const [list, cap] = await Promise.all([
      Api.request("customers", `${base}/beneficiaries`),
      Api.request("customers", `${base}/capacity`)]);
    if (!list.ok) { Ui.show(Api.errorMessage(list), "err"); return; }
    if (cap.ok) {
      const c = cap.data;
      document.getElementById("capacity").textContent =
        `Cupo: ${c.assignedBeneficiaries} de ${c.maxBeneficiaries} usados · disponibles: ${c.availableCapacity} · ${c.canAcceptBeneficiary ? "acepta más beneficiarios" : "no acepta más beneficiarios"}`;
    }
    benRows.replaceChildren(...list.data.map((b) => Ui.el("tr", {},
      Ui.el("td", {}, b.fullName),
      Ui.el("td", {}, Ui.label(b.subjectType)),
      Ui.el("td", {}, Ui.label(b.relationshipType)),
      Ui.el("td", {}, String(b.derivedAge)),
      Ui.el("td", {}, b.identificationNumber ? `${b.identificationType} ${b.identificationNumber}` : "—"),
      Ui.el("td", {}, Ui.el("span", { class: `status ${b.status === "Active" ? "Active" : "Inactive"}` }, Ui.label(b.status))),
      Ui.el("td", {}, b.status === "Active"
        ? Ui.el("button", { class: "secondary", type: "button", onclick: () => changeBeneficiary(b.memberId, "DELETE", "Beneficiario retirado.") }, "Retirar")
        : Ui.el("button", { class: "secondary", type: "button", onclick: () => changeBeneficiary(b.memberId, "PATCH", "Beneficiario reactivado.") }, "Reactivar")))));
    document.getElementById("benEmpty").hidden = list.data.length > 0;
  }

  async function changeBeneficiary(memberId, method, okText) {
    Ui.show("");
    const suffix = method === "PATCH" ? "/reactivate" : "";
    const res = await Api.request("customers", `/api/v1/subscriptions/${currentSub.id}/beneficiaries/${memberId}${suffix}`, { method });
    await loadBeneficiaries();
    if (res.ok) Ui.show(okText, "ok"); else Ui.show(Api.errorMessage(res), "err");
  }

  // Una mascota solo puede tener parentesco "Pet"
  document.getElementById("b_subjectType").addEventListener("change", (e) => {
    if (e.target.value === "Pet") document.getElementById("b_relationshipType").value = "Pet";
  });

  document.getElementById("benForm").addEventListener("submit", async (e) => {
    e.preventDefault();
    Ui.show("");
    const v = (id) => document.getElementById(`b_${id}`).value.trim();
    const body = {
      subjectType: v("subjectType"), firstName: v("firstName"), lastName: v("lastName"), birthDate: v("birthDate"),
      beneficiaryType: v("beneficiaryType"), relationshipType: v("relationshipType"),
      identificationType: v("identificationType") || null, identificationNumber: v("identificationNumber") || null,
      email: v("email") || null, phone: v("phone") || null
    };
    const res = await Api.request("customers", `/api/v1/subscriptions/${currentSub.id}/beneficiaries`, { method: "POST", body });
    if (res.ok) { e.target.reset(); await loadBeneficiaries(); Ui.show("Beneficiario agregado.", "ok"); }
    else { Ui.show(Api.errorMessage(res), "err"); }
  });
}
