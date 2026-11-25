// Cliente HTTP compartido de la interfaz. TODAS las páginas lo usan.
// Habla solo con el gateway (mismo origen): /identity/**, /customers/**, /financials/**.
const Api = (() => {
  const TOKEN = "fh_token";
  const USER = "fh_user";
  const EXPIRES = "fh_expires";

  const listeners = [];
  // Opciones de prueba para la demostración de seguridad (se activan desde el panel de peticiones)
  const debug = { noToken: false, tamperToken: false };

  const store = (k, v) => { try { sessionStorage.setItem(k, v); } catch { /* sin storage */ } };
  const read = (k) => { try { return sessionStorage.getItem(k); } catch { return null; } };
  const drop = (k) => { try { sessionStorage.removeItem(k); } catch { /* sin storage */ } };

  const token = () => read(TOKEN);
  const user = () => { try { return JSON.parse(read(USER) || "null"); } catch { return null; } };
  const isLoggedIn = () => !!token();
  const hasRole = (...roles) => { const u = user(); return !!u && roles.some((r) => u.roles.includes(r)); };

  function saveSession(login) {
    store(TOKEN, login.accessToken);
    store(USER, JSON.stringify(login.user));
    store(EXPIRES, login.expiresAtUtc);
  }

  function logout() { [TOKEN, USER, EXPIRES].forEach(drop); }
  function expiresAt() { return read(EXPIRES); }

  // Redirige al login si no hay sesión (no valida el token: eso lo hace cada microservicio)
  function requireLogin() {
    if (!isLoggedIn()) { window.location.href = "/index.html"; return false; }
    return true;
  }

  function onLog(fn) { listeners.push(fn); }
  function emit(entry) { listeners.forEach((fn) => fn(entry)); }

  // service: "identity" | "customers" | "financials"; path: "/api/v1/..."
  async function request(service, path, { method = "GET", body, auth = true } = {}) {
    const url = `/api/${service}${path}`;
    const headers = {};
    if (body !== undefined) headers["Content-Type"] = "application/json";

    let jwt = token();
    if (auth && jwt && !debug.noToken) {
      if (debug.tamperToken) jwt = jwt.slice(0, -4) + "AAAA"; // firma alterada -> debe dar 401
      headers["Authorization"] = `Bearer ${jwt}`;
    }

    const started = performance.now();
    let status = 0;
    let data = null;
    try {
      const res = await fetch(url, { method, headers, body: body !== undefined ? JSON.stringify(body) : undefined });
      status = res.status;
      const text = await res.text();
      if (text) { try { data = JSON.parse(text); } catch { data = text; } }
    } catch (err) {
      data = { message: "No se pudo conectar con el gateway o el microservicio." };
    }
    const ms = Math.round(performance.now() - started);
    emit({ method, url, status, ms, at: new Date() });
    return { ok: status >= 200 && status < 300, status, data };
  }

  async function login(email, password) {
    const res = await request("identity", "/api/v1/auth/login", { method: "POST", body: { email, password }, auth: false });
    if (res.ok) saveSession(res.data);
    return res;
  }

  // Texto claro para mostrar al usuario según el código HTTP
  function errorMessage(res) {
    const d = res.data;
    const detail = (d && (d.message || d.detail || d.title)) || (typeof d === "string" ? d : "");
    if (res.status === 0) return "Sin conexión: ¿están corriendo el gateway y los microservicios?";
    if (res.status === 401) return `401 No autenticado: falta el token, es inválido o expiró. ${detail}`.trim();
    if (res.status === 403) {
      const u = user();
      return `403 Sin permiso: tu rol (${u ? u.roles.join(", ") : "ninguno"}) no puede hacer esta operación. ${detail}`.trim();
    }
    if (res.status === 400 && d && d.errors) {
      return `400 Datos inválidos: ${Object.values(d.errors).flat().join(" ")}`;
    }
    return `${res.status} ${detail}`.trim();
  }

  return { debug, token, user, isLoggedIn, hasRole, expiresAt, logout, requireLogin, login, request, errorMessage, onLog };
})();
