import http from 'node:http';
import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';
test('carga varios archivos, guarda tags y rechaza rutas y duplicados',async()=>{
 const root=await fs.mkdtemp(path.join(os.tmpdir(),'shelf-upload-test-'));
 process.env.VRC_LIBRARY_ROOT=root;
 await fs.mkdir(path.join(root,'99_Catalogo'));
 const {createServer}=await import('../server/index.mjs');const server=createServer();
 await new Promise(r=>server.listen(0,'127.0.0.1',r));
 const base='http://127.0.0.1:'+server.address().port;
 async function post(route,data,binary=false){return new Promise((resolve,reject)=>{const req=http.request(base+route,{method:'POST',headers:{host:'127.0.0.1:4173',origin:'http://127.0.0.1:4173','content-type':binary?'application/octet-stream':'application/json'}},res=>{let text='';res.on('data',c=>text+=c);res.on('end',()=>resolve({status:res.statusCode,json:async()=>JSON.parse(text)}));});req.on('error',reject);req.end(binary?data:JSON.stringify(data));});}
 const data={name:'Producto de prueba',category:'02_Ropa',avatars:['Airi'],tags:'Booth',files:[{name:'foto.jpg',size:3},{name:'paquete.zip',size:4}]};
 try{
 assert.equal((await post('/api/upload/start',{...data,name:'../escape'})).status,400);
 assert.equal((await post('/api/upload/start',{...data,files:[data.files[0],data.files[0]]})).status,400);
 const start=await post('/api/upload/start',data);assert.equal(start.status,200);const {token}=await start.json();
 assert.equal((await post('/api/upload/finish',{token})).status,400);
 assert.equal((await post('/api/upload/file?token='+token+'&name=foto.jpg','abc',true)).status,200);
 assert.equal((await post('/api/upload/file?token='+token+'&name=foto.jpg','abc',true)).status,400);
 assert.equal((await post('/api/upload/file?token='+token+'&name=paquete.zip','1234',true)).status,200);
 assert.equal((await post('/api/upload/finish',{token})).status,200);
 assert.equal(await fs.readFile(path.join(root,'02_Ropa','Airi',data.name,'paquete.zip'),'utf8'),'1234');
 const overrides=JSON.parse(await fs.readFile(path.join(root,'99_Catalogo','compatibilidades-confirmadas.json'),'utf8'));assert.deepEqual(overrides[0].avatares,['Airi']);assert(overrides[0].etiquetas.includes('tag:Booth'));
 assert.equal((await post('/api/upload/start',data)).status,400);
 const interrupted={...data,name:'Interrumpido'};const first=await(await post('/api/upload/start',interrupted)).json();const second=await(await post('/api/upload/start',interrupted)).json();assert.notEqual(first.token,second.token);assert.equal((await post('/api/upload/cancel',{token:first.token})).status,200);assert.equal((await post('/api/upload/cancel',{token:second.token})).status,200);await assert.rejects(fs.stat(path.join(root,'02_Ropa','Airi','Interrumpido')));
 const generic={...data,name:'Generico',generic:true,files:[{name:'a.zip',size:1}]};const started=await(await post('/api/upload/start',generic)).json();assert.equal((await post('/api/upload/file?token='+started.token+'&name=a.zip','x',true)).status,200);assert.equal((await post('/api/upload/finish',{token:started.token})).status,200);assert.equal(await fs.readFile(path.join(root,'02_Ropa','Mix','Generico','a.zip'),'utf8'),'x');

 }finally{await new Promise(r=>server.close(r));if(path.dirname(root)===os.tmpdir()&&path.basename(root).startsWith('shelf-upload-test-'))await fs.rm(root,{recursive:true,force:true});}
});
