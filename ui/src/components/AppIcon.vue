<script setup>
defineProps({ name: String, filled: Boolean });
</script>
<template>
  <svg
    width="18"
    height="18"
    viewBox="0 0 24 24"
    :fill="name === 'heart' && filled ? 'currentColor' : 'none'"
    stroke="currentColor"
    stroke-width="1.8"
    stroke-linecap="round"
    stroke-linejoin="round"
    aria-hidden="true"
  >
    <path
      v-if="name === 'heart'"
      d="M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.7l-1.1-1.1a5.5 5.5 0 0 0-7.8 7.8L12 21l8.8-8.6a5.5 5.5 0 0 0 0-7.8Z"
    />
    <path
      v-else-if="name === 'folder'"
      d="M3 7V5a2 2 0 0 1 2-2h5l2 3h7a2 2 0 0 1 2 2v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2Z"
    />
    <path
      v-else-if="name === 'trash'"
      d="M3 6h18M9 6V4h6v2M5 6l1 14h12l1-14M10 10v6M14 10v6"
    />
    <path
      v-else-if="name === 'search'"
      d="m21 21-5-5M18 10a8 8 0 1 1-16 0 8 8 0 0 1 16 0"
    />
    <path
      v-else-if="name === 'refresh'"
      d="M20 7v5h-5M4 17v-5h5M6 6a8 8 0 0 1 13 2M5 16a8 8 0 0 0 13 2"
    />
    <path v-else d="m6 6 12 12M18 6 6 18" />
  </svg>
</template>
