<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { useFavoriteStore } from '@/stores/favorite'

defineProps<{
  collapsed?: boolean
}>()

defineEmits<{
  logout: []
}>()

const router = useRouter()
const userStore = useUserStore()
const favoriteStore = useFavoriteStore()
const activeMenu = ref(router.currentRoute.value.path)

function handleMenuSelect(index: string) {
  router.push(index)
}
</script>

<template>
  <el-aside :width="collapsed ? '64px' : 'var(--sidebar-width)'" class="app-sidebar">
    <div class="sidebar-header">
      <el-icon :size="24" color="#3370ff"><Notebook /></el-icon>
      <span v-if="!collapsed" class="sidebar-title">知识库</span>
    </div>
    
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
      </div>
    </div>
  </el-aside>
</template>

<style scoped>
.app-sidebar {
  background: white;
  border-right: 1px solid var(--border-color);
  display: flex;
  flex-direction: column;
  transition: width 0.3s;
}

.sidebar-header {
  height: var(--header-height);
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  border-bottom: 1px solid var(--border-color);
}

.sidebar-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
}

.el-menu {
  border-right: none;
  flex: 1;
}

.favorites-section {
  padding: 12px;
  border-top: 1px solid var(--border-color);
}

.section-title {
  font-size: 12px;
  color: var(--text-secondary);
  margin-bottom: 8px;
}

.favorite-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px;
  border-radius: 4px;
  cursor: pointer;
  transition: background-color 0.2s;
}

.favorite-item:hover {
  background-color: #f0f2f5;
}

.favorite-title {
  font-size: 13px;
  color: var(--text-regular);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.sidebar-footer {
  padding: 12px;
  border-top: 1px solid var(--border-color);
}

.user-card {
  display: flex;
  align-items: center;
  gap: 12px;
}

.user-info {
  flex: 1;
  min-width: 0;
}

.user-name {
  font-size: 14px;
  color: var(--text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.user-email {
  font-size: 12px;
  color: var(--text-secondary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
