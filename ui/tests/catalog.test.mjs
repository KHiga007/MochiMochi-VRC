import { test } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs/promises";
import path from "node:path";
import { buildCatalog, matchesAvatar } from "../src/lib/catalog.js";
import { safeLibraryPath } from "../server/index.mjs";
const raw = JSON.parse(
  (
    await fs.readFile("H:/Unity Asset/99_Catalogo/catalogo.json", "utf8")
  ).replace(/^\uFEFF/, ""),
);
const items = buildCatalog(raw);
test("cada archivo se conserva exactamente una vez al agrupar", () => {
  assert.equal(
    items.reduce((n, x) => n + x.files.length, 0),
    raw.length,
  );
  assert.equal(
    new Set(items.flatMap((x) => x.files.map((f) => f.relativa))).size,
    raw.length,
  );
});
test("prendas homónimas forman una ficha con rutas distintas de variantes", () => {
  const clothes = items.filter((x) => x.category === "02_Ropa");
  assert.equal(
    new Set(
      clothes.map((x) =>
        x.name.normalize("NFKC").trim().replace(/\s+/g, " ").toLowerCase(),
      ),
    ).size,
    clothes.length,
  );
  const caged = clothes.find((x) => x.name === "Caged Sailor");
  assert(caged.variants.length >= 3);
  assert.equal(
    new Set(caged.variants.map((x) => x.id)).size,
    caged.variants.length,
  );
});
test("Chocolat normal y chibi permanecen separados", () => {
  const matches = items.filter(
    (x) => x.category === "01_Avatares" && /Chocolat/.test(x.name),
  );
  assert(matches.some((x) => x.variant === "normal"));
  assert(matches.some((x) => x.variant === "chibi"));
  assert(!matches.find((x) => x.variant === "normal").name.includes("Normal"));
});
test("Mamefriends respeta compatibilidad del grupo sin propagar un exclusivo a otro", () => {
  const families = { Mamefriends: ["Kipfel", "Mamehinata"] };
  assert(matchesAvatar({ avatars: ["Mamefriends"] }, "Kipfel", families));
  assert(matchesAvatar({ avatars: ["Kipfel"] }, "Mamefriends", families));
  assert(!matchesAvatar({ avatars: ["Kipfel"] }, "Mamehinata", families));
});
test("conserva compatibilidad manual de Riru de Wako", () => {
  const x = items.find((x) => x.name.includes("アイドルType Etoile"));
  assert(x.avatars.includes("Riru (Wako's Atelier)"));
});
test("rutas quedan dentro de biblioteca", async () => {
  assert(
    (await safeLibraryPath("99_Catalogo/catalogo.json")).endsWith(
      "catalogo.json",
    ),
  );
  await assert.rejects(safeLibraryPath("../outside"));
  await assert.rejects(safeLibraryPath("C:/Windows/win.ini"));
});
