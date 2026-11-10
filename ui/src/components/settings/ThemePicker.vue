<script setup>
import SettingsIcon from '../SettingsIcon.vue';
const props=defineProps({modelValue:String});const emit=defineEmits(['update:modelValue']);
const options=[{value:'light',label:'Claro',icon:'sun'},{value:'dark',label:'Oscuro',icon:'moon'},{value:'system',label:'Sistema',icon:'monitor'}];
function navigate(e,index){if(!['ArrowLeft','ArrowRight','ArrowUp','ArrowDown','Home','End'].includes(e.key))return;e.preventDefault();const next=e.key==='Home'?0:e.key==='End'?2:(index+(e.key==='ArrowLeft'||e.key==='ArrowUp'?-1:1)+3)%3;emit('update:modelValue',options[next].value);e.currentTarget.parentElement.children[next].focus();}
</script>
<template><div class="config-theme-picker" role="radiogroup" aria-label="Tema"><button v-for="(option,i) in options" :key="option.value" type="button" role="radio" :aria-checked="modelValue===option.value" :tabindex="modelValue===option.value?0:-1" @click="emit('update:modelValue',option.value)" @keydown="navigate($event,i)"><SettingsIcon :name="option.icon"/><span>{{option.label}}</span></button></div></template>