// Pantalla de Planes (módulo Financials). Mismo patrón que Clientes.
if (Ui.init("/plans/")) {
  const rows = document.getElementById("rows");
  const empty = document.getElementById("empty");
  const quoteCard = document.getElementById("quoteCard");
  const benBox = document.getElementById("beneficiaries");
  const money = (n) => Number(n).toLocaleString("es-CO", { style: "currency", currency: "COP", maximumFractionDigits: 0 });
  let quotePlanId = null;

  async function load() {
    const res = await Api.request("financials", "/api/v1/plans");
    if (!res.ok) { Ui.show(Api.errorMessage(res), "err"); return; }
    rows.replaceChildren(...res.data.map((p) => Ui.el("tr", {},
      Ui.el("td", {}, p.name),
      Ui.el("td", {}, p.description || "—"),
      Ui.el("td", {}, money(p.baseMonthlyFee)),
      Ui.el("td", {}, String(p.maxBeneficiaries)),
      Ui.el("td", {}, Ui.el("span", { class: `status ${p.status === "Active" ? "Active" : "Inactive"}` }, p.status)),
      Ui.el("td", {}, Ui.el("button", { class: "secondary", type: "button", onclick: () => openQuote(p) }, "Cotizar")))));
    empty.hidden = res.data.length > 0;
  }

  function benRow() {
    const row = Ui.el("div", { class: "row", style: "margin-top:6px" },
      Ui.el("select", { "data-f": "subjectType" }, Ui.el("option", { value: "Human" }, "Persona"), Ui.el("option", { value: "Pet" }, "Mascota")),
      Ui.el("input", { "data-f": "derivedAge", type: "number", min: "0", max: "120", value: "30", style: "width:80px", title: "Edad" }),
      Ui.el("select", { "data-f": "relationshipType" },
        ["Principal", "Spouse", "Child", "Parent", "Employee", "Pet", "Other"].map((r) => Ui.el("option", { value: r }, r))),
      Ui.el("button", { class: "secondary", type: "button", onclick: () => row.remove() }, "Quitar"));
    return row;
  }

  function openQuote(plan) {
    quotePlanId = plan.id;
    document.getElementById("quotePlan").textContent = plan.name;
    benBox.replaceChildren(benRow());
    document.getElementById("quoteResult").replaceChildren();
    quoteCard.hidden = false;
    quoteCard.scrollIntoView({ behavior: "smooth" });
  }

  document.getElementById("addBen").addEventListener("click", () => benBox.append(benRow()));

  document.getElementById("doQuote").addEventListener("click", async () => {
    Ui.show("");
    const beneficiaries = [...benBox.children].map((r) => {
      const v = (f) => r.querySelector(`[data-f=${f}]`).value;
      return { subjectType: v("subjectType"), derivedAge: Number(v("derivedAge")), relationshipType: v("relationshipType") };
    });
    const res = await Api.request("financials", `/api/v1/plans/${quotePlanId}/quote`, { method: "POST", body: { beneficiaries } });
    const out = document.getElementById("quoteResult");
    if (!res.ok) { out.replaceChildren(); Ui.show(Api.errorMessage(res), "err"); return; }
    const q = res.data;
    out.replaceChildren(
      Ui.el("p", {}, `Cuota base: ${money(q.baseMonthlyFee)}`),
      ...q.surcharges.map((s) => Ui.el("p", { class: "hint" }, `${s.subjectType} · ${s.derivedAge} años · ${s.relationshipType}: +${money(s.surcharge)}`)),
      Ui.el("p", {}, Ui.el("strong", {}, `Total mensual: ${money(q.totalMonthlyFee)}`)));
  });

  document.getElementById("form").addEventListener("submit", async (e) => {
    e.preventDefault();
    Ui.show("");
    const val = (id) => document.getElementById(id).value.trim();
    const body = { name: val("name"), description: val("description") || null,
      baseMonthlyFee: Number(val("baseMonthlyFee")), maxBeneficiaries: Number(val("maxBeneficiaries")) };
    const res = await Api.request("financials", "/api/v1/plans", { method: "POST", body });
    if (res.ok) { e.target.reset(); await load(); Ui.show(`Plan "${res.data.name}" creado.`, "ok"); }
    else { Ui.show(Api.errorMessage(res), "err"); }
  });

  load();
}
