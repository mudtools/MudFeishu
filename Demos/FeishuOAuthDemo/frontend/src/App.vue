<template>
  <el-container class="app-container">
    <el-header class="app-header">
      <div class="header-content">
        <div class="header-left">
          <h1 class="logo">飞书OAuth登录演示</h1>
          <el-menu
            :default-active="activeMenu"
            mode="horizontal"
            class="nav-menu"
            :ellipsis="false"
            background-color="transparent"
            text-color="#fff"
            active-text-color="#ffd04b"
            @select="handleMenuSelect"
          >
            <el-menu-item index="home">首页</el-menu-item>
            <el-menu-item index="departments">部门管理</el-menu-item>
          </el-menu>
        </div>
        <div class="user-info" v-if="userStore.isLoggedIn">
          <el-dropdown>
            <span class="user-name">
              <el-icon><User /></el-icon>
              {{ userStore.user?.name }}
            </span>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click="handleLogout">退出登录</el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </div>
    </el-header>
    <el-main class="app-main">
      <router-view />
    </el-main>
  </el-container>
</template>

<script setup lang="ts">
import { onMounted, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useUserStore } from './stores/user'

const router = useRouter()
const route = useRoute()
const userStore = useUserStore()

const activeMenu = computed(() => {
  if (route.path === '/departments') return 'departments'
  return 'home'
})

onMounted(async () => {
  // 初始化时验证token
  await userStore.validateToken()
})

const handleMenuSelect = (index: string) => {
  if (index === 'home') {
    router.push('/')
  } else if (index === 'departments') {
    router.push('/departments')
  }
}

const handleLogout = () => {
  userStore.logout()
  router.push('/login')
}
</script>

<style scoped>
.app-container {
  min-height: 100vh;
}

.app-header {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  display: flex;
  align-items: center;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.header-content {
  width: 100%;
  max-width: 1200px;
  margin: 0 auto;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 24px;
}

.logo {
  margin: 0;
  font-size: 24px;
  font-weight: 600;
  white-space: nowrap;
}

.nav-menu {
  border-bottom: none !important;
}

.nav-menu .el-menu-item {
  font-size: 15px;
}

.user-info {
  display: flex;
  align-items: center;
}

.user-name {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  font-size: 16px;
}

.app-main {
  max-width: 1200px;
  margin: 0 auto;
  padding: 40px 20px;
}
</style>
