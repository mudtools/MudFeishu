<script setup lang="ts">
import { useUserStore } from '@/stores/user'

defineEmits<{
  toggle: []
  logout: []
}>()

const userStore = useUserStore()
</script>

<template>
  <el-header class="app-header">
    <div class="header-left">
      <el-button text @click="$emit('toggle')">
        <el-icon :size="20"><Expand /></el-icon>
      </el-button>
      <span class="header-title">飞书知识库管理</span>
    </div>
    
    <div class="header-right">
      <slot name="extra" />
      
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
  background: white;
  border-bottom: 1px solid var(--border-color);
  padding: 0 16px;
  height: var(--header-height);
}

.header-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.header-title {
  font-size: 16px;
  font-weight: 500;
  color: var(--text-primary);
}

.header-right {
  display: flex;
  align-items: center;
  gap: 16px;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  padding: 4px 8px;
  border-radius: 4px;
  transition: background-color 0.2s;
}

.user-info:hover {
  background-color: #f0f2f5;
}

.user-name {
  color: var(--text-primary);
}
</style>
