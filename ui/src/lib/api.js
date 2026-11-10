export const offline = Boolean(window.__VRC_BOOTSTRAP__);
export async function apiFetch(url, options = {}) {
  if (!offline) return fetch(url, options);
  const boot = window.__VRC_BOOTSTRAP__;
  if (url === "/api/catalog") return new Response(JSON.stringify(boot));
  if (url === "/api/personal") {
    if (options.method === "POST") {
      localStorage.setItem("vrc-shelf-personal", options.body);
      return new Response('{"ok":true}');
    }
    return new Response(localStorage.getItem("vrc-shelf-personal") || "{}");
  }
  throw Error("Esta función requiere iniciar el servidor local");
}
