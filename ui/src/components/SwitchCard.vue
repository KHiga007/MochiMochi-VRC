<script setup>
defineProps({modelValue:Boolean,title:String,description:String,disabled:Boolean});
const emit=defineEmits(['update:modelValue']);
</script>
<template><button type="button" class="compact-switch-card" :aria-pressed="modelValue" :disabled="disabled" @click="emit('update:modelValue',!modelValue)"><span class="compact-switch-track" :class="{on:modelValue}" aria-hidden="true"><span/></span><span><strong>{{title}}</strong><small>{{description}}</small></span></button></template>