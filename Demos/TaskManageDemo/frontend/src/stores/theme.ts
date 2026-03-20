import { defineStore } from 'pinia'
import { ref, watch } from 'vue'

export type ThemeMode = 'light' | 'dark'

export const useThemeStore = defineStore('theme', () => {
  const theme = ref<ThemeMode>((localStorage.getItem('theme') as ThemeMode) || 'light')

  const isDark = () => theme.value === 'dark'

  const setTheme = (mode: ThemeMode) => {
    theme.value = mode
    localStorage.setItem('theme', mode)
    applyTheme(mode)
  }

  const toggleTheme = () => {
    setTheme(isDark() ? 'light' : 'dark')
  }

  const applyTheme = (mode: ThemeMode) => {
    const html = document.documentElement
    if (mode === 'dark') {
      html.classList.add('dark')
    } else {
      html.classList.remove('dark')
    }
  }

  watch(theme, (newTheme) => {
    applyTheme(newTheme)
  }, { immediate: true })

  return {
    theme,
    isDark,
    setTheme,
    toggleTheme
  }
})
