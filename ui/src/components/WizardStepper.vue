<script setup>
import SettingsIcon from './SettingsIcon.vue';
defineProps({step:Number,completed:Number,disabled:Boolean});defineEmits(['navigate']);
const steps=['Archivos','Detalles','Compatibilidad','Resumen'];
</script>
<template><ol class="wizard-stepper" aria-label="Progreso para agregar producto"><li v-for="(label,i) in steps" :key="label" :class="{current:step===i+1,complete:i+1<completed}" :aria-current="step===i+1?'step':undefined"><button type="button" :disabled="disabled||i+1>completed" :aria-label="'Paso '+(i+1)+': '+label" @click="$emit('navigate',i+1)"><span class="wizard-step-circle"><SettingsIcon v-if="i+1<completed&&step!==i+1" name="check"/><template v-else>{{i+1}}</template></span><span class="wizard-step-label">{{label}}</span></button><span v-if="i<steps.length-1" class="wizard-step-line" aria-hidden="true"></span></li></ol></template>
