<script setup>
import { ref, computed, onMounted, onBeforeUnmount, watch, nextTick } from "vue";
import { offline } from "../lib/api";
import {closeModalSurface} from "../lib/modalMotion";
import AppIcon from "./AppIcon.vue";
import { fileUrl, size, compare, folderOf } from "../lib/catalog";
const props = defineProps({
  item: Object,
  favorite: Boolean,
  personal: Object,
  preferred: Array,
});
function requestClose(){if(deleteBusy.value)return;closeModalSurface(dialog.value,()=>emit('close'));}
const emit = defineEmits(["close", "favorite", "save", "error", "deleted"]);
const enlargedImage = ref('');
const deleting = ref(false), confirmation = ref(''), deleteBusy = ref(false), deleteError = ref('');
const visibleTags = computed(() => [...new Set(props.item.tags.filter(t => /^(tag:|herramienta:|origen:|plataforma:)/.test(t)).map(t => t.replace(/^[^:]+:/, '')).filter(t => t.toUpperCase() !== 'R18'))]);
async function openFile(file) {
  if (/\.(png|jpe?g|webp|gif|bmp|avif)$/i.test(file.relativa)) { enlargedImage.value=fileUrl(file); return; }
  if (/\.(zip|rar|7z)$/i.test(file.relativa)) {
    try { const r=await fetch('/api/open-file',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({path:file.relativa})}); if(!r.ok)throw Error((await r.json()).error || 'No se pudo abrir el archivo'); } catch(e) {emit('error',e.message);}
    return;
  }
  window.open(fileUrl(file),'_blank','noopener');
}
async function deleteProduct() {
  if (confirmation.value !== props.item.name || deleteBusy.value) return;
  deleteBusy.value = true; deleteError.value = '';
  const deletedPaths=current.value.files.map(f=>f.relativa);
  try {
    const r = await fetch('/api/delete-product', {method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({name:props.item.name,confirmation:confirmation.value,files:deletedPaths})});
    if (!r.ok) throw Error((await r.json()).error || 'No se pudo eliminar');
    await closeModalSurface(dialog.value,()=>emit('deleted',deletedPaths));
  } catch(e) { deleteError.value = e.message; } finally { deleteBusy.value = false; }
}
watch(deleting, async value => { if(value) { await nextTick(); dialog.value?.querySelector('.delete-confirm-panel input')?.focus(); } });
const dialog = ref(),
  variant = ref(0),
  photo = ref(0),
  displayPhoto = ref("");
let ticket = 0;
const grouped = computed(() => props.item.variants?.length > 1),
  avatar = computed(() => props.item.category === "01_Avatares"),
  clothing = computed(() => props.item.category === "02_Ropa"), hair = computed(() => props.item.category === "04_Cabello");
