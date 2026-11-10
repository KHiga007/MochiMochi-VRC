import { test } from "node:test";
import assert from "node:assert/strict";
import http from "node:http";
import fs from "node:fs/promises";
import os from "node:os";
import path from "node:path";
test("servidor sirve aplicación, catálogo e imagen y rechaza acceso fuera de biblioteca", async () => {
  const root=await fs.mkdtemp(path.join(os.tmpdir(),'mochi-server-fixture-'));
  process.env.VRC_LIBRARY_ROOT=root;
  await fs.mkdir(path.join(root,'99_Catalogo'));
  await fs.mkdir(path.join(root,'03_Accesorios','Mix','Producto'),{recursive:true});
  await fs.writeFile(path.join(root,'03_Accesorios','Mix','Producto','preview.jpg'),'fixture-image');
  await fs.writeFile(path.join(root,'03_Accesorios','Mix','Producto','asset.zip'),'fixture-package');
  const {createServer}=await import('../server/index.mjs');
  const server = createServer();
  await new Promise((r) => server.listen(0, "127.0.0.1", r));
  const port = server.address().port;
  const get = (pathname) =>
    new Promise((resolve, reject) => {
      http
        .get(
          {
            host: "127.0.0.1",
            port,
            path: pathname,
            headers: { host: "127.0.0.1:4173" },
          },
          (res) => {
            const chunks = [];
            res.on("data", (x) => chunks.push(x));
            res.on("end", () =>
              resolve({ status: res.statusCode, body: Buffer.concat(chunks) }),
            );
          },
        )
        .on("error", reject);
    });
  try {
    assert.equal((await get("/")).status, 200);
    const catalog = await get("/api/catalog");
    assert.equal(catalog.status, 200);
    const data = JSON.parse(catalog.body);
    assert.equal(data.raw.length, 2);
    const image = data.raw.find((f) => /\.jpg$/i.test(f.nombre));
    assert.equal(
      (await get("/api/file?path=" + encodeURIComponent(image.relativa)))
        .status,
      200,
    );
    assert.equal(
      (await get("/api/file?path=" + encodeURIComponent("../outside"))).status,
      400,
    );
  } finally {
    await new Promise((r) => server.close(r));
    if(path.dirname(root)===os.tmpdir()&&path.basename(root).startsWith('mochi-server-fixture-'))await fs.rm(root,{recursive:true,force:true});
  }
});
