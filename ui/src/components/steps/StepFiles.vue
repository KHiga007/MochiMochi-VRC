<script setup>
import {inject,ref} from 'vue';
import DropZone from '../DropZone.vue';
import SettingsIcon from '../SettingsIcon.vue';
import {size} from '../../lib/catalog';
const {form,frozen,addFiles,fileNotice,totalSize}=inject('productWizard');const picker=ref();
function picked(e){addFiles([...e.target.files]);e.target.value='';}
</script>
<template><section class="wizard-step-files"><DropZone :disabled="frozen" :large="!form.files.length" :compact="!!form.files.length" @pick="picker.click()" @files="addFiles"/><input ref="picker" type="file" hidden multiple :disabled="frozen" @change="picked"/><p v-if="fileNotice" class="product-inline-notice" role="status">{{fileNotice}}</p><template v-if="form.files.length"><p class="product-files-summary">{{form.files.length}} archivos · {{size(totalSize)}}</p><ul class="product-file-list"><li v-for="(file,index) in form.files" :key="file.name"><SettingsIcon :name="/\.(png|jpe?g|webp)$/i.test(file.name)?'image':/\.(zip|rar|7z|unitypackage)$/i.test(file.name)?'archive':'file'"/><span :title="file.name">{{file.name}}</span><small>{{size(file.size)}}</small><button type="button" class="product-icon-button" :disabled="frozen" :aria-label="'Quitar '+file.name" @click="form.files.splice(index,1)"><SettingsIcon name="x"/></button></li></ul></template></section></template>
