import {reactive,toRefs,watch} from 'vue';
const key='mochi-library-filters';
export function normalizeFilters(value={}) {
 if(!value||typeof value!=='object')value={};
 const list=x=>Array.isArray(x)?[...new Set(x.filter(v=>typeof v==='string'))]:[];
 return {chosenCategories:list(value.chosenCategories),chosenTags:list(value.chosenTags),chosenAvatars:list(value.chosenAvatars),query:typeof value.query==='string'?value.query:'',onlyFavorites:value.onlyFavorites===true,includeGeneric:value.includeGeneric!==false,sortBy:['az','za','largest','smallest'].includes(value.sortBy)?value.sortBy:'az',perPage:['30','60','90','120','all'].includes(String(value.perPage))?String(value.perPage):'30',viewMode:value.viewMode==='list'?'list':'grid'};
}
export function useLibraryFilters(){let saved={};try{saved=JSON.parse(localStorage.getItem(key)||'{}')||{};}catch{}const state=reactive(normalizeFilters(saved));watch(state,()=>{try{localStorage.setItem(key,JSON.stringify(state));}catch{}},{deep:true});function clear(){state.chosenCategories=[];state.chosenTags=[];state.chosenAvatars=[];state.query='';state.onlyFavorites=false;state.includeGeneric=true;}return {state,...toRefs(state),clear};}

export function matchesLibrarySelections(item,filters,tags,avatarMatcher){return (!filters.chosenCategories.length||filters.chosenCategories.includes(item.category))&&(!filters.chosenTags.length||filters.chosenTags.some(tag=>tags.some(t=>t.toLocaleLowerCase()===tag.toLocaleLowerCase())))&&(!filters.chosenAvatars.length||(item.generic?filters.includeGeneric:filters.chosenAvatars.some(avatar=>avatarMatcher(item,avatar))));}
