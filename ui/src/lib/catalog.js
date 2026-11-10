export const categories = {
  "01_Avatares": "Avatares",
  "02_Ropa": "Ropa",
  "03_Accesorios": "Accesorios",
  "04_Cabello": "Cabello",
  "05_Texturas": "Texturas",
  "06_Animaciones_y_Presets": "Animaciones y presets",
  "07_Herramientas_y_Sistemas": "Herramientas y sistemas",
  "08_Mundos": "Mundos",
  "09_R18": "R18",
  "10_Face_Tracking": "Face Tracking",
  "11_VRM_y_Shaders": "VRM y Shaders",
  "12_Mochi_Fitter": "Mochi Fitter",
};
export const compare = (a, b) =>
  a.localeCompare(b, "es", { numeric: true, sensitivity: "base" });
export const size = (n) =>
  n >= 1073741824
    ? (n / 1073741824).toFixed(1) + " GB"
    : (n / 1048576).toFixed(1) + " MB";
export const fileUrl = (f) =>
  globalThis.window?.__VRC_BOOTSTRAP__
    ? "file:///H:/Unity%20Asset/" +
      (typeof f === "string" ? f : f.relativa)
        .replace(/\\/g, "/")
        .split("/")
        .map(encodeURIComponent)
        .join("/")
    : "/api/file?path=" +
      encodeURIComponent(typeof f === "string" ? f : f.relativa) +
      (typeof f === "string" ? "" : "&v=" + encodeURIComponent(f.modificadoUtc || f.bytes));
export const folderOf = (x) => {
  if (x.id && x.files?.some(f => f.relativa.startsWith(x.id + "\\"))) return x.id;
  const relative = x.files?.[0]?.relativa;
  return relative ? relative.slice(0, relative.lastIndexOf("\\")) : x.id;
};
export function matchesAvatar(x, a, families = {}) {
  if (x.generic) return true;
  return (
    x.avatars.includes(a) ||
    Object.entries(families).some(
      ([family, members]) =>
        (a === family && x.avatars.some((v) => members.includes(v))) ||
        (members.includes(a) && x.avatars.includes(family)),
    )
  );
}
export function familyTerms(x, families) {
  return Object.entries(families)
    .flatMap(([f, m]) =>
      x.avatars.includes(f)
        ? [f, ...m]
        : x.avatars.some((a) => m.includes(a))
          ? [f]
          : [],
    )
    .join(" ");
}
export function buildCatalog(raw) {
  const grouped = new Map();
  for (const f of raw) {
    const p = f.relativa.split("\\");
    let id = p.slice(0, -1).join("\\");
    if (p.length === 2) id += "\\" + f.nombre.replace(/\.[^.]+$/, "");
    if (p[0] === "01_Avatares")
      id = p.slice(0, p[1].startsWith("!_") ? 3 : 2).join("\\");
    if (p[0] === "03_Accesorios") {
      if (p[1] === "Por_Avatar" && p.length >= 5)
        id = p.slice(0, 4).join("\\");
      else if (p[1] === "Por_Avatar" && p.length === 4)
        id = p.slice(0, -1).concat(f.nombre.replace(/\.[^.]+$/, "")).join("\\");
      else if (!['Por_Avatar', 'Orejas_y_Colas'].includes(p[1]) && p.length >= 4)
        id = p.slice(0, 3).join("\\");
      else if (!['Por_Avatar', 'Orejas_y_Colas'].includes(p[1]) && p.length === 3)
        id = p.slice(0, -1).concat(f.nombre.replace(/\.[^.]+$/, "")).join("\\");
      const product = "Succubus_Devil_Accessory_FullSet";
      const index = p.indexOf(product);
      if (index >= 0) id = p.slice(0, index + 1).join("\\");
      else if (f.nombre.replace(/\.[^.]+$/, "") === product)
        id = p.slice(0, -1).concat(product).join("\\");
    }
    if (!grouped.has(id)) grouped.set(id, []);
    grouped.get(id).push(f);
  }
  let items = [...grouped].map(([id, files]) => {
    const tags = [
      ...new Set(
        files.flatMap((f) =>
          f.etiquetas.flatMap((t) => t.split(/\s+(?=formato:)/)),
        ),
      ),
    ];
    const category = id.split("\\")[0];
    const variant = id.includes("!_Chibi") ? "chibi" : "normal";
    const name =
      id.split("\\").at(-1) +
      (category === "01_Avatares" && variant === "chibi" ? " · Chibi" : "");
    let images = files.filter((f) => /\.(png|jpe?g|webp)$/i.test(f.nombre));
    if (category === "01_Avatares") {
      const score = (f) => {
        if (
          /(?:^|\\)(?:textures?|tex|png|psd|uv[^\\]*|mask[^\\]*|normal[^\\]*|icon|emoji|particle)(?:\\|$)/i.test(
            f.relativa,
          ) ||
          /body|face|hair|mask|normal|uvmap|eyebrow|matcap/i.test(f.nombre)
        )
          return -1;
        return (
          (f.relativa.slice(0, f.relativa.lastIndexOf("\\")) === id ? 100 : 0) +
          (/booth/i.test(f.relativa) ? 80 : 0) +
          (/preview|thumbnail/i.test(f.nombre) ? 20 : 0)
        );
      };
      images = images
        .filter((f) => score(f) >= 0)
        .sort((a, b) => score(b) - score(a));
    }
    images.sort((a,b) => Number(/^__preview\./i.test(b.nombre)) - Number(/^__preview\./i.test(a.nombre)));
    return {
      id,
      name,
      category,
      variant,
      files,
      images,
      tags,
      generic: tags.includes("compatibilidad:generico") || tags.includes("clasificacion:generico"),
      avatars: tags
        .filter((t) => t.startsWith("avatar:"))
        .map((t) => t.slice(7))
        .sort(compare),
      size: files.reduce((n, f) => n + f.bytes, 0),
      adult: tags.includes("contenido:r18") || id.includes("!_R18"),
      dup: tags.includes("estado:duplicado_exacto"),
    };
  });
  const groups = new Map();
  for (const x of items.filter((x) => x.category === "02_Ropa")) {
    const key = x.name
      .normalize("NFKC")
      .trim()
      .replace(/\s+/g, " ")
      .toLowerCase();
    if (!groups.has(key)) groups.set(key, []);
    groups.get(key).push(x);
  }
  for (const variants of groups.values()) {
    if (variants.length < 2) continue;
    const first = variants[0],
      snapshots = variants.map((v) => ({ ...v }));
    Object.assign(first, {
      variants: snapshots,
      files: snapshots.flatMap((v) => v.files),
      images: snapshots.flatMap((v) => v.images),
      avatars: [...new Set(snapshots.flatMap((v) => v.avatars))].sort(compare),
      tags: [...new Set(snapshots.flatMap((v) => v.tags))],
      size: snapshots.reduce((n, v) => n + v.size, 0),
      adult: snapshots.some((v) => v.adult),
      dup: snapshots.some((v) => v.dup),
    });
    items = items.filter((x) => x === first || !variants.includes(x));
  }
  return items.sort((a, b) => compare(a.name, b.name));
}





