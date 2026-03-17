<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useUserStore } from '@/stores/user'
import { useWikiStore } from '@/stores/wiki'
import { useFavoriteStore } from '@/stores/favorite'
import { useNodeTree } from '@/composables/useNodeTree'
import { authApi } from '@/api'
import AppHeader from '@/components/common/AppHeader.vue'
import AppSidebar from '@/components/common/AppSidebar.vue'
import NodeTree from '@/components/wiki/NodeTree.vue'
import CreateDialog from '@/components/wiki/CreateDialog.vue'
import type { Node } from '@/types'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()
const wikiStore = useWikiStore()
const favoriteStore = useFavoriteStore()
const spaceId = ref(route.params.spaceId as string)

const { 
  rootNodes, 
  loading, 
  fetchRootNodes, 
  fetchChildNodes 
} = useNodeTree(spaceId.value)

const createDialogVisible = ref(false)
const selectedParentToken = ref<string | undefined>()

async function loadSpaceData() {
  if (!spaceId.value) return
  
  try {
    await Promise.all([
      wikiStore.fetchSpaceInfo(spaceId.value),
      fetchRootNodes(),
      favoriteStore.fetchFavorites()
    ])
  } catch (error: any) {
    ElMessage.error(error.message || '加载失败')
  }
}

function handleNodeExpand(node: Node) {
  if (node.hasChildren) {
    fetchChildNodes(node.nodeToken)
  }
}

async function handleToggleFavorite(node: Node) {
  try {
    if (favoriteStore.isFavorite(node.nodeToken)) {
      await favoriteStore.removeFavorite(node.nodeToken)
      ElMessage.success('已取消收藏')
    } else {
      await favoriteStore.addFavorite(
        spaceId.value,
        node.nodeToken,
        node.title,
        node.objToken,
        node.objType
      )
      ElMessage.success('已添加收藏')
    }
  } catch (error: any) {
    ElMessage.error(error.message || '操作失败')
  }
}

function handleCreateDoc(parentToken?: string) {
  selectedParentToken.value = parentToken
  createDialogVisible.value = true
}

async function handleCreated() {
  await fetchRootNodes()
}

async function handleLogout() {
  try {
    await ElMessageBox.confirm('确定要退出登录吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    
    await authApi.logout()
    userStore.logout()
    ElMessage.success('已退出登录')
    router.push('/login')
  } catch (error) {
    // 用户取消
  }
}

watch(() => route.params.spaceId, (newId) => {
  if (newId) {
    spaceId.value = newId as string
    loadSpaceData()
  }
})

onMounted(() => {
  loadSpaceData()
})
</script>

<template>
  <el-container class="main-layout" direction="vertical">
    <AppHeader @logout="handleLogout">
      <template #extra>
        <el-button type="primary" @click="handleCreateDoc()">
          <el-icon><Plus /></el-icon>
          新建文档
        </el-button>
      </template>
    </AppHeader>
    
    <el-container>
      <AppSidebar @logout="handleLogout" />
      
      <el-main class="main-content">
        <div class="page-header">
          <el-skeleton v-if="loading" :rows="1" animated />
          <template v-else>
            <h1>{{ wikiStore.currentSpace?.name || '知识空间' }}</h1>
            <p>{{ wikiStore.currentSpace?.description || '暂无描述' }}</p>
          </template>
        </div>
        
        <el-card v-loading="loading">
          <NodeTree
            :nodes="rootNodes"
            :space-id="spaceId"
            :favorites="favoriteStore.favorites"
            @node-expand="handleNodeExpand"
            @toggle-favorite="handleToggleFavorite"
          />
        </el-card>
      </el-main>
    </el-container>
    
    <CreateDialog
      v-model:visible="createDialogVisible"
      :space-id="spaceId"
      :parent-token="selectedParentToken"
      @created="handleCreated"
    />
  </el-container>
</template>

<style scoped>
.main-layout {
  height: 100vh;
}

.main-content {
  background-color: var(--bg-color);
  padding: 32px;
  transition: background-color var(--transition-normal);
}

.page-header {
  margin-bottom: 32px;
  animation: slideUp var(--transition-normal);
}

.page-header h1 {
  font-size: 28px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0 0 8px 0;
}

.page-header p {
  color: var(--text-secondary);
  margin: 0;
  font-size: 15px;
}

.el-card {
  animation: slideUp var(--transition-slow);
}
</style>
