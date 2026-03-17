<script setup lang="ts">
import { ref } from 'vue'
import { ElMessage } from 'element-plus'
import { useUserStore } from '@/stores/user'
import ThemeSwitch from './ThemeSwitch.vue'

defineEmits<{
  toggle: []
  logout: []
}>()

const userStore = useUserStore()
const userInfoDialogVisible = ref(false)

function showUserInfo() {
  userInfoDialogVisible.value = true
}

function formatId(id: string | undefined): string {
  if (!id) return '-'
  if (id.length <= 16) return id
  return id.slice(0, 8) + '...' + id.slice(-8)
}

async function copyToClipboard(text: string | undefined) {
  if (!text) return
  try {
    await navigator.clipboard.writeText(text)
    ElMessage.success('已复制到剪贴板')
  } catch {
    ElMessage.error('复制失败')
  }
}
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
            <el-dropdown-item @click="showUserInfo">
              <el-icon><User /></el-icon>
              用户信息
            </el-dropdown-item>
            <el-dropdown-item divided @click="$emit('logout')">
              <el-icon><SwitchButton /></el-icon>
              退出登录
            </el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>
      
      <!-- 用户信息对话框 -->
      <el-dialog
        v-model="userInfoDialogVisible"
        title="用户信息"
        width="420px"
        :close-on-click-modal="true"
        class="user-info-dialog"
        append-to-body
      >
        <div class="user-info-content">
          <div class="user-avatar-section">
            <el-avatar :size="80" :src="userStore.user?.avatar">
              {{ userStore.user?.name?.charAt(0) }}
            </el-avatar>
            <span class="user-status-dot"></span>
          </div>
          <div class="user-status-text">在线</div>
          
          <div class="user-details">
            <div class="detail-item">
              <span class="detail-label">姓名</span>
              <span class="detail-value">{{ userStore.user?.name || '-' }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">邮箱</span>
              <span class="detail-value">{{ userStore.user?.email || '-' }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">OpenID</span>
              <span class="detail-value copyable" :title="userStore.user?.openId" @click="copyToClipboard(userStore.user?.openId)">
                {{ formatId(userStore.user?.openId) }}
                <el-icon class="copy-icon"><CopyDocument /></el-icon>
              </span>
            </div>
            <div class="detail-item">
              <span class="detail-label">UnionID</span>
              <span class="detail-value copyable" :title="userStore.user?.unionId" @click="copyToClipboard(userStore.user?.unionId)">
                {{ formatId(userStore.user?.unionId) }}
                <el-icon class="copy-icon"><CopyDocument /></el-icon>
              </span>
            </div>
          </div>
        </div>
        
        <template #footer>
          <el-button type="primary" @click="userInfoDialogVisible = false">
            确定
          </el-button>
        </template>
      </el-dialog>
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

/* 用户信息对话框样式 */
.user-info-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 16px 0;
}

.user-avatar-section {
  position: relative;
  margin-bottom: 8px;
}

.user-avatar-section .el-avatar {
  border: 3px solid var(--primary-bg);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.user-status-dot {
  position: absolute;
  bottom: 4px;
  right: 4px;
  width: 18px;
  height: 18px;
  background: #22c55e;
  border: 3px solid var(--card-bg);
  border-radius: 50%;
}

.user-status-text {
  font-size: 13px;
  color: #22c55e;
  font-weight: 500;
  margin-bottom: 16px;
  display: flex;
  align-items: center;
  gap: 6px;
}

.user-status-text::before {
  content: '';
  width: 6px;
  height: 6px;
  background: #22c55e;
  border-radius: 50%;
}

.user-details {
  width: 100%;
}

.detail-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 0;
  border-bottom: 1px solid var(--border-color);
}

.detail-item:last-child {
  border-bottom: none;
}

.detail-label {
  font-size: 14px;
  color: var(--text-secondary);
  font-weight: 500;
}

.detail-value {
  font-size: 14px;
  color: var(--text-primary);
  font-weight: 500;
}

.detail-value.copyable {
  display: flex;
  align-items: center;
  gap: 6px;
  cursor: pointer;
  padding: 4px 8px;
  border-radius: var(--border-radius);
  transition: all var(--transition-fast);
}

.detail-value.copyable:hover {
  background: var(--bg-hover);
  color: var(--primary-color);
}

.copy-icon {
  font-size: 14px;
  opacity: 0.6;
}

.detail-value.copyable:hover .copy-icon {
  opacity: 1;
}
</style>
