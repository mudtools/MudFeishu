<template>
  <el-container class="app-layout">
    <el-aside :width="isCollapsed ? '64px' : '260px'" class="app-aside" :class="{ 'is-collapsed': isCollapsed }">
      <div class="logo">
        <div class="logo-icon">
          <el-icon :size="32">
            <DocumentChecked />
          </el-icon>
        </div>
        <div v-show="!isCollapsed" class="logo-text">
          <h1>TaskMaster</h1>
          <span>任务管理系统</span>
        </div>
      </div>

      <div class="nav-section">
        <div v-show="!isCollapsed" class="nav-title">主菜单</div>
        <el-menu :default-active="activeMenu" class="app-menu" router :collapse="isCollapsed">
          <el-menu-item index="/tasks">
            <el-icon>
              <Document />
            </el-icon>
            <template #title>
              <span>任务列表</span>
              <el-badge v-if="taskStats.pending > 0" :value="taskStats.pending" class="menu-badge" />
            </template>
          </el-menu-item>
          <el-menu-item index="/kanban">
            <el-icon>
              <Grid />
            </el-icon>
            <template #title>
              <span>任务看板</span>
            </template>
          </el-menu-item>
          <el-menu-item index="/tasklists">
            <el-icon>
              <Folder />
            </el-icon>
            <template #title>
              <span>任务清单</span>
            </template>
          </el-menu-item>
        </el-menu>
      </div>

      <div class="nav-section">
        <div v-show="!isCollapsed" class="nav-title">工具</div>
        <el-menu :default-active="activeMenu" class="app-menu" router :collapse="isCollapsed">
          <el-menu-item index="/templates">
            <el-icon>
              <DocumentCopy />
            </el-icon>
            <template #title>
              <span>任务模板</span>
            </template>
          </el-menu-item>
          <el-menu-item index="/statistics">
            <el-icon>
              <DataAnalysis />
            </el-icon>
            <template #title>
              <span>统计报表</span>
            </template>
          </el-menu-item>
        </el-menu>
      </div>

      <div class="nav-section" v-if="hasUserManagePermission">
        <div v-show="!isCollapsed" class="nav-title">系统管理</div>
        <el-menu :default-active="activeMenu" class="app-menu" router :collapse="isCollapsed">
          <el-menu-item index="/users">
            <el-icon>
              <User />
            </el-icon>
            <template #title>
              <span>用户管理</span>
            </template>
          </el-menu-item>
          <el-menu-item index="/roles">
            <el-icon>
              <Key />
            </el-icon>
            <template #title>
              <span>角色权限</span>
            </template>
          </el-menu-item>
        </el-menu>
      </div>

      <div class="sidebar-footer">
        <div class="quick-actions">
          <el-tooltip :content="isCollapsed ? '新建任务' : ''" placement="top">
            <el-button type="primary" circle @click="showCreateTaskDialog">
              <el-icon>
                <Plus />
              </el-icon>
            </el-button>
          </el-tooltip>
        </div>
      </div>
    </el-aside>

    <el-container>
      <el-header class="app-header">
        <div class="header-left">
          <el-button text @click="toggleSidebar">
            <el-icon :size="20">
              <Fold v-if="!isCollapsed" />
              <Expand v-else />
            </el-icon>
          </el-button>
          <breadcrumb-nav />
        </div>

        <div class="header-center">
          <global-search />
        </div>

        <div class="header-right">
          <el-tooltip :content="themeStore.isDark() ? '切换到亮色模式' : '切换到暗色模式'">
            <el-button text circle @click="themeStore.toggleTheme()">
              <el-icon :size="18">
                <Sunny v-if="themeStore.isDark()" />
                <Moon v-else />
              </el-icon>
            </el-button>
          </el-tooltip>

          <el-tooltip content="通知" placement="bottom">
            <el-button text circle class="notification-btn">
              <el-icon :size="18">
                <Bell />
              </el-icon>
              <el-badge v-if="notificationCount > 0" :value="notificationCount" class="notification-badge" />
            </el-button>
          </el-tooltip>

          <el-dropdown trigger="click">
            <div class="user-info">
              <el-avatar :size="36" :src="userAvatar" class="user-avatar">
                {{ userName.charAt(0).toUpperCase() }}
              </el-avatar>
              <div class="user-details">
                <span class="user-name">{{ userName }}</span>
                <span class="user-role">管理员</span>
              </div>
              <el-icon>
                <ArrowDown />
              </el-icon>
            </div>
            <template #dropdown>
              <el-dropdown-menu class="user-dropdown">
                <div class="dropdown-header">
                  <el-avatar :size="48" :src="userAvatar">
                    {{ userName.charAt(0).toUpperCase() }}
                  </el-avatar>
                  <div class="dropdown-user-info">
                    <span class="dropdown-user-name">{{ userName }}</span>
                    <span class="dropdown-user-email">{{ userEmail || '未设置邮箱' }}</span>
                  </div>
                </div>
                <el-dropdown-item divided>
                  <el-icon>
                    <User />
                  </el-icon>
                  个人设置
                </el-dropdown-item>
                <el-dropdown-item>
                  <el-icon>
                    <Setting />
                  </el-icon>
                  系统设置
                </el-dropdown-item>
                <el-dropdown-item divided @click="handleLogout">
                  <el-icon>
                    <SwitchButton />
                  </el-icon>
                  退出登录
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </el-header>

      <el-main class="app-main">
        <router-view v-slot="{ Component }">
          <transition name="fade-transform" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>
      </el-main>
    </el-container>
  </el-container>

  <!-- 新建任务对话框 -->
  <create-task-dialog v-model="createTaskVisible" />
