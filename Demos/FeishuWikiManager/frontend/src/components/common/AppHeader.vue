<script setup lang="ts">
import { ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { useUserStore } from '@/stores/user'
import ThemeSwitch from './ThemeSwitch.vue'

defineEmits<{
  toggle: []
  logout: []
}>()

const userStore = useUserStore()
const userInfoDialogVisible = ref(false)

// 监听对话框显示，加载详细用户信息
watch(userInfoDialogVisible, (visible) => {
  if (visible && !userStore.userDetail) {
    userStore.fetchUserDetail()
  }
})

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
        width="720px"
        :close-on-click-modal="true"
        class="user-info-dialog"
        append-to-body
      >
        <div v-if="userStore.detailLoading" class="user-info-loading">
          <el-skeleton :rows="4" animated />
        </div>
        <div v-else class="user-info-content">
          <!-- 顶部头像和状态 -->
          <div class="user-header-section">
            <div class="user-avatar-section">
              <el-avatar :size="64" :src="userStore.userDetail?.avatarBig || userStore.userDetail?.avatar || userStore.user?.avatar">
                {{ (userStore.userDetail?.name || userStore.user?.name)?.charAt(0) }}
              </el-avatar>
              <span class="user-status-dot"></span>
            </div>
            <div class="user-header-info">
              <div class="user-name">{{ userStore.userDetail?.name || userStore.user?.name || '-' }}</div>
              <div class="user-status-text">
                <span class="status-dot"></span>
                在线
              </div>
            </div>
          </div>
          
          <!-- 两栏信息展示 -->
          <div class="user-details-two-column">
            <!-- 左栏 -->
            <div class="detail-column">
              <div class="detail-section">
                <div class="detail-section-title">基本信息</div>
                <div class="detail-item">
                  <span class="detail-label">姓名</span>
                  <span class="detail-value">{{ userStore.userDetail?.name || userStore.user?.name || '-' }}</span>
                </div>
                <div v-if="userStore.userDetail?.enName" class="detail-item">
                  <span class="detail-label">英文名</span>
                  <span class="detail-value">{{ userStore.userDetail.enName }}</span>
                </div>
                <div v-if="userStore.userDetail?.nickname" class="detail-item">
                  <span class="detail-label">别名</span>
                  <span class="detail-value">{{ userStore.userDetail.nickname }}</span>
                </div>
                <div v-if="userStore.userDetail?.employeeNo" class="detail-item">
                  <span class="detail-label">工号</span>
                  <span class="detail-value">{{ userStore.userDetail.employeeNo }}</span>
                </div>
              </div>
              
              <div class="detail-section">
                <div class="detail-section-title">联系信息</div>
                <div class="detail-item">
                  <span class="detail-label">邮箱</span>
                  <span class="detail-value">{{ userStore.userDetail?.email || userStore.user?.email || '-' }}</span>
                </div>
                <div v-if="userStore.userDetail?.enterpriseEmail" class="detail-item">
                  <span class="detail-label">企业邮箱</span>
                  <span class="detail-value">{{ userStore.userDetail.enterpriseEmail }}</span>
                </div>
                <div v-if="userStore.userDetail?.mobile" class="detail-item">
                  <span class="detail-label">手机号</span>
                  <span class="detail-value">{{ userStore.userDetail.mobile }}</span>
                </div>
              </div>
            </div>
            
            <!-- 右栏 -->
            <div class="detail-column">
              <div class="detail-section">
                <div class="detail-section-title">系统标识</div>
                <div class="detail-item">
                  <span class="detail-label">User ID</span>
                  <span class="detail-value copyable" :title="userStore.userDetail?.userId" @click="copyToClipboard(userStore.userDetail?.userId)">
                    {{ formatId(userStore.userDetail?.userId) }}
                    <el-icon class="copy-icon"><CopyDocument /></el-icon>
                  </span>
                </div>
                <div class="detail-item">
                  <span class="detail-label">OpenID</span>
                  <span class="detail-value copyable" :title="userStore.userDetail?.openId || userStore.user?.openId" @click="copyToClipboard(userStore.userDetail?.openId || userStore.user?.openId)">
                    {{ formatId(userStore.userDetail?.openId || userStore.user?.openId) }}
                    <el-icon class="copy-icon"><CopyDocument /></el-icon>
                  </span>
                </div>
                <div class="detail-item">
                  <span class="detail-label">UnionID</span>
                  <span class="detail-value copyable" :title="userStore.userDetail?.unionId || userStore.user?.unionId" @click="copyToClipboard(userStore.userDetail?.unionId || userStore.user?.unionId)">
                    {{ formatId(userStore.userDetail?.unionId || userStore.user?.unionId) }}
                    <el-icon class="copy-icon"><CopyDocument /></el-icon>
                  </span>
                </div>
                <div v-if="userStore.userDetail?.tenantKey" class="detail-item">
                  <span class="detail-label">Tenant Key</span>
                  <span class="detail-value copyable" :title="userStore.userDetail.tenantKey" @click="copyToClipboard(userStore.userDetail.tenantKey)">
                    {{ formatId(userStore.userDetail.tenantKey) }}
                    <el-icon class="copy-icon"><CopyDocument /></el-icon>
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>
        
        <template #footer>
          <el-button @click="userInfoDialogVisible = false">关闭</el-button>
          <el-button type="primary" :loading="userStore.detailLoading" @click="userStore.fetchUserDetail()">
            <el-icon><Refresh /></el-icon>
            刷新
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
  padding: 8px 0;
}

