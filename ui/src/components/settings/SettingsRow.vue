<script setup>
import SettingsIcon from '../SettingsIcon.vue';
defineProps({icon:String,title:String,subtitle:String,disabled:Boolean,spinning:Boolean});defineEmits(['click']);
</script>
<template><button type="button" class="config-row" :disabled="disabled" :aria-disabled="disabled||undefined" @click="$emit('click')"><SettingsIcon :name="icon" :class="{'config-spinning':spinning}"/><span class="config-row-copy"><strong>{{title}}</strong><small v-if="subtitle">{{subtitle}}</small></span><span class="config-row-trailing"><slot><SettingsIcon name="chevron"/></slot></span></button></template>