// Piezas visuales compartidas: barra de navegación, panel de peticiones y utilidades DOM.
const Ui = (() => {
  // Crea elementos sin innerHTML (evita inyectar HTML desde datos del servidor)
  function el(tag, attrs = {}, ...children) {
    const node = document.createElement(tag);
    for (const [k, v] of Object.entries(attrs)) {
      if (k === "class") node.className = v;
      else if (k.startsWith("on")) node.addEventListener(k.slice(2), v);
      else if (v !== false && v != null) node.setAttribute(k, v === true ? "" : v);
    }
    for (const c of children.flat()) {
      if (c == null || c === false) continue;
      node.append(c.nodeType ? c : document.createTextNode(String(c)));
    }
    return node;
  }

  // Páginas del sistema. Cada compañero entrega la carpeta de SU módulo; los enlaces ya están aquí.
  const PAGES = [
    { href: "/customers/", label: "Clientes" },
    { href: "/subscriptions/", label: "Suscripciones" },
    { href: "/plans/", label: "Planes" }
  ];

  // Textos en español para valores que el API devuelve en inglés (el valor enviado al API no cambia)
  const LABELS = {
    Active: "Activo", Inactive: "Inactivo", Suspended: "Suspendido", Cancelled: "Cancelada", Expired: "Vencida", Removed: "Retirado",
    Individual: "Persona natural", Corporate: "Empresa", Human: "Persona", Pet: "Mascota",
    Principal: "Principal", Associated: "Asociado",
    Titular: "Titular", Spouse: "Cónyuge", Child: "Hijo(a)", Parent: "Padre/madre", Employee: "Empleado", Other: "Otro"
  };
  const label = (v) => LABELS[v] || v;

  function renderNav(activeHref) {
    const u = Api.user();
    const nav = el("header", { class: "topbar" },
      el("strong", { class: "brand" }, "Funeral Homes"),
      el("nav", {}, PAGES.map((p) => el("a", { href: p.href, class: p.href === activeHref ? "active" : "" }, p.label))),
      el("div", { class: "who" },
        u ? el("span", {}, `${u.fullName} · `, el("span", { class: "badge" }, u.roles.join(", "))) : "",
        el("button", { class: "link", onclick: () => { Api.logout(); window.location.href = "/index.html"; } }, "Salir")));
    document.body.prepend(nav);
  }

  function renderLogPanel() {
    const rows = el("tbody");
    const noToken = el("input", { type: "checkbox", onchange: (e) => { Api.debug.noToken = e.target.checked; } });
    const tamper = el("input", { type: "checkbox", onchange: (e) => { Api.debug.tamperToken = e.target.checked; } });

    const panel = el("details", { class: "logpanel", open: true },
      el("summary", {}, "Registro de peticiones (gateway)"),
      el("div", { class: "tests" },
        el("label", {}, noToken, " Enviar sin token (prueba 401)"),
        el("label", {}, tamper, " Alterar el token (prueba 401)")),
      el("table", {},
        el("thead", {}, el("tr", {}, ["Hora", "Método", "URL", "Código", "ms"].map((h) => el("th", {}, h)))),
        rows));
    document.body.append(panel);

    Api.onLog((e) => {
      const cls = e.status >= 200 && e.status < 300 ? "ok" : e.status === 0 || e.status >= 500 ? "err" : "warn";
      rows.prepend(el("tr", {},
        el("td", {}, e.at.toLocaleTimeString()),
        el("td", {}, e.method),
        el("td", { class: "mono" }, e.url),
        el("td", {}, el("span", { class: `code ${cls}` }, String(e.status))),
        el("td", {}, String(e.ms))));
      while (rows.children.length > 12) rows.lastChild.remove();
    });
  }

  // Mensaje visible (éxito o error) en un contenedor con id="message"
  function show(message, kind = "info") {
    const box = document.getElementById("message");
    if (!box) return;
    box.className = `message ${kind}`;
    box.textContent = message;
    box.hidden = !message;
  }

  // Cada página protegida llama a Ui.init("/ruta/")
  function init(activeHref) {
    if (!Api.requireLogin()) return false;
    renderNav(activeHref);
    renderLogPanel();
    return true;
  }

  return { el, show, init, label };
})();
