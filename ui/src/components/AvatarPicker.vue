<script setup>
import { ref, computed } from "vue";
import AppIcon from "./AppIcon.vue";
const props = defineProps({ names: Array, modelValue: Array });
const emit = defineEmits(["update:modelValue"]);
const query = ref(""),
  open = ref(false),
  input = ref();
const results = computed(() =>
  props.names.filter(
    (a) =>
      a.toLowerCase().includes(query.value.trim().toLowerCase()) &&
      !props.modelValue.includes(a),
  ),
);
function select(a) {
  emit("update:modelValue", [...props.modelValue, a]);
  query.value = "";
  input.value.focus();
}
function clear() {
  query.value = "";
  emit("update:modelValue", []);
}
</script>
<template>
  <section class="avatar-picker">
    <div class="picker-row">
      <div class="picker-input">
        <AppIcon name="search" /><input
          ref="input"
          v-model="query"
          placeholder="Buscar avatares…"
          aria-label="Buscar avatares"
          role="combobox"
          :aria-expanded="open && !!query"
          aria-controls="avatar-results"
          @focus="open = true"
          @blur="open = false"
          @keydown.escape="open = false"
          @keydown.enter.prevent="results.length && query && select(results[0])"
        />
        <div
          v-if="open && query.trim()"
          id="avatar-results"
          class="picker-results"
          role="listbox"
        >
          <button
            v-for="a in results"
            :key="a"
            role="option"
            @mousedown.prevent="select(a)"
          >
            {{ a }} <span>+</span>
          </button>
          <p v-if="!results.length">No hay coincidencias</p>
        </div>
      </div>
      <button
        class="icon-button trash"
        aria-label="Limpiar selección"
        title="Limpiar selección"
        @click="clear"
      >
        <AppIcon name="trash" />
      </button>
    </div>
    <div v-if="modelValue.length" class="selected-badges">
      <button
        v-for="a in modelValue"
        :key="a"
        class="tag selected"
        :aria-label="'Quitar ' + a"
        @click="
          emit(
            'update:modelValue',
            modelValue.filter((v) => v !== a),
          )
        "
      >
        {{ a }} ×
      </button>
    </div>
  </section>
</template>
