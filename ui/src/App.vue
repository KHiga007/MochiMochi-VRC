<script setup>
import { ref, shallowRef, computed, watch, onMounted, onBeforeUnmount, nextTick } from "vue";
import { offline } from "./lib/api";
import { apiFetch } from "./lib/api";
import SettingsPanel from "./components/SettingsPanel.vue";
import SettingsIcon from "./components/SettingsIcon.vue";
import AddProductAllModal from "./components/AddProductAllModal.vue";
const addingAll = ref(false);
import PaginationNav from "./components/PaginationNav.vue";
import FilterBar from "./components/FilterBar.vue";
import {useLibraryFilters,matchesLibrarySelections} from "./composables/useLibraryFilters";
import "./styles/library-filter-bar.css";
import "./styles/modal-motion.css";
const {state:libraryFilters,query,onlyFavorites,perPage,viewMode,sortBy,chosenCategories,chosenTags,chosenAvatars,includeGeneric,clear:clearLibraryFilters}=useLibraryFilters();
import ProductCard from "./components/ProductCard.vue";
import ProductModal from "./components/ProductModal.vue";
import AvatarPicker from "./components/AvatarPicker.vue";
import AppIcon from "./components/AppIcon.vue";
import {
  buildCatalog,
  categories,
  size,
  matchesAvatar,
  familyTerms,
  compare,
} from "./lib/catalog";
const raw = shallowRef([]),
  families = shallowRef({}),
  items = shallowRef([]),
  personal = ref({}),
  category = ref(new URLSearchParams(location.search).get("review") === "similar" ? "02_Ropa" : ""),

  selected = ref([]),
  singleAvatar = ref(""),
  group = ref(""),
  format = ref(""),
  adult = ref(true),
  preview = ref(false),
  duplicates = ref(false),


  page = ref(1),
  current = ref(null),
  loading = ref(true),
  error = ref(""),
  notice = ref(""),
  menu = ref(false),
  importInput = ref(),
  searchInput = ref();
let noticeTimer,
  saveQueue = Promise.resolve();
const avatarNames = computed(() =>
  [
    ...new Set([
      ...items.value.flatMap((x) => x.avatars),
      ...Object.keys(families.value),
      ...Object.values(families.value).flat(),
    ]),
  ].sort(compare),
);
const temporaryReview = ref(new URLSearchParams(location.search).get("review") === "similar");
const reviewNames = new Set(["小悪魔セーラー", "小悪魔セーラー ❤︎", "Succubus Bunny", "Bunny Succubus", "Lime_Lace__Sport_Swimsuits", "Sport and Lace Swimsuit for Selestia", "NAKAYOSHI", "NAKAYOSHIもーど", "Noir", "Noir Suit", "anomaly", "Distorted Anomaly"]);
function clearReview() { temporaryReview.value = false; const url = new URL(location.href); url.searchParams.delete("review"); history.replaceState(null, "", url); }
const desktop=Boolean(window.__VRC_DESKTOP__);
const settingsOpen=ref(false),scanning=ref(false),lastRead=ref(0);
const theme=ref(localStorage.getItem('vrc-shelf-theme')||'system');
const systemTheme=window.matchMedia('(prefers-color-scheme: dark)');
function applyTheme(){document.documentElement.dataset.theme=theme.value==='system'?(systemTheme.matches?'dark':'light'):theme.value;}
watch(theme,()=>{localStorage.setItem('vrc-shelf-theme',theme.value);applyTheme();});applyTheme();
const nativeRefresh=()=>reload();

