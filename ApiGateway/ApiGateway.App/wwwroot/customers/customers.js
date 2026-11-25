// Pantalla de Clientes: PATRÓN DE REFERENCIA para las demás pantallas (ver LEEME_UI).
// 1) Ui.init(...)  2) cargar datos con Api.request(...)  3) pintar con Ui.el  4) errores con Api.errorMessage
if (Ui.init("/customers/")) {
  const rows = document.getElementById("rows");
  const empty = document.getElementById("empty");
  const filter = document.getElementById("filter");
  const STATUSES = ["Active", "Suspended", "Inactive"];

  async function load() {
    const query = filter.value ? `?status=${filter.value}` : "";
    const res = await Api.request("customers", `/api/v1/customers${query}`);
    if (!res.ok) { Ui.show(Api.errorMessage(res), "err"); return; }

    rows.replaceChildren(...res.data.map((c) => Ui.el("tr", {},
      Ui.el("td", {}, c.name),
      Ui.el("td", {}, c.customerType === "Corporate" ? "Empresa" : "Persona natural"),
      Ui.el("td", {}, `${c.identificationType} ${c.identificationNumber}`),
      Ui.el("td", {}, c.email || "—"),
      Ui.el("td", {}, Ui.el("span", { class: `status ${c.status}` }, Ui.label(c.status))),
      Ui.el("td", {}, Ui.el("select", { onchange: (e) => changeStatus(c.id, e.target.value) },
        Ui.el("option", { value: "" }, "Cambiar a…"),
        STATUSES.filter((s) => s !== c.status).map((s) => Ui.el("option", { value: s }, Ui.label(s))))))));
    empty.hidden = res.data.length > 0;
  }

  async function changeStatus(id, status) {
    if (!status) return;
    Ui.show("");
    const res = await Api.request("customers", `/api/v1/customers/${id}/status`, { method: "PATCH", body: { status } });
    await load(); // recarga primero para que el mensaje de abajo no se borre
    if (res.ok) { Ui.show("Estado actualizado.", "ok"); } else { Ui.show(Api.errorMessage(res), "err"); }
  }

  document.getElementById("form").addEventListener("submit", async (e) => {
    e.preventDefault();
    Ui.show("");
    const val = (id) => document.getElementById(id).value.trim();
    const body = {
      customerType: val("customerType"),
      name: val("name"),
      identificationType: val("identificationType"),
      identificationNumber: val("identificationNumber"),
      email: val("email") || null,
      phone: val("phone") || null,
      address: val("address") || null
    };
    const res = await Api.request("customers", "/api/v1/customers", { method: "POST", body });
    if (res.ok) { e.target.reset(); await load(); Ui.show(`Cliente "${res.data.name}" creado.`, "ok"); }
    else { Ui.show(Api.errorMessage(res), "err"); }
  });

  filter.addEventListener("change", load);
  document.getElementById("reload").addEventListener("click", load);
  load();
}
