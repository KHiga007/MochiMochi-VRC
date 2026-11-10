<script setup>
import {ref,computed,onMounted,onBeforeUnmount,watch,nextTick} from 'vue';
const props=defineProps({modelValue:[Array,String],options:Array,label:String,multiple:Boolean,placeholder:String,disabled:Boolean,night:Boolean,allowCreate:Boolean});
const emit=defineEmits(['update:modelValue']);
const open=ref(false),search=ref('');
const trigger=ref(),panel=ref();
const dropdownId=Symbol('dropdown');
function positionPanel(){
 if(!open.value || !panel.value || !trigger.value)return;
 const rect=trigger.value.getBoundingClientRect(), el=panel.value;
 const width=Math.min(rect.width,window.innerWidth-24);
 el.style.width=width+'px';el.style.left=Math.max(12,Math.min(rect.left,window.innerWidth-width-12))+'px';
 const height=el.getBoundingClientRect().height;
 const below=window.innerHeight-rect.bottom-12,above=rect.top-12;
 el.style.top=(below>=height || below>=above ? Math.min(rect.bottom+6,window.innerHeight-height-12) : Math.max(12,rect.top-height-6))+'px';
}
watch(()=>props.disabled,value=>{if(value)open.value=false;});
watch(open,async value=>{await nextTick();if(!panel.value)return;if(value){panel.value.showPopover();positionPanel();}else{panel.value.hidePopover();}});
function handleEscape(event){if(open.value){event.preventDefault();event.stopPropagation();closeDropdown();}}
function closeDropdown(){open.value=false;nextTick(()=>trigger.value?.focus());}
function modalClosed(){open.value=false;}
function outsideClick(event){if(open.value && !panel.value?.contains(event.target) && !trigger.value?.contains(event.target))open.value=false;}

function closeOther(event){if(event.detail!==dropdownId)open.value=false;}
function toggleOpen(){if(props.disabled)return;if(open.value){open.value=false;return;}window.dispatchEvent(new CustomEvent('shelf-dropdown-open',{detail:dropdownId}));search.value='';open.value=true;}
onMounted(()=>{window.addEventListener('shelf-dropdown-open',closeOther);window.addEventListener('resize',positionPanel);document.addEventListener('scroll',positionPanel,true);document.addEventListener('close',modalClosed,true);document.addEventListener('pointerdown',outsideClick,true);});
onBeforeUnmount(()=>{window.removeEventListener('shelf-dropdown-open',closeOther);window.removeEventListener('resize',positionPanel);document.removeEventListener('scroll',positionPanel,true);document.removeEventListener('close',modalClosed,true);document.removeEventListener('pointerdown',outsideClick,true);});
const selected=computed(()=>Array.isArray(props.modelValue)?props.modelValue:[props.modelValue]);
const filtered=computed(()=>props.options.filter(o=>o.label.toLocaleLowerCase().includes(search.value.toLocaleLowerCase())));
const canCreate=computed(()=>props.allowCreate&&search.value.trim()&&!props.options.some(o=>o.label.toLocaleLowerCase()===search.value.trim().toLocaleLowerCase())&&!selected.value.some(v=>v.toLocaleLowerCase()===search.value.trim().toLocaleLowerCase()));
function createTag(){if(!canCreate.value||props.disabled)return;emit('update:modelValue',[...selected.value,search.value.trim()]);search.value='';}
const caption=computed(()=>props.multiple?(selected.value.length?selected.value.length+' seleccionados':'Seleccionar '+props.label.toLowerCase()):(props.options.find(o=>o.value===props.modelValue)?.label||props.placeholder||'Seleccionar'));
function choose(value){if(props.multiple){emit('update:modelValue',selected.value.includes(value)?selected.value.filter(v=>v!==value):[...selected.value,value]);}else{emit('update:modelValue',value);open.value=false;}}
</script>
<template><div class="search-dropdown" @keydown.esc="handleEscape"><button ref="trigger" type="button" class="dropdown-trigger" :disabled="disabled" :aria-label="label" :aria-expanded="open" @click="toggleOpen">{{caption}}<span>⌄</span></button><div ref="panel" popover="manual" class="dropdown-panel floating-dropdown-panel" :class="{'mochi-night product-category-menu':night}"><input v-model="search" type="search" :aria-label="'Buscar en '+label" placeholder="Buscar…" @keydown.enter.prevent="createTag"/><div class="dropdown-options"><button v-if="canCreate" type="button" @click="createTag"><span class="dropdown-check" aria-hidden="true">+</span>Crear «{{search.trim()}}»</button><button v-for="o in filtered" :key="o.value" type="button" :class="{selected:selected.includes(o.value)}" @click="choose(o.value)"><span class="dropdown-check" :class="{checked:selected.includes(o.value)}" aria-hidden="true">{{selected.includes(o.value)?'✓':''}}</span>{{o.label}}</button><p v-if="!filtered.length && !canCreate">Sin resultados</p></div><button type="button" class="dropdown-done" @click="open=false">Listo</button></div><div v-if="multiple && selected.length" class="dropdown-chips"><button v-for="value in selected" :key="value" type="button" @click="choose(value)">{{options.find(o=>o.value===value)?.label||value}} ×</button></div></div></template>