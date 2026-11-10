// Optional identity and URLs; version comes from the desktop settings API.
export const appVersion = settings => settings.version || null;
export const author = null;
export const links = Object.freeze({changelog:null,issues:null});
export const licenses = [
  {
    "name": "Vue",
    "version": "3.5.44",
    "type": "MIT",
    "file": "/licenses/Vue.txt"
  },
  {
    "name": "Microsoft Edge WebView2 SDK",
    "version": "1.0.4314.35",
    "type": "BSD-3-Clause",
    "file": "/licenses/WebView2.txt"
  },
  {
    "name": "Plus Jakarta Sans",
    "version": "Fuente variable incluida",
    "type": "SIL OFL 1.1",
    "file": "/licenses/PlusJakartaSans.txt"
  },
  {
    "name": "JetBrains Mono",
    "version": "Fuente variable incluida",
    "type": "SIL OFL 1.1",
    "file": "/licenses/JetBrainsMono.txt"
  },
  {
    "name": "Íconos SVG de MochiMochi VRC",
    "version": "Propios",
    "type": "Sin librería externa",
    "file": null
  }
];