const highlightedPath=ref('');let highlightTimer;
function isNewProduct(item){return !!highlightedPath.value&&(item.id===highlightedPath.value||item.files.some(f=>f.relativa.startsWith(highlightedPath.value+'\\')));}
function productAdded(path){addingAll.value=false;toast('Producto guardado. Presiona Actualizar para incorporarlo a la lista.');}
function productDeleted(paths){const removed=new Set(paths);raw.value=raw.value.filter(f=>!removed.has(f.relativa));items.value=buildCatalog(raw.value);current.value=null;toast('Variante o producto eliminado');}
const tagFilter=ref(''),openFilter=ref('');
const categoryFilterOptions=computed(()=>Object.entries(categories).map(([value,label])=>({value,label,count:items.value.filter(x=>x.category===value).length})));
const avatarFilterOptions=computed(()=>avatarNames.value.map(value=>({value,label:value,count:items.value.filter(x=>matchesAvatar(x,value,families.value)).length})));
const tagFilterOptions=computed(()=>tagOptions.value.map(value=>({value,label:value,count:items.value.filter(x=>productTags(x).includes(value)).length})));

function productTags(x) {
 const tags=x.tags.filter(t=>/^(avatar:|herramienta:|origen:|plataforma:|tag:)/.test(t)).map(t=>t.slice(t.indexOf(':')+1));
 if(x.generic) tags.push('Genérico'); if(x.adult) tags.push('R18');
 if(x.category==='08_Mundos') tags.push('World'); if(x.category==='12_Mochi_Fitter') tags.push('MochiFitter');
 return [...new Set(tags)];
}
const tagOptions=computed(()=>[...new Set(['Booth','PM',...items.value.flatMap(productTags)])].sort(compare));
const filtered = computed(() => {
  const words = query.value.toLowerCase().trim().split(/\s+/).filter(Boolean);
  return items.value.filter((x) => {
    const p = personal.value[x.id] || {};
    return (
      (!temporaryReview.value || (x.category === "02_Ropa" && reviewNames.has(x.name))) &&
      matchesLibrarySelections(x,libraryFilters,productTags(x),(item,avatar)=>matchesAvatar(item,avatar,families.value)) &&
      (!onlyFavorites.value || p.favorite) &&
      (adult.value || !x.adult) &&
      (!preview.value || x.images.length) &&
      (!duplicates.value || x.dup) &&
      (!format.value ||
        x.files.some((f) =>
          f.nombre.toLowerCase().endsWith("." + format.value),
        )) &&
      (!singleAvatar.value ||
        matchesAvatar(x, singleAvatar.value, families.value)) &&
      (category.value !== "02_Ropa" ||
        !selected.value.length ||
        selected.value.some((a) => matchesAvatar(x, a, families.value))) &&
      (!group.value ||
        (group.value === "r18"
          ? x.adult
          : x.variant === group.value && !x.adult)) &&
      words.every((w) =>
        (
          x.name +
          " " +
          x.id +
          " " +
          x.tags.join(" ") +
          " " +
          familyTerms(x, families.value) +
          " " +
          (p.tags || []).join(" ") +
          " " +
          (p.notes || "")
        )
          .toLowerCase()
          .includes(w),
      )
    );
  });
});
const limit = computed(() =>
  perPage.value === "all"
    ? Math.max(1, filtered.value.length)
    : Number(perPage.value),
);
const pages = computed(() =>
  Math.max(1, Math.ceil(filtered.value.length / limit.value)),
);
watch(pages,total=>{if(page.value>total)page.value=total;});
const sorted=computed(()=>[...filtered.value].sort((a,b)=>sortBy.value==='za'?compare(b.name,a.name):sortBy.value==='largest'?b.size-a.size:sortBy.value==='smallest'?a.size-b.size:compare(a.name,b.name)));
const shown = computed(() =>
  sorted.value.slice(
    (page.value - 1) * limit.value,
    page.value * limit.value,
  ),
);
const favoritesCount = computed(
  () => items.value.filter((x) => personal.value[x.id]?.favorite).length,
);
const heading = computed(() =>
  onlyFavorites.value
    ? "Tus favoritos"
    : (categories[category.value] || "Explorá tu colección") +
      (group.value
        ? " · " + { normal: "Normal", chibi: "Chibi", r18: "R18" }[group.value]
        : ""),
);
function toast(s) {
  notice.value = s;
  clearTimeout(noticeTimer);
  noticeTimer = setTimeout(() => (notice.value = ""), 3500);
}
let refreshing = false;
async function reload(silent = false) {
  silent = silent === true;
  if (refreshing) return;
  refreshing = true;
  scanning.value = true;
  if (!silent) { loading.value = true; error.value = ""; }
  try {
    const res = await apiFetch("/api/catalog");
    if (!res.ok) throw Error((await res.json()).error);
    const data = await res.json();
    lastRead.value=Date.now();
    const catalog = buildCatalog(data.raw);
    raw.value = data.raw;
    families.value = data.families;
    items.value = catalog;
    if (current.value)
      current.value =
        items.value.find((x) => x.id === current.value.id) || null;
    if (!silent) page.value = 1;
    else page.value = Math.min(page.value, pages.value);
  } catch (e) {
    if (!silent) error.value = "No se pudo cargar la biblioteca: " + e.message;
  } finally {
    loading.value = false;
    refreshing = false;
    scanning.value = false;
  }
}
function save() {
  try {
    localStorage.setItem("vrc-shelf-personal", JSON.stringify(personal.value));
  } catch {}
  const snapshot = JSON.stringify(personal.value);
  saveQueue = saveQueue
    .catch(() => {})
    .then(async () => {
      const r = await apiFetch("/api/personal", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: snapshot,
      });
      if (!r.ok)
        toast("No se pudo guardar el respaldo en disco. Exportá tus datos.");
    });
}
function favorite(id) {
  personal.value[id] = {
    ...personal.value[id],
    favorite: !personal.value[id]?.favorite,
  };
  save();
}
function chooseCategory(k) {
  category.value = k;
  group.value = "";
  singleAvatar.value = "";
  menu.value = false;
}
function reset() {
  query.value = "";
  tagFilter.value = "";
  clearLibraryFilters(); openFilter.value="";
  selected.value = [];
  singleAvatar.value = "";
  group.value = "";
  format.value = "";
  adult.value = true;
  preview.value = false;
  duplicates.value = false;
  onlyFavorites.value = false;
  page.value = 1;
}
function exportData() {
  const a = document.createElement("a"),
    url = URL.createObjectURL(
      new Blob([JSON.stringify(personal.value, null, 2)], {
        type: "application/json",
      }),
    );
  a.href = url;
  a.download = "vrc-shelf-favoritos-notas.json";
  a.click();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}
