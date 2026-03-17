<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useWikiStore } from '@/stores/wiki'
import { useFavoriteStore } from '@/stores/favorite'
import { useNodeTree } from '@/composables/useNodeTree'
import AppHeader from '@/components/common/AppHeader.vue'
import AppSidebar from '@/components/common/AppSidebar.vue'
import NodeTree from '@/components/wiki/NodeTree.vue'
import CreateDialog from '@/components/wiki/CreateDialog.vue'
import type { Node } from '@/types'

const route = useRoute()
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

function handleNodeClick(node: Node) {
  console.log('Node clicked:', node)
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
  <el-container class="main-layout">
    <AppSidebar />
    
    <el-container>
      <AppHeader>
        <template #extra>
          <el-button type="primary" @click="handleCreateDoc()">
            <el-icon><Plus /></el-icon>
            新建文档
          </el-button>
        </template>
      </AppHeader>
      
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
            :favorites="favoriteStore.favorites"
            @node-click="handleNodeClick"
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
  background-color: #f5f7fa;
  padding: 24px;
}

.page-header {
  margin-bottom: 24px;
}

.page-header h1 {
  font-size: 24px;
  color: #303133;
  margin: 0 0 8px 0;
}

.page-header p {
  color: #909399;
  margin: 0;
}
</style>