/* 顶部头像区域 */
.user-header-section {
  display: flex;
  align-items: center;
  gap: 16px;
  margin-bottom: 20px;
  padding-bottom: 16px;
  border-bottom: 1px solid var(--border-color);
}

.user-avatar-section {
  position: relative;
  flex-shrink: 0;
}

.user-avatar-section .el-avatar {
  border: 3px solid var(--primary-bg);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.user-status-dot {
  position: absolute;
  bottom: 2px;
  right: 2px;
  width: 16px;
  height: 16px;
  background: #22c55e;
  border: 2px solid var(--card-bg);
  border-radius: 50%;
}

.user-header-info {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.user-name {
  font-size: 18px;
  font-weight: 600;
  color: var(--text-primary);
}

.user-status-text {
  font-size: 13px;
  color: #22c55e;
  font-weight: 500;
  display: flex;
  align-items: center;
  gap: 6px;
}

.status-dot {
  width: 8px;
  height: 8px;
  background: #22c55e;
  border-radius: 50%;
}

/* 两栏布局 */
.user-details-two-column {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 24px;
}

.detail-column {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.detail-section {
  background: var(--bg-tertiary);
  border-radius: var(--border-radius-lg);
  padding: 12px 16px;
}

.detail-section-title {
  font-size: 12px;
  font-weight: 600;
  color: var(--text-tertiary);
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-bottom: 12px;
}

.detail-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 0;
  border-bottom: 1px solid var(--border-color);
}

.detail-item:last-child {
  border-bottom: none;
}

.detail-label {
  font-size: 13px;
  color: var(--text-secondary);
  font-weight: 500;
}

.detail-value {
  font-size: 13px;
  color: var(--text-primary);
  font-weight: 500;
  max-width: 60%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  text-align: right;
}

.detail-value.copyable {
  display: flex;
  align-items: center;
  gap: 6px;
  cursor: pointer;
  padding: 2px 6px;
  border-radius: var(--border-radius);
  transition: all var(--transition-fast);
}

.detail-value.copyable:hover {
  background: var(--bg-hover);
  color: var(--primary-color);
}

.copy-icon {
  font-size: 12px;
  opacity: 0.6;
  flex-shrink: 0;
}

.detail-value.copyable:hover .copy-icon {
  opacity: 1;
}

/* 加载状态 */
.user-info-loading {
  padding: 20px;
}
</style>