async function importData(e) {
  try {
    const file = e.target.files[0];
    if (!file) return;
    const value = JSON.parse(await file.text());
    if (!value || typeof value !== "object" || Array.isArray(value))
      throw Error("Formato inválido");
    const clean = {};
    for (const [k, v] of Object.entries(value)) {
      if (!v || typeof v !== "object" || (v.tags && !Array.isArray(v.tags)))
        throw Error("Formato inválido");
      clean[k] = {
        favorite: !!v.favorite,
        tags: (v.tags || []).filter((t) => typeof t === "string"),
        notes: typeof v.notes === "string" ? v.notes : "",
      };
    }
    personal.value = { ...personal.value, ...clean };
    mergeFavorites();
    save();
    toast("Respaldo importado");
  } catch (e) {
    toast("No se pudo importar: " + e.message);
  }
  e.target.value = "";
}
function mergeFavorites() {
  for (const item of items.value) {
    const ids = item.variants?.map((v) => v.id) || [item.id];
    const records = Object.entries(personal.value)
      .filter(
        ([k]) =>
          ids.includes(k) ||
          (item.category === "01_Avatares" && k.startsWith(item.id + "\\")),
      )
      .map(([, v]) => v);
    if (records.length)
      personal.value[item.id] = {
        favorite: records.some((v) => v.favorite),
        tags: [...new Set(records.flatMap((v) => v.tags || []))],
        notes: [...new Set(records.map((v) => v.notes).filter(Boolean))].join(
          "\n\n",
        ),
      };
  }
}
watch(
  [
    category,
    query,
    selected,
    singleAvatar,
    group,
    format,
    adult,
    preview,
    duplicates,
    onlyFavorites,
    perPage,
    sortBy,
    tagFilter,
    chosenCategories,
    chosenTags,
    chosenAvatars,
    includeGeneric,
  ],
  () => (page.value = 1),
  { deep: true },
);
watch(group, (v) => {
  if (v === "r18") adult.value = true;
});
const keyboard = (e) => {
  if(desktop && e.key==='F5'){e.preventDefault();reload();return;}
  if ((e.ctrlKey || e.metaKey) && e.key === "k") {
    e.preventDefault();
    searchInput.value?.focus();
  }
};
onMounted(async () => {
  try {
    const r = await apiFetch("/api/personal");
    if (!r.ok) throw Error();
    personal.value = await r.json();
    if (!Object.keys(personal.value).length)
      personal.value = JSON.parse(
        localStorage.getItem("vrc-shelf-personal") || "{}",
      );
  } catch {
    toast("No se pudo leer el respaldo de favoritos");
  }
  await reload();
  mergeFavorites();
  window.addEventListener("keydown", keyboard);
  window.addEventListener("shelf-refresh",nativeRefresh);
  systemTheme.addEventListener("change",applyTheme);
});
onBeforeUnmount(() => {
  clearTimeout(noticeTimer);
  clearTimeout(highlightTimer);
  window.removeEventListener("keydown", keyboard);
  window.removeEventListener("shelf-refresh",nativeRefresh);
  systemTheme.removeEventListener("change",applyTheme);
});
</script>
<template>
<header class="app-header shelf-navbar"><button class="brand" @click="onlyFavorites=false; category=''; reset()" aria-label="MochiMochi VRC">MochiMochi<span class="brand-vrc">VRC</span></button><label class="global-search"><AppIcon name="search"/><input ref="searchInput" v-model="query" type="search" aria-label="Buscar" placeholder="Buscar en tu biblioteca…"/></label><div class="navbar-actions"><button v-if="!offline" class="icon-button nav-icon-add" aria-label="Agregar producto" title="Agregar producto" @click="addingAll=true"><SettingsIcon name="plus"/></button><button v-if="desktop" class="icon-button desktop-settings-button" aria-label="Ajustes" title="Ajustes" @click="settingsOpen=true"><SettingsIcon name="settings"/></button></div></header>
<div class="app-wrap shelf-browser">
<main><FilterBar :state="libraryFilters" :category-options="categoryFilterOptions" :avatar-options="avatarFilterOptions" :tag-options="tagFilterOptions" :count="filtered.length" :total="items.length" @clear="reset"/><div v-if="error" class="error-panel">{{error}}<button @click="reload">Reintentar</button></div><p v-else-if="loading" class="loading">Leyendo biblioteca…</p><div v-else class="product-grid shelf-products" :class="{'list-view':viewMode==='list'}"><ProductCard v-for="item in shown" :key="item.id" :item="item" :favorite="!!personal[item.id]?.favorite" :class="{'product-just-added':isNewProduct(item)}" :show-type="!category" @open="current=item" @favorite="favorite(item.id)"/><div v-if="!shown.length" class="empty"><h3>No hay productos con estos filtros</h3><button type="button" @click="reset">Limpiar filtros</button></div></div>
<PaginationNav v-model="page" :total="pages" :count="filtered.length"/></main></div>
<SettingsPanel v-if="settingsOpen" :scanning="scanning" :count="items.length" :bytes="raw.reduce((n,f)=>n+f.bytes,0)" :last-read="lastRead" :theme="theme" @close="settingsOpen=false" @refresh="reload()" @update:theme="theme=$event"/><AddProductAllModal :category-usage="Object.fromEntries(Object.keys(categories).map(c=>[c,items.filter(i=>i.category===c).length]))" v-if="addingAll" :avatars="avatarNames" :tag-options="tagOptions" @close="addingAll=false" @added="addingAll=false; productAdded($event)"/><ProductModal v-if="current" :key="current.id" :item="current" :favorite="!!personal[current.id]?.favorite" :personal="personal[current.id]||{}" :preferred="selected" @close="current=null" @favorite="favorite(current.id)" @error="toast" @deleted="productDeleted"/><div v-if="notice" class="toast" role="status">{{notice}}</div>
</template>
