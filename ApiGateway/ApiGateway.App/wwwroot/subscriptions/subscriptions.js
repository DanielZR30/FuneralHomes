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
      Ui.el("td", {}, Ui.el("span", { class: `status ${s.status === "Active" ? "Active" : "Inactive"}` }, s.status)),
      Ui.el("td", {}, s.status === "Active" ? Ui.el("button", { class: "secondary", type: "button", onclick: () => cancel(s.id) }, "Cancelar") : ""))));
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
}