</template>

<script setup lang="ts">
import { ref, computed } from "vue"
import { useRoute, useRouter } from "vue-router"
import {
  Document,
  Grid,
  Folder,
  DocumentCopy,
  DataAnalysis,
  Sunny,
  Moon,
  Plus,
  Bell,
  User,
  Setting,
  SwitchButton,
  Fold,
  Expand,
  ArrowDown,
  DocumentChecked,
  Key,
} from "@element-plus/icons-vue"
import { useThemeStore } from "../stores/theme"
import { useAuthStore } from "../stores/auth"
import BreadcrumbNav from "../components/BreadcrumbNav.vue"
import GlobalSearch from "../components/GlobalSearch.vue"
import CreateTaskDialog from "../components/CreateTaskDialog.vue"

const route = useRoute()
const router = useRouter()
const themeStore = useThemeStore()
const authStore = useAuthStore()

const isCollapsed = ref(false)
const createTaskVisible = ref(false)
const notificationCount = ref(3)

const taskStats = ref({
  pending: 5,
  total: 12,
})

const activeMenu = computed(() => route.path)

const userName = computed(
  () => authStore.user?.name || localStorage.getItem("username") || "用户"
)
const userEmail = computed(() => authStore.user?.email || "")
const userAvatar = computed(() => authStore.user?.avatarUrl || "")

const hasUserManagePermission = computed(() => {
  return authStore.hasPermission("user:manage")
})

const toggleSidebar = () => {
  isCollapsed.value = !isCollapsed.value
}

const showCreateTaskDialog = () => {
  createTaskVisible.value = true
}

const handleLogout = () => {
  authStore.logout()
  localStorage.removeItem("permissions")
  router.push("/login")
}
</script>

<style scoped>
.app-layout {
  height: 100vh;
  background: var(--bg-secondary);
}

.app-aside {
  background: linear-gradient(
    180deg,
    var(--bg-card) 0%,
    var(--bg-secondary) 100%
  );
  border-right: 1px solid var(--border-color);
  display: flex;
  flex-direction: column;
  transition: all 0.3s ease;
  overflow: hidden;
}

.logo {
  height: 80px;
  display: flex;
  align-items: center;
  padding: 0 24px;
  border-bottom: 1px solid var(--border-light);
  gap: 12px;
}

.app-aside.is-collapsed .logo {
  padding: 0 10px;
  justify-content: center;
}

.logo-icon {
  width: 44px;
  height: 44px;
  min-width: 44px;
  background: linear-gradient(
    135deg,
    var(--primary-color) 0%,
    var(--accent-color) 100%
  );
  border-radius: var(--radius-lg);
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  box-shadow: var(--shadow-md);
}

