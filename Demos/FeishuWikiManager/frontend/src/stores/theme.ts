import { defineStore } from 'pinia'
import { ref, watch } from 'vue'

export type ThemeMode = 'light' | 'dark' | 'auto'

export const useThemeStore = defineStore('theme', () => {
  const mode = ref<ThemeMode>(getInitialTheme())
  
  function getInitialTheme(): ThemeMode {
    const stored = localStorage.getItem('theme-mode') as ThemeMode
    if (stored && ['light', 'dark', 'auto'].includes(stored)) {
      return stored
    }
    return 'auto'
  }
  
  function getSystemTheme(): 'light' | 'dark' {
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
  }
  
  function getEffectiveTheme(): 'light' | 'dark' {
    if (mode.value === 'auto') {
      return getSystemTheme()
    }
    return mode.value
  }
  
  function applyTheme(theme: 'light' | 'dark') {
    const html = document.documentElement
    html.classList.remove('light', 'dark')
    html.classList.add(theme)
    
    // 更新 Element Plus 主题
    if (theme === 'dark') {
      document.body.setAttribute('data-theme', 'dark')
    } else {
      document.body.removeAttribute('data-theme')
    }
  }
  
  function setMode(newMode: ThemeMode) {
    mode.value = newMode
    localStorage.setItem('theme-mode', newMode)
    applyTheme(getEffectiveTheme())
  }
  
  function toggleTheme() {
    const current = getEffectiveTheme()
    setMode(current === 'light' ? 'dark' : 'light')
  }
  
  // 监听系统主题变化
  function setupSystemThemeListener() {
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)')
    const handler = () => {
      if (mode.value === 'auto') {
        applyTheme(getSystemTheme())
      }
    }
    mediaQuery.addEventListener('change', handler)
    return () => mediaQuery.removeEventListener('change', handler)
  }
  
  // 初始化主题
  function initTheme() {
    applyTheme(getEffectiveTheme())
    setupSystemThemeListener()
  }
  
  watch(mode, () => {
    applyTheme(getEffectiveTheme())
  })
  
  return {
    mode,
    setMode,
    toggleTheme,
    initTheme,
    getEffectiveTheme
  }
})
