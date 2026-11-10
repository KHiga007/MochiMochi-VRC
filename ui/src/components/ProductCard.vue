<script setup>
import { ref, onMounted, onBeforeUnmount, watch, nextTick } from "vue";
import AppIcon from "./AppIcon.vue";
import { size, fileUrl, categories } from "../lib/catalog";
const props = defineProps({
  item: Object,
  favorite: Boolean,
  showType: Boolean,
  hideAdultTag: Boolean,
  coverUrl: String,
});
defineEmits(["open", "favorite"]);
const row = ref(),
  visible = ref(0),
  broken = ref(false);
let observer;
async function fit() {
  await nextTick();
  if (!row.value) return;
  const all = [...row.value.querySelectorAll(".measure-tag")];
  let used = ((props.item.adult || props.item.category === '09_R18') && !props.hideAdultTag) ? 42 : props.item.tags.includes('herramienta:FaceTracking') ? 100 : (props.item.category === '12_Mochi_Fitter' || props.item.tags.includes('herramienta:MochiFitter')) ? 95 : 0,
    count = 0;

  if (props.item.category === '07_Herramientas_y_Sistemas') used += 52;
  for (let i = 0; i < all.length; i++) {
    const w = all[i].offsetWidth + 6,
      reserve = i === all.length - 1 ? 0 : 40;
    if (used + w + reserve > row.value.clientWidth) break;
    used += w;
    count++;
  }
  visible.value = count;
}
onMounted(() => {
  fit();
  if (row.value) {
    observer = new ResizeObserver(fit);
    observer.observe(row.value);
  }
});
onBeforeUnmount(() => observer?.disconnect());
watch(
  () => props.item,
  () => {
    broken.value = false;
    fit();
  },
);
</script>
<template>
  <article @click="$emit('open')" class="product-card" :class="{ 'adult-texture-card': item.adult || item.category === '09_R18' }">
    <div class="card-photo">
      <button
        class="cover"
        :aria-label="'Ver ' + item.name"
        @click.stop="$emit('open')"
      >
        <img
          v-if="(coverUrl || item.images.length) && !broken"
          :src="coverUrl || fileUrl(item.images[0])"
          :alt="item.name"
          loading="lazy"
          @error="broken = true"
        /><span v-else-if="item.category === '03_Accesorios'" class="accessory-placeholder">
          <svg viewBox="0 0 180 180" fill="none" aria-hidden="true">
            <circle cx="90" cy="90" r="72" fill="#fff" fill-opacity=".55"/>
            <path d="M54 47c0 35 12 59 36 70 24-11 36-35 36-70" stroke="#a58ac4" stroke-width="5" stroke-linecap="round"/>
            <path d="m90 105 19 20-19 25-19-25 19-20Z" fill="#ded0ef" stroke="#8d6dad" stroke-width="4" stroke-linejoin="round"/>
            <path d="m90 111 8 14-8 18-8-18 8-14Z" fill="#fff" fill-opacity=".7"/>
            <path d="M139 76v16m-8-8h16M42 105v10m-5-5h10" stroke="#bca6d3" stroke-width="3" stroke-linecap="round"/>
          </svg>
          <span>Accesorio</span>
        </span><span v-else-if="item.category === '11_VRM_y_Shaders'" class="vrm-placeholder">
          <svg viewBox="0 0 180 180" fill="none" aria-hidden="true">
            <circle cx="90" cy="90" r="72" fill="white" fill-opacity=".55"/>
            <path d="m90 35 46 26v54l-46 28-46-28V61l46-26Z" fill="#e0d7f3" stroke="#9279b7" stroke-width="3" stroke-linejoin="round"/>
            <path d="m44 61 46 27 46-27M90 88v55" stroke="#ad97cc" stroke-width="3"/>
            <circle cx="90" cy="69" r="12" fill="#faf7ff" stroke="#9279b7" stroke-width="3"/>
            <path d="M70 107v-9c0-11 9-19 20-19s20 8 20 19v9" fill="#faf7ff" stroke="#9279b7" stroke-width="3" stroke-linecap="round"/>
            <path d="m135 108 4 12 12 4-12 4-4 12-4-12-12-4 12-4 4-12Z" fill="#a6cfcd" stroke="#6caaa8" stroke-width="2" stroke-linejoin="round"/>
            <path d="M42 37v12m-6-6h12" stroke="#ad97cc" stroke-width="3" stroke-linecap="round"/>
          </svg>
          <span>VRM &amp; Shaders</span>
        </span><span v-else class="placeholder">◇</span></button
      ><button
        class="heart-button"
        :aria-pressed="favorite"
        :aria-label="favorite ? 'Quitar favorito' : 'Agregar favorito'"
        @click.stop="$emit('favorite')"
      >
        <AppIcon name="heart" :filled="favorite" /></button
      ><span v-if="showType" class="photo-type">{{
        categories[item.category] || item.category
      }}</span
      ><span class="photo-size">{{ size(item.size) }}</span>
    </div>
    <div class="card-copy">

      <button class="card-title" :title="item.name" @click.stop="$emit('open')">
        {{ item.name }}
      </button>
      <div v-if="item.category === '08_Mundos'" class="card-tags"><span v-if="(item.adult || item.category === '09_R18') && !hideAdultTag" class="tag r18-tag">R18</span><span class="tag">World</span></div>
      <div v-else-if="item.category === '11_VRM_y_Shaders'" class="card-tags"><span v-if="(item.adult || item.category === '09_R18') && !hideAdultTag" class="tag r18-tag">R18</span><span class="tag">{{ /Mochies|UTS2/i.test(item.name) ? 'Shader' : 'VRM' }}</span></div>
      <div v-else-if="item.generic && item.category !== '12_Mochi_Fitter'" class="card-tags"><span v-if="(item.adult || item.category === '09_R18') && !hideAdultTag" class="tag r18-tag">R18</span><span v-if="item.category === '07_Herramientas_y_Sistemas'" class="tag">Tools</span><span class="tag generic-tag">Genérico</span></div>
      <div v-else-if="['02_Ropa', '03_Accesorios', '04_Cabello', '05_Texturas', '06_Animaciones_y_Presets', '07_Herramientas_y_Sistemas', '09_R18', '10_Face_Tracking', '12_Mochi_Fitter'].includes(item.category)" ref="row" class="card-tags"><span v-if="(item.adult || item.category === '09_R18') && !hideAdultTag" class="tag r18-tag">R18</span>
        <span v-if="item.category === '07_Herramientas_y_Sistemas'" class="tag">Tools</span>
        <span v-if="item.tags.includes('herramienta:FaceTracking')" class="tag">FaceTracking</span>
        <span v-if="item.category === '12_Mochi_Fitter' || item.tags.includes('herramienta:MochiFitter')" class="tag">MochiFitter</span>

        <span
          v-for="a in item.avatars.slice(0, visible)"
          :key="a"
          class="tag"
          >{{ a }}</span
        ><button
          v-if="visible < item.avatars.length"
          class="tag more"
          :title="item.avatars.slice(visible).join(', ')"
          @click.stop="$emit('open')"
        >
          +{{ item.avatars.length - visible }}</button
        ><span v-if="!item.avatars.length && item.category !== '12_Mochi_Fitter' && !item.tags.includes('herramienta:MochiFitter')" class="muted">Por confirmar</span
        ><span class="tag-measure" aria-hidden="true"
          ><span v-for="a in item.avatars" :key="a" class="tag measure-tag">{{
            a
          }}</span></span
        >
      </div>
    </div>
  </article>
</template>
























