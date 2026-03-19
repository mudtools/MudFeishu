<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessageBox } from 'element-plus'
import { useUserStore } from '@/stores/user'
import { useFavoriteStore } from '@/stores/favorite'

defineProps<{
  collapsed?: boolean
}>()

const emit = defineEmits<{
  logout: []
}>()

const router = useRouter()
const userStore = useUserStore()
const favoriteStore = useFavoriteStore()
const activeMenu = ref(router.currentRoute.value.path)

function handleMenuSelect(index: string) {
  router.push(index)
}

async function handleLogout() {
  try {
    await ElMessageBox.confirm('确定要退出登录吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    emit('logout')
  } catch (error: any) {
    // 如果是401错误，说明token已过期，直接清理状态
    if (error?.response?.status === 401) {
      emit('logout')
    }
    // 用户取消或其他错误不处理
  }
}
</script>

<template>
  <el-aside :width="collapsed ? '64px' : 'var(--sidebar-width)'" class="app-sidebar">
    <el-menu
      :default-active="activeMenu"
      :collapse="collapsed"
      @select="handleMenuSelect"
    >
      <el-menu-item index="/">
        <el-icon><HomeFilled /></el-icon>
        <template #title>首页</template>
      </el-menu-item>
      
      <el-menu-item index="/spaces">
        <el-icon><Folder /></el-icon>
        <template #title>知识空间</template>
      </el-menu-item>
      
      <el-menu-item index="/search">
        <el-icon><Search /></el-icon>
        <template #title>搜索</template>
      </el-menu-item>
    </el-menu>
    
    <div v-if="!collapsed && favoriteStore.favorites.length > 0" class="favorites-section">
      <div class="section-title">收藏夹</div>
      <div 
        v-for="fav in favoriteStore.favorites.slice(0, 5)" 
        :key="fav.id" 
        class="favorite-item"
        @click="router.push(`/spaces/${fav.spaceId}`)"
      >
        <el-icon :size="14"><Star /></el-icon>
        <span class="favorite-title">{{ fav.title }}</span>
      </div>
    </div>
    
    <div class="sidebar-footer">
      <div class="user-card" v-if="!collapsed">
        <el-avatar :size="32" :src="userStore.user?.avatar">
          {{ userStore.user?.name?.charAt(0) }}
        </el-avatar>
        <div class="user-info">
          <div class="user-name">{{ userStore.user?.name }}</div>
          <div class="user-email">{{ userStore.user?.email || '未设置邮箱' }}</div>
        </div>
        <el-tooltip content="退出登录" placement="top">
          <el-button
            type="danger"
            link
            size="small"
            @click="handleLogout"
            class="logout-btn"
          >
            <el-icon><SwitchButton /></el-icon>
          </el-button>
        </el-tooltip>
      </div>
      <!-- 折叠状态下的退出按钮 -->
      <div v-else class="collapsed-logout">
        <el-tooltip content="退出登录" placement="right">
          <el-button
            type="danger"
            link
            @click="handleLogout"
            class="collapsed-logout-btn"
          >
            <el-icon><SwitchButton /></el-icon>
          </el-button>
        </el-tooltip>
      </div>
    </div>
  </el-aside>
</template>

<style scoped>
.app-sidebar {
  background: var(--sidebar-bg);
  border-right: 1px solid var(--sidebar-border);
  display: flex;
  flex-direction: column;
  transition: width var(--transition-normal), background-color var(--transition-normal), border-color var(--transition-normal);
}

.el-menu {
  border-right: none;
  flex: 1;
  padding: 12px 0;
}

.el-menu-item {
  margin: 4px 12px;
  border-radius: var(--border-radius);
  height: 44px;
  line-height: 44px;
}

.favorites-section {
  padding: 16px;
  border-top: 1px solid var(--border-color);
}

.section-title {
  font-size: 12px;
  font-weight: 600;
  color: var(--text-tertiary);
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-bottom: 12px;
}

.favorite-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 12px;
  border-radius: var(--border-radius);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.favorite-item:hover {
  background-color: var(--bg-hover);
}

.favorite-title {
  font-size: 13px;
  color: var(--text-secondary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.sidebar-footer {
  padding: 16px;
  border-top: 1px solid var(--border-color);
}

.user-card {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 8px;
  border-radius: var(--border-radius);
  transition: background-color var(--transition-fast);
}

.user-card:hover {
  background-color: var(--bg-hover);
}

.user-info {
  flex: 1;
  min-width: 0;
}

.user-name {
  font-size: 14px;
  font-weight: 500;
  color: var(--text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.user-email {
  font-size: 12px;
  color: var(--text-tertiary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  margin-top: 2px;
}

.logout-btn {
  margin-left: 8px;
  padding: 4px 8px;
}

.logout-btn:hover {
  background-color: var(--danger-color-light, #fef0f0);
}

/* 折叠状态下的退出按钮 */
.collapsed-logout {
  display: flex;
  justify-content: center;
  padding: 8px 0;
}

.collapsed-logout-btn {
  padding: 8px;
}

.collapsed-logout-btn:hover {
  background-color: var(--danger-color-light, #fef0f0);
  border-radius: var(--border-radius);
}
</style>
