import http from "node:http";
import fs from "node:fs/promises";
import { createReadStream } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { spawn } from "node:child_process";
export const root = path.resolve(
  process.env.VRC_LIBRARY_ROOT || "H:/Unity Asset",
);
const project = path.resolve(
    path.dirname(fileURLToPath(import.meta.url)),
    "..",
  ),
  dist = path.join(project, "dist"),
  stateFile = path.join(project, "personal-data.json");
const port = Number(process.env.PORT || 4173);
const mime = {
  ".html": "text/html; charset=utf-8",
  ".js": "application/javascript",
  ".css": "text/css",
  ".json": "application/json",
  ".png": "image/png",
  ".jpg": "image/jpeg",
  ".jpeg": "image/jpeg",
  ".webp": "image/webp",
  ".svg": "image/svg+xml",
  ".pdf": "application/pdf",
};
export async function safeLibraryPath(relative) {
  const resolved = path.resolve(root, relative);
  if (
    resolved !== root &&
    !resolved.toLowerCase().startsWith(root.toLowerCase() + path.sep)
  )
    throw Error("Ruta fuera de biblioteca");
  let current = root;
  for (const segment of path
    .relative(root, resolved)
    .split(path.sep)
    .filter(Boolean)) {
    current = path.join(current, segment);
    const stat = await fs.lstat(current);
    if (stat.isSymbolicLink())
      throw Error("No se permiten enlaces de archivos");
  }
  return resolved;
}
async function readJson(p, fallback) {
  try {
    return JSON.parse((await fs.readFile(p, "utf8")).replace(/^\uFEFF/, ""));
  } catch (e) {
    if (e.code === "ENOENT") return fallback;
    throw e;
  }
}
async function catalog() {
  const base = path.join(root, "99_Catalogo"),
    raw = await readJson(path.join(base, "catalogo.json"), []),
    families = await readJson(path.join(base, "familias-avatares.json"), {}),
    overrides = await readJson(
      path.join(base, "compatibilidades-confirmadas.json"),
      [],
    ),
    old = new Map(raw.map((f) => [f.relativa.toLowerCase(), f])),
    files = [];
  async function walk(dir) {
    for (const entry of await fs.readdir(dir, { withFileTypes: true })) {
      if (entry.isSymbolicLink()) continue;
      const full = path.join(dir, entry.name);
      if (entry.isDirectory()) {
        if (full === base) continue;
        await walk(full);
      } else if (entry.isFile()) {
        const st = await fs.stat(full),
          rel = path.relative(root, full),
          prev = old.get(rel.toLowerCase());
        let tags = prev?.etiquetas?.filter(
          (t) => !t.startsWith("estado:duplicado"),
        ) || [
          "tipo:" + rel.split(path.sep)[0],
          "formato:" + path.extname(full).slice(1),
        ];
        if (
          prev?.bytes === st.size &&
          prev?.etiquetas?.includes("estado:duplicado_exacto")
        )
          tags.push("estado:duplicado_exacto");
        const match = overrides.find((o) =>
          rel.toLowerCase().includes(o.producto.toLowerCase()),
        );
        if (match) {
          tags = tags.filter(
            (t) => !t.startsWith("avatar:") && !t.startsWith("compatibilidad:"),
          );
          tags.push(
            ...match.avatares.map((a) => "avatar:" + a),
            "compatibilidad:confirmada_por_usuario",
          );
          if (match.etiquetas) tags.push(...match.etiquetas);
          if (match.generico) tags.push("compatibilidad:generico", "clasificacion:generico");
        } else if (
          rel.startsWith("02_Ropa" + path.sep) &&
          !rel.includes("Compatibilidad_Pendiente")
        ) {
          const a = rel.split(path.sep)[1];
          if (!tags.includes("avatar:" + a)) tags.push("avatar:" + a);
        }
        if ((/09_R18|!_R18/.test(rel) || rel.includes("【まよ Mayo】《R-18》ボディテクスチャ BodyTexture") || rel.includes("【りる RIRU用】(R18) Nipple＆Body texture")) && !tags.includes("contenido:r18"))
          tags.push("contenido:r18");
        files.push({
          ...prev,
          nombre: entry.name,
          ruta: full,
          relativa: rel,
          bytes: st.size,
          modificadoUtc: st.mtime.toISOString(),
          etiquetas: tags,
          version:
            prev?.version || entry.name.match(/\d+\.\d+(?:\.\d+)*/)?.[0] || "",
        });
      }
    }
  }
  await walk(root);
  return { raw: files, families, updated: new Date().toISOString() };
}
async function body(req) {
  let text = "";
  for await (const chunk of req) {
    text += chunk;
    if (text.length > 2e6) throw Error("Demasiados datos");
  }
  return JSON.parse(text || "{}");
}
function send(res, status, value) {
  res.writeHead(status, {
    "Content-Type": "application/json; charset=utf-8",
    "Cache-Control": "no-store",
  });
  res.end(JSON.stringify(value));
}
async function stream(res, file) {
  const st = await fs.stat(file);
  if (!st.isFile()) throw Error("No es archivo");
  res.writeHead(200, {
    "Content-Type":
      mime[path.extname(file).toLowerCase()] || "application/octet-stream",
    "Content-Length": st.size,
    "Cache-Control": "no-cache",
    "X-Content-Type-Options": "nosniff",
  });
  createReadStream(file).pipe(res);
}
const uploadSessions = new Map();
const uploadCategories = new Set(['01_Avatares','02_Ropa','03_Accesorios','04_Cabello','05_Texturas','06_Animaciones_y_Presets','07_Herramientas_y_Sistemas','08_Mundos','09_R18','10_Face_Tracking','11_VRM_y_Shaders','12_Mochi_Fitter']);
function validName(value) { return typeof value === 'string' && value.trim() === value && value.length > 0 && value.length <= 180 && !/[<>:"/\\|?*\x00-\x1f]/.test(value) && !/[. ]$/.test(value) && !/^(con|prn|aux|nul|com[1-9]|lpt[1-9])(?:\.|$)/i.test(value) && value!=='.' && value!=='..'; }
export function createServer() {
  return http.createServer(async (req, res) => {
    try {
      const host = req.headers.host;
      if (!["127.0.0.1:" + port, "localhost:" + port].includes(host))
        return send(res, 403, { error: "Host no permitido" });
      const url = new URL(req.url, "http://" + host);
      if (
        req.method === "POST" &&
        ![
          "http://127.0.0.1:" + port,
          "http://localhost:" + port,
          "http://127.0.0.1:5173",
          "http://localhost:5173",
        ].includes(req.headers.origin)
      )
        return send(res, 403, { error: "Origen no permitido" });
      if (url.pathname === '/api/upload/start' && req.method === 'POST') {
        const data=await body(req);
        if(!uploadCategories.has(data.category)||!validName(data.name)||!Array.isArray(data.files)||!data.files.length||data.files.length>100) throw Error('Nombre, categoría o archivos inválidos');
        const avatars=data.generic?[]:Array.isArray(data.avatars)?[...new Set(data.avatars)]:[];
        if(!avatars.every(validName)) throw Error('Avatar inválido');
        if(data.files.some(f=>!validName(f.name)||!Number.isSafeInteger(f.size)||f.size<0)||new Set(data.files.map(f=>f.name.toLowerCase())).size!==data.files.length) throw Error('Nombres de archivo repetidos o inválidos');
        const parts=[data.category];
        if(['02_Ropa','03_Accesorios','05_Texturas','06_Animaciones_y_Presets','09_R18'].includes(data.category))parts.push(avatars.length===1?avatars[0]:'Mix');
        parts.push(data.name);
        const relative=parts.join(path.sep), parent=path.join(root,...parts.slice(0,-1));
        await safeLibraryPath('');
        let cursor=root;for(const segment of parts.slice(0,-1)){cursor=path.join(cursor,segment);try{await fs.mkdir(cursor);}catch(e){if(e.code!=='EEXIST')throw e;}await safeLibraryPath(path.relative(root,cursor));}
        const target=path.join(root,relative);try{await fs.lstat(target);throw Error('Ya existe un producto con ese nombre');}catch(e){if(e.code!=='ENOENT')throw e;}
        const token=globalThis.crypto.randomUUID(),workRelative=path.join('99_Catalogo','.mochi-uploads',token),folder=path.join(root,workRelative);await fs.mkdir(folder,{recursive:true});await safeLibraryPath(workRelative);
        uploadSessions.set(token,{folder,relative,workRelative,data:{...data,avatars},done:new Set(),busy:false});
        return send(res,200,{token});
      }
      if(url.pathname==='/api/upload/cancel' && req.method==='POST'){
        const {token}=await body(req),session=uploadSessions.get(token);if(!session)return send(res,200,{ok:true});if(session.busy)throw Error('La carga está ocupada');
        if(session.workRelative!==path.join('99_Catalogo','.mochi-uploads',token))throw Error('Carga no válida');
        await safeLibraryPath(session.workRelative);for(const entry of await fs.readdir(session.folder)){const file=await safeLibraryPath(path.join(session.workRelative,entry));await fs.unlink(file);}await fs.rmdir(session.folder);uploadSessions.delete(token);return send(res,200,{ok:true});
      }
      if(url.pathname==='/api/upload/file' && req.method==='POST'){
        const session=uploadSessions.get(url.searchParams.get('token')),name=url.searchParams.get('name');
        if(!session||session.busy||session.done.has(name))throw Error('Carga inválida');
        const expected=session.data.files.find(f=>f.name===name);if(!expected)throw Error('Archivo no autorizado');
        await safeLibraryPath(session.workRelative);const target=path.join(session.folder,name);session.busy=true;let handle;
        try{handle=await fs.open(target,'wx');let count=0;for await(const chunk of req){count+=chunk.length;if(count>expected.size)throw Error('Tamaño incorrecto');await handle.writeFile(chunk);}if(count!==expected.size)throw Error('Carga incompleta');await handle.close();handle=null;session.done.add(name);}
        catch(e){if(handle){await handle.close();await fs.unlink(target).catch(()=>{});}throw e;}finally{session.busy=false;}
        return send(res,200,{ok:true});
      }
      if(url.pathname==='/api/upload/finish' && req.method==='POST'){
        const {token}=await body(req),session=uploadSessions.get(token);if(!session||session.done.size!==session.data.files.length)throw Error('Faltan archivos por subir');
        const location=path.join(root,'99_Catalogo','compatibilidades-confirmadas.json'),overrides=await readJson(location,[]);
        const tags=(session.data.tags||'').split(',').map(t=>t.trim()).filter(Boolean).map(t=>'tag:'+t);
        if(session.data.adult||session.data.category==='09_R18')tags.push('contenido:r18');
        overrides.unshift({producto:session.relative+path.sep,avatares:session.data.avatars,generico:!!session.data.generic,pendientes:0,etiquetas:tags});
        await fs.writeFile(location,JSON.stringify(overrides,null,2));const destination=path.join(root,session.relative);try{await fs.lstat(destination);throw Error('Ya existe un producto con ese nombre');}catch(e){if(e.code!=='ENOENT')throw e;}await fs.rename(session.folder,destination);uploadSessions.delete(token);return send(res,200,{ok:true,path:session.relative});
      }
      if (url.pathname === "/api/catalog" && req.method === "GET")
        return send(res, 200, await catalog());
      if (url.pathname === "/api/personal" && req.method === "GET")
        return send(res, 200, await readJson(stateFile, {}));
      if (url.pathname === "/api/personal" && req.method === "POST") {
        const data = await body(req);
        if (!data || Array.isArray(data) || typeof data !== "object")
          return send(res, 400, { error: "Datos inválidos" });
        for (const v of Object.values(data)) {
          if (
            !v ||
            typeof v !== "object" ||
            (v.tags && !Array.isArray(v.tags)) ||
            (v.notes && typeof v.notes !== "string")
          )
            return send(res, 400, { error: "Respaldo inválido" });
        }
        await fs.writeFile(stateFile + ".tmp", JSON.stringify(data, null, 2));
        await fs.rename(stateFile + ".tmp", stateFile);
        return send(res, 200, { ok: true });
      }
      if (url.pathname === "/api/file" && req.method === "GET")
        return await stream(
          res,
          await safeLibraryPath(url.searchParams.get("path") || ""),
        );
      if (url.pathname === "/api/delete-product" && req.method === "POST") {
        const data = await body(req);
        if (!data.name || data.confirmation !== data.name || !Array.isArray(data.files) || !data.files.length) throw Error("El nombre no coincide o faltan archivos");
        if (process.platform !== "win32") throw Error("La eliminación requiere Windows");
        const paths = [];
        for (const relative of [...new Set(data.files)]) {
          const file = await safeLibraryPath(relative), parts = path.relative(root,file).split(path.sep);
          if (parts.length < 2 || parts[0].toLowerCase() === '99_catalogo' || !(await fs.stat(file)).isFile()) throw Error('Archivo no permitido');
          paths.push(file);
        }
        const payload = Buffer.from(JSON.stringify(paths),'utf8').toString('base64');
        const script = "$ErrorActionPreference='Stop'; Add-Type -AssemblyName Microsoft.VisualBasic; $paths=ConvertFrom-Json ([Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + payload + "'))); foreach($file in $paths){[Microsoft.VisualBasic.FileIO.FileSystem]::DeleteFile($file,[Microsoft.VisualBasic.FileIO.UIOption]::OnlyErrorDialogs,[Microsoft.VisualBasic.FileIO.RecycleOption]::SendToRecycleBin,[Microsoft.VisualBasic.FileIO.UICancelOption]::ThrowException)}";
        await new Promise((resolve,reject)=> { const child=spawn('powershell.exe',['-NoProfile','-NonInteractive','-EncodedCommand',Buffer.from(script,'utf16le').toString('base64')],{windowsHide:true}); child.once('error',reject); child.once('exit',code=>code===0?resolve():reject(Error('No se pudieron enviar los archivos a la Papelera'))); });
        return send(res,200,{ok:true});
      }
      if (url.pathname === "/api/open-file" && req.method === "POST") {
        const file=await safeLibraryPath((await body(req)).path || '');
        if (!/\.(zip|rar|7z)$/i.test(file) || !(await fs.stat(file)).isFile()) throw Error('Solo se pueden abrir archivos ZIP, RAR o 7z');
        if(process.platform !== 'win32') throw Error('Esta función requiere Windows');
        const encoded=Buffer.from(file,'utf8').toString('base64');
        const script="$ErrorActionPreference='Stop'; $file=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('"+encoded+"')); Start-Process -FilePath $file";
        await new Promise((resolve,reject)=>{const child=spawn('powershell.exe',['-NoProfile','-NonInteractive','-EncodedCommand',Buffer.from(script,'utf16le').toString('base64')],{windowsHide:true});child.once('error',reject);child.once('exit',code=>code===0?resolve():reject(Error('No se pudo abrir la aplicación predeterminada')));});
        return send(res,200,{ok:true});
      }
      if (url.pathname === "/api/open-folder" && req.method === "POST") {
        const p = await safeLibraryPath((await body(req)).path || "");
        if (!(await fs.stat(p)).isDirectory())
          return send(res, 400, { error: "No es carpeta" });
        const child = spawn(path.join(process.env.SystemRoot || "C:\\Windows", "explorer.exe"), ["/n,", p], {
          windowsHide: false,
          detached: true,
          stdio: "ignore",
        });
        await new Promise((resolve, reject) => {
          child.once("spawn", resolve);
          child.once("error", reject);
        });
        child.unref();
        return send(res, 200, { ok: true });
      }
      if (url.pathname.startsWith("/api/"))
        return send(res, 404, { error: "No encontrado" });
      if (req.method !== "GET")
        return send(res, 405, { error: "Método no permitido" });
      const requested = path.resolve(
        dist,
        "." + decodeURIComponent(url.pathname),
      );
      if (requested !== dist && !requested.startsWith(dist + path.sep))
        return send(res, 403, { error: "Ruta inválida" });
      await stream(
        res,
        url.pathname === "/" ? path.join(dist, "index.html") : requested,
      );
    } catch (e) {
      if (!res.headersSent)
        send(res, e.code === "ENOENT" ? 404 : 400, { error: e.message });
      else res.destroy();
    }
  });
}
if (
  process.argv[1] &&
  path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)
) {
  createServer().listen(port, "127.0.0.1", () =>
    console.log("VRC Shelf: http://127.0.0.1:" + port),
  );
}