.logo-text {
  display: flex;
  flex-direction: column;
}

.logo-text h1 {
  font-size: 18px;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
  line-height: 1.2;
}

.logo-text span {
  font-size: 12px;
  color: var(--text-muted);
  margin-top: 2px;
}

.nav-section {
  padding: 16px 12px;
}

.nav-title {
  font-size: 11px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  color: var(--text-muted);
  padding: 0 12px 8px;
  margin-bottom: 4px;
}

.app-menu {
  border-right: none;
  background: transparent;
}

.app-menu :deep(.el-menu-item) {
  height: 44px;
  line-height: 44px;
  margin: 4px 0;
  border-radius: var(--radius-md);
  font-weight: 500;
  color: var(--text-secondary);
}

.app-menu :deep(.el-menu-item:hover) {
  background: var(--primary-bg);
  color: var(--primary-color);
}

.app-menu :deep(.el-menu-item.is-active) {
  background: linear-gradient(
    135deg,
    var(--primary-color) 0%,
    var(--primary-light) 100%
  );
  color: white;
  box-shadow: var(--shadow-md);
}

.app-menu :deep(.el-icon) {
  font-size: 18px;
}

.menu-badge :deep(.el-badge__content) {
  background: var(--accent-color);
  border: none;
}

.sidebar-footer {
  margin-top: auto;
  padding: 16px;
  border-top: 1px solid var(--border-light);
}

.app-aside.is-collapsed .sidebar-footer {
  padding: 16px 8px;
}

.quick-actions {
  display: flex;
  justify-content: center;
}

.quick-actions .el-button {
  width: 48px;
  height: 48px;
  background: linear-gradient(
    135deg,
    var(--primary-color) 0%,
    var(--accent-color) 100%
  );
  border: none;
  box-shadow: var(--shadow-lg);
}

.app-aside.is-collapsed .nav-section {
  padding: 8px 0;
}

.quick-actions .el-button:hover {
  transform: translateY(-2px);
  box-shadow: var(--shadow-xl);
}

.app-header {
  height: 70px;
  background: var(--bg-card);
  border-bottom: 1px solid var(--border-color);
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 24px;
  box-shadow: var(--shadow-sm);
}

.header-left {
  display: flex;
  align-items: center;
  gap: 16px;
}

.header-center {
  flex: 1;
  max-width: 500px;
  margin: 0 24px;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 8px;
}

.notification-btn {
  position: relative;
}

.notification-badge :deep(.el-badge__content) {
  position: absolute;
  top: 4px;
  right: 4px;
  background: var(--danger-color);
  border: none;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 6px 12px;
  border-radius: var(--radius-lg);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.user-info:hover {
  background: var(--bg-tertiary);
}

.user-avatar {
  background: linear-gradient(
    135deg,
    var(--primary-color) 0%,
    var(--accent-color) 100%
  );
  color: white;
  font-weight: 600;
}

.user-details {
  display: flex;
  flex-direction: column;
  line-height: 1.3;
}

.user-name {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
}

.user-role {
  font-size: 12px;
  color: var(--text-muted);
}

.app-main {
  background: var(--bg-secondary);
  overflow: auto;
  padding: 24px;
}

/* Dropdown styles */
.user-dropdown {
  min-width: 240px;
}

.dropdown-header {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 16px;
  background: linear-gradient(135deg, var(--primary-bg) 0%, transparent 100%);
  border-radius: var(--radius-md) var(--radius-md) 0 0;
}

.dropdown-user-info {
  display: flex;
  flex-direction: column;
}

.dropdown-user-name {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
}

.dropdown-user-email {
  font-size: 12px;
  color: var(--text-muted);
  margin-top: 2px;
}

/* Transitions */
.fade-transform-enter-active,
.fade-transform-leave-active {
  transition: all 0.3s ease;
}

.fade-transform-enter-from {
  opacity: 0;
  transform: translateX(-20px);
}

.fade-transform-leave-to {
  opacity: 0;
  transform: translateX(20px);
}
</style>
