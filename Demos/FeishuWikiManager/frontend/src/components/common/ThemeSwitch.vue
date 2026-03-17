<script setup lang="ts">
import { computed } from 'vue'
import { useThemeStore, type ThemeMode } from '@/stores/theme'

const themeStore = useThemeStore()

const themeOptions: { value: ThemeMode; label: string; icon: string }[] = [
  { value: 'light', label: '亮色', icon: 'Sunny' },
  { value: 'dark', label: '暗色', icon: 'Moon' },
  { value: 'auto', label: '跟随系统', icon: 'Monitor' }
]

const currentIcon = computed(() => {
  const option = themeOptions.find(o => o.value === themeStore.mode)
  return option?.icon || 'Monitor'
})

function handleCommand(command: ThemeMode) {
  themeStore.setMode(command)
}
</script>

<template>
  <el-dropdown trigger="click" @command="handleCommand">
    <el-button class="theme-switch" circle>
      <el-icon :size="18">
        <component :is="currentIcon" />
      </el-icon>
    </el-button>
    <template #dropdown>
      <el-dropdown-menu>
        <el-dropdown-item
          v-for="option in themeOptions"
          :key="option.value"
          :command="option.value"
          :class="{ 'is-active': themeStore.mode === option.value }"
        >
          <el-icon class="theme-icon"><component :is="option.icon" /></el-icon>
          <span>{{ option.label }}</span>
          <el-icon v-if="themeStore.mode === option.value" class="check-icon"><Check /></el-icon>
        </el-dropdown-item>
      </el-dropdown-menu>
    </template>
  </el-dropdown>
</template>

<style scoped>
.theme-switch {
  border: none;
  background: transparent;
  color: var(--text-primary);
  transition: all var(--transition-fast);
}

.theme-switch:hover {
  background: var(--bg-hover);
  color: var(--primary-color);
}

.theme-icon {
  margin-right: 8px;
}

.check-icon {
  margin-left: auto;
  color: var(--primary-color);
}

:deep(.el-dropdown-menu__item) {
  display: flex;
  align-items: center;
  min-width: 140px;
}

:deep(.el-dropdown-menu__item.is-active) {
  background: var(--primary-bg);
  color: var(--primary-color);
}
</style>
