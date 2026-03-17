<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useWikiStore } from '@/stores/wiki'
import { useFavoriteStore } from '@/stores/favorite'
import AppHeader from '@/components/common/AppHeader.vue'
import AppSidebar from '@/components/common/AppSidebar.vue'
import NodeTree from '@/components/wiki/NodeTree.vue'
import type { Node } from '@/types'

const route = useRoute()
const wikiStore = useWikiStore()
const favoriteStore = useFavoriteStore()
const loading = ref(false)
const createDialogVisible = ref(false)
const newDocTitle = ref('')
const newDocType = ref('docx')
const selectedParentToken = ref<string | undefined>()
const creating = ref(false)

const spaceId = ref(route.params.spaceId as string)

async function loadSpaceData() {
  if (!spaceId.value) return
  
  loading.value = true
  try {
    await Promise.all([
      wikiStore.fetchSpaceInfo(spaceId.value),
      wikiStore.fetchNodeTree(spaceId.value, undefined, true),
      favoriteStore.fetchFavorites()
    ])
  } catch (error: any) {
    ElMessage.error(error.message || '加载失败')
  } finally {
    loading.value = false
  }
}

async function loadChildNodes(parentToken: string) {
  try {
    await wikiStore.fetchNodeTree(spaceId.value, parentToken)
  } catch (error: any) {
    ElMessage.error(error.message || '加载子节点失败')
  }
}

async function handleCreateDoc() {
  if (!newDocTitle.value.trim()) {
    ElMessage.warning('请输入文档标题')
    return
  }

  try {
    creating.value = true
    const { wikiApi } = await import('@/api')
    await wikiApi.createNode(spaceId.value, {
      spaceId: spaceId.value,
      parentNodeToken: selectedParentToken.value,
      title: newDocTitle.value,
      objType: newDocType.value
    })
    ElMessage.success('创建成功')
    createDialogVisible.value = false
    newDocTitle.value = ''
    await wikiStore.fetchNodeTree(spaceId.value, selectedParentToken.value, true)
  } catch (error: any) {
    ElMessage.error(error.message || '创建失败')
  } finally {
    creating.value = false
  }
}

function handleNodeClick(node: Node) {
  console.log('Node clicked:', node)
}

function handleNodeExpand(node: Node) {
  if (node.hasChildren) {
    loadChildNodes(node.nodeToken)
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
          <el-button type="primary" @click="createDialogVisible = true">
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
            :nodes="wikiStore.nodeTree.get('root') || []"
            :favorites="favoriteStore.favorites"
            @node-click="handleNodeClick"
            @node-expand="handleNodeExpand"
            @toggle-favorite="handleToggleFavorite"
          />
        </el-card>
      </el-main>
    </el-container>
    
    <el-dialog v-model="createDialogVisible" title="新建文档" width="480px">
      <el-form label-width="80px">
        <el-form-item label="文档类型">
          <el-select v-model="newDocType" style="width: 100%">
            <el-option label="文档" value="docx" />
            <el-option label="表格" value="sheet" />
            <el-option label="多维表格" value="bitable" />
            <el-option label="幻灯片" value="slides" />
            <el-option label="思维导图" value="mindnote" />
          </el-select>
        </el-form-item>
        <el-form-item label="标题" required>
          <el-input v-model="newDocTitle" placeholder="请输入文档标题" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="createDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="creating" @click="handleCreateDoc">
          创建
        </el-button>
      </template>
    </el-dialog>
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
