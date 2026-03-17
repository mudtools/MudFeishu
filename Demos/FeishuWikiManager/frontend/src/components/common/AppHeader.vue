<script setup lang="ts">
import { useUserStore } from '@/stores/user'
import ThemeSwitch from './ThemeSwitch.vue'

defineEmits<{
  toggle: []
  logout: []
}>()

const userStore = useUserStore()
</script>

<template>
  <el-header class="app-header">
    <div class="header-left">
      <div class="logo">
        <el-icon :size="28" class="logo-icon"><Notebook /></el-icon>
      </div>
      <div class="header-title-group">
        <span class="header-title">飞书知识库管理</span>
        <span class="header-subtitle">Feishu Wiki Manager</span>
      </div>
    </div>
    
    <div class="header-right">
      <slot name="extra" />
      
      <ThemeSwitch />
      
      <el-divider direction="vertical" />
      
      <el-dropdown trigger="click">
        <div class="user-info">
          <el-avatar :size="32" :src="userStore.user?.avatar">
            {{ userStore.user?.name?.charAt(0) }}
          </el-avatar>
          <span class="user-name">{{ userStore.user?.name }}</span>
          <el-icon><ArrowDown /></el-icon>
        </div>
        <template #dropdown>
          <el-dropdown-menu>
            <el-dropdown-item @click="$emit('logout')">
              <el-icon><SwitchButton /></el-icon>
              退出登录
            </el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>
    </div>
  </el-header>
</template>

<style scoped>
.app-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: var(--header-bg);
  border-bottom: 1px solid var(--header-border);
  padding: 0 24px;
  height: var(--header-height);
  transition: background-color var(--transition-normal), border-color var(--transition-normal);
}

.header-left {
  display: flex;
  align-items: center;
  gap: 14px;
}

.logo {
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, var(--primary-color) 0%, var(--primary-color-dark) 100%);
  border-radius: var(--border-radius);
  box-shadow: 0 2px 8px rgba(51, 112, 255, 0.3);
}

.logo-icon {
  color: white;
}

.header-title-group {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.header-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
  line-height: 1.2;
}

.header-subtitle {
  font-size: 11px;
  color: var(--text-tertiary);
  letter-spacing: 0.5px;
  text-transform: uppercase;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 12px;
}

.header-right .el-divider {
  height: 24px;
  margin: 0 4px;
  border-color: var(--border-color);
}

.user-info {
  display: flex;
  align-items: center;
  gap: 10px;
  cursor: pointer;
  padding: 6px 10px;
  border-radius: var(--border-radius);
  transition: all var(--transition-fast);
}

.user-info:hover {
  background-color: var(--bg-hover);
}

.user-name {
  color: var(--text-primary);
  font-weight: 500;
}
</style>
