<script setup>
import {inject} from 'vue';
import ChipInput from '../ChipInput.vue';
const {form,frozen,avatarOptions,tagOptions}=inject('productWizard');
</script>
<template><section class="wizard-step-compat"><div class="wizard-generic-option"><div><strong id="generic-switch-label">Genérico</strong><small id="generic-switch-hint">Sirve para cualquier avatar</small></div><button type="button" role="switch" class="wizard-generic-switch" :aria-checked="form.generic" aria-labelledby="generic-switch-label" aria-describedby="generic-switch-hint" :disabled="frozen" @click="form.generic=!form.generic"><span aria-hidden="true"></span></button></div><div v-if="!form.generic" class="product-field wizard-avatar-chips"><label>Avatares compatibles</label><ChipInput v-model="form.avatars" :options="avatarOptions" label="Avatares compatibles" placeholder="Buscar avatares…" icon="search" chips-below :disabled="frozen||form.generic"/></div><div class="product-field wizard-tag-chips"><label>Tags</label><ChipInput v-model="form.tags" :options="tagOptions" label="Tags" placeholder="Buscar o crear tag — Enter para agregar" icon="tag" chips-below allow-create :disabled="frozen"/></div></section></template>