const current = computed(() =>
  grouped.value ? props.item.variants[variant.value] : props.item,
);
const images = computed(() => current.value.images);
const files = computed(() =>
  [...current.value.files].sort((a, b) => compare(a.relativa, b.relativa)),
);
const version = computed(
  () =>
    props.item.name.match(/\b\d+\.\d+(?:\.\d+)*/)?.[0] ||
    props.item.files.map((f) => f.version).find(Boolean) ||
    "—",
);
const name = computed(() =>
  avatar.value
    ? props.item.name.replace(
        /\s+\d+\.\d+(?:\.\d+)*(?:\s+\d{4}-\d{2}-\d{2})?/,
        "",
      )
    : props.item.name,
);
async function loadPhoto() {
  const t = ++ticket,
    f = images.value[photo.value];
  if (!f) {
    displayPhoto.value = "";
    return;
  }
  const src = fileUrl(f);
  try {
    const img = new Image();
    img.src = src;
    await img.decode();
    if (t === ticket) displayPhoto.value = src;
  } catch {
    if (t === ticket) displayPhoto.value = "";
  }
}
watch([current, photo], loadPhoto);
function choose(i) {
  variant.value = i;
  photo.value = 0;
}
function step(delta) {
  photo.value =
    (photo.value + delta + images.value.length) % images.value.length;
}
async function openFolder() {
  if (offline) {
    window.open(fileUrl(folderOf(current.value)) + "/", "_blank", "noopener");
    return;
  }
  try {
    const r = await fetch("/api/open-folder", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ path: folderOf(current.value) }),
    });
    if (!r.ok) throw Error((await r.json()).error);
  } catch (e) {
    emit("error", e.message);
  }
}
const keyboard = (e) => {
  if (
    e.key === "ArrowLeft" &&
    !["INPUT", "TEXTAREA"].includes(e.target.tagName) &&
    images.value.length
  )
    step(-1);
  if (
    e.key === "ArrowRight" &&
    !["INPUT", "TEXTAREA"].includes(e.target.tagName) &&
    images.value.length
  )
    step(1);
};
onMounted(() => {
  if (grouped.value) {
    const i = props.item.variants.findIndex((v) =>
      v.avatars.some((a) => props.preferred?.includes(a)),
    );
    if (i >= 0) variant.value = i;
  }
  dialog.value.showModal();
  dialog.value.scrollTop = 0;
  dialog.value.querySelector('.detail-close')?.focus({preventScroll:true});
  document.body.style.overflow = "hidden";
  loadPhoto();
  window.addEventListener("keydown", keyboard);
});
onBeforeUnmount(() => {
  ticket++;
  document.body.style.overflow = "";
  window.removeEventListener("keydown", keyboard);
});
</script>
<template>
  <dialog ref="dialog" class="product-modal unified-product-modal" :aria-label="item.name" @cancel.prevent="enlargedImage ? enlargedImage='' : deleting ? deleting=false : requestClose()" @click="e => e.target === dialog && !deleteBusy && (deleting ? deleting=false : requestClose())">
    <div class="product-detail-content">
      <button class="icon-button detail-close" aria-label="Cerrar" @click="requestClose"><AppIcon name="close"/></button>
      <div class="product-detail-top">
        <div class="detail-preview"><button v-if="displayPhoto" class="detail-preview-open" aria-label="Ampliar imagen" @click="enlargedImage=displayPhoto"><img :src="displayPhoto" :alt="item.name"/></button><span v-else class="placeholder">◇</span><div v-if="images.length > 1" class="detail-photo-nav"><button aria-label="Anterior" @click="step(-1)">‹</button><small>{{photo+1}} / {{images.length}}</small><button aria-label="Siguiente" @click="step(1)">›</button></div></div>
        <div class="detail-description"><h2>{{item.name}}</h2><div class="compatible-list"><span v-if="item.adult || item.category === '09_R18'" class="tag r18-tag">R18</span><span v-for="tag in visibleTags" :key="tag" class="tag">{{tag}}</span></div><h3>Avatares compatibles</h3><div class="compatible-list"><span v-if="item.generic" class="tag generic-tag">Genérico</span><template v-else><span v-for="a in item.avatars" :key="a" class="tag">{{a}}</span><span v-if="!item.avatars.length" class="muted">Por confirmar</span></template></div>
      <div v-if="grouped" class="detail-variants"><label>Versión <select :disabled="deleteBusy" :value="variant" @change="choose(Number($event.target.value))"><option v-for="(v,i) in item.variants" :key="v.id" :value="i">{{v.avatars.join(' · ') || v.name}}</option></select></label></div>
      <div class="files-heading"><h3>Archivos incluidos</h3><small>{{files.length}} archivos</small></div>
      <div class="detail-files"><a v-for="f in files" :key="f.relativa" :href="fileUrl(f)" :title="f.relativa" @click.prevent="openFile(f)"><span>{{f.relativa.slice(current.id.length+1) || f.nombre}}</span><small>{{size(f.bytes)}}</small></a></div>
        </div>
      </div>
    </div>
    <footer class="detail-actions"><button class="secondary detail-delete" @click="deleting=true; confirmation=''; deleteError=''"><AppIcon name="trash"/>Eliminar</button><button class="primary" @click="openFolder"><AppIcon name="folder"/>Abrir carpeta</button><button class="secondary" :aria-pressed="favorite" @click="emit('favorite')"><AppIcon name="heart" :filled="favorite"/>{{favorite ? 'Quitar favorito' : 'Favorito'}}</button></footer>
    <Transition name="modal-overlay"><div v-if="enlargedImage" class="image-viewer-overlay" role="dialog" aria-modal="true" aria-label="Vista ampliada" @click.self="enlargedImage=''" @keydown.esc.stop.prevent="enlargedImage=''"><button class="icon-button image-viewer-close" aria-label="Cerrar imagen" @click="enlargedImage=''"><AppIcon name="close"/></button><img :src="enlargedImage" :alt="item.name"/></div>
    </Transition><Transition name="modal-overlay"><div v-if="deleting" class="delete-confirm-overlay" @click.self="!deleteBusy && (deleting=false)"><section role="dialog" aria-modal="true" aria-labelledby="delete-title" class="delete-confirm-panel"><h2 id="delete-title">Eliminar producto</h2><p>Se enviarán los archivos de este producto a la Papelera de reciclaje. Escribe el nombre exacto para confirmar:</p><strong>{{item.name}}</strong><p v-if="grouped">Solo la variante seleccionada: {{current.avatars.join(' · ')}} ({{files.length}} archivos).</p><input v-model="confirmation" :disabled="deleteBusy" aria-label="Nombre del producto para confirmar" autocomplete="off" @keydown.enter.prevent="deleteProduct"/><p v-if="deleteError" role="alert">{{deleteError}}</p><div><button class="secondary" :disabled="deleteBusy" @click="deleting=false">Cancelar</button><button class="detail-delete" :disabled="confirmation !== item.name || deleteBusy" @click="deleteProduct">{{deleteBusy ? 'Eliminando…' : 'Eliminar producto'}}</button></div></section></div>
    </Transition>
  </dialog>
</template>
