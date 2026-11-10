<script setup>
import SettingsIcon from '../SettingsIcon.vue';
import SettingsCard from './SettingsCard.vue';
import SettingsRow from './SettingsRow.vue';
import {author} from '../../config/app';
defineProps({version:String});defineEmits(['licenses']);

</script>
<template><div class="config-about-identity"><span class="config-logo large" aria-hidden="true">✦</span><h2>MochiMochi <span>VRC</span></h2><span class="config-version">{{version?'Versión '+version:'Leyendo versión…'}}</span><p>Tu biblioteca local de recursos para VRChat.</p><small v-if="author">Hecho por {{author}}</small></div><div class="config-about-sections"><section><h3>CRÉDITOS Y LICENCIAS</h3><SettingsCard><SettingsRow icon="file" title="Licencias de código abierto" subtitle="Herramientas, fuentes e íconos que usa la app" @click="$emit('licenses')"/></SettingsCard></section></div></template>