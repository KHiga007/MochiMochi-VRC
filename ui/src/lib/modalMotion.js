const pending=new WeakMap();
export function closeModalSurface(element,finish=()=>{}) {
 if(!element){finish();return Promise.resolve();}
 if(pending.has(element))return pending.get(element);
 const reduced=window.matchMedia('(prefers-reduced-motion: reduce)').matches;
 element.classList.add('modal-is-closing');
 const task=new Promise(resolve=>setTimeout(resolve,reduced?0:180)).then(()=>{finish();element.classList.remove('modal-is-closing');pending.delete(element);});pending.set(element,task);return task;
}
