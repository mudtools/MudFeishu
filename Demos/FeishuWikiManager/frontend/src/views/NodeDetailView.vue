<script setup lang="ts">
import { ref, onMounted, watch, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useUserStore } from '@/stores/user'
import { useFavoriteStore } from '@/stores/favorite'
import { wikiApi } from '@/api'
import AppHeader from '@/components/common/AppHeader.vue'
import AppSidebar from '@/components/common/AppSidebar.vue'
import type { Node } from '@/types'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()
const favoriteStore = useFavoriteStore()

const spaceId = ref(route.params.spaceId as string)
const nodeToken = ref(route.params.nodeToken as string)
const nodeInfo = ref<Node | null>(null)
const loading = ref(false)
const editDialogVisible = ref(false)
const editTitle = ref('')
const saving = ref(false)

const isFavorite = computed(() => favoriteStore.isFavorite(nodeToken.value))

async function loadNodeInfo() {
  if (!nodeToken.value) return
  
  loading.value = true
  try {
    const response = await wikiApi.getNodeInfo(nodeToken.value)
    if (response.data.success && response.data.data) {
      nodeInfo.value = response.data.data
    }
  } catch (error: any) {
    ElMessage.error(error.message || '获取节点信息失败')
  } finally {
    loading.value = false
  }
}

async function handleToggleFavorite() {
  if (!nodeInfo.value) return
  
  try {
    if (isFavorite.value) {
      await favoriteStore.removeFavorite(nodeToken.value)
      ElMessage.success('已取消收藏')
    } else {
      await favoriteStore.addFavorite(
        spaceId.value,
        nodeToken.value,
        nodeInfo.value.title,
        nodeInfo.value.objToken,
        nodeInfo.value.objType
      )
      ElMessage.success('已添加收藏')
    }
  } catch (error: any) {
    ElMessage.error(error.message || '操作失败')
  }
}

function handleEdit() {
  if (nodeInfo.value) {
    editTitle.value = nodeInfo.value.title
    editDialogVisible.value = true
  }
}

async function handleSaveEdit() {
  if (!editTitle.value.trim()) {
    ElMessage.warning('请输入标题')
    return
  }

  saving.value = true
  try {
    await wikiApi.updateNodeTitle(spaceId.value, nodeToken.value, editTitle.value)
    if (nodeInfo.value) {
      nodeInfo.value.title = editTitle.value
    }
    ElMessage.success('更新成功')
    editDialogVisible.value = false
  } catch (error: any) {
    ElMessage.error(error.message || '更新失败')
  } finally {
    saving.value = false
  }
}

async function handleDelete() {
  if (!nodeInfo.value) return
  
  try {
    await ElMessageBox.confirm(
      `确定要删除文档「${nodeInfo.value.title}」吗？此操作不可恢复。`,
      '删除确认',
      {
        confirmButtonText: '删除',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )
    
    ElMessage.info('删除功能需要飞书 API 支持')
  } catch {
    // 用户取消
  }
}

function openInFeishu() {
  if (nodeInfo.value?.objToken) {
    const url = `https://feishu.cn/docx/${nodeInfo.value.objToken}`
    window.open(url, '_blank')
  }
}

function goBack() {
  router.push(`/spaces/${spaceId.value}`)
}

async function handleLogout() {
  try {
    await ElMessageBox.confirm('确定要退出登录吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    
    await userStore.logout()
    ElMessage.success('已退出登录')
    router.push('/login')
  } catch (error: any) {
    // 如果是401错误，说明token已过期，直接清理状态
    if (error?.response?.status === 401) {
      await userStore.logout(true)
      router.push('/login')
      return
    }
    // 用户取消
  }
}

watch(() => route.params.nodeToken, (newToken) => {
  if (newToken) {
    nodeToken.value = newToken as string
    loadNodeInfo()
  }
})

onMounted(() => {
  loadNodeInfo()
  favoriteStore.fetchFavorites()
})
</script>

<template>
  <el-container class="main-layout" direction="vertical">
    <AppHeader @logout="handleLogout">
      <template #extra>
        <el-button-group>
          <el-button :type="isFavorite ? 'warning' : 'default'" @click="handleToggleFavorite">
            <el-icon><Star /></el-icon>
            {{ isFavorite ? '已收藏' : '收藏' }}
          </el-button>
          <el-button @click="handleEdit">
            <el-icon><Edit /></el-icon>
            编辑
          </el-button>
          <el-button type="primary" @click="openInFeishu">
            <el-icon><Link /></el-icon>
            在飞书中打开
          </el-button>
        </el-button-group>
      </template>
    </AppHeader>
    
    <el-container>
      <AppSidebar @logout="handleLogout" />
      
      <el-main class="main-content">
        <div class="page-header">
          <el-button link @click="goBack">
            <el-icon><ArrowLeft /></el-icon>
            返回列表
          </el-button>
        </div>
        
        <el-card v-loading="loading">
          <template v-if="nodeInfo">
            <div class="node-header">
              <div class="node-icon">
                <el-icon :size="48" color="#3370ff">
                  <Document v-if="nodeInfo.objType === 'docx'" />
                  <Grid v-else-if="nodeInfo.objType === 'sheet'" />
                  <Table v-else-if="nodeInfo.objType === 'bitable'" />
                  <PictureFilled v-else-if="nodeInfo.objType === 'slides'" />
                  <Share v-else-if="nodeInfo.objType === 'mindnote'" />
                  <Document v-else />
                </el-icon>
              </div>
              <div class="node-info">
                <h1>{{ nodeInfo.title }}</h1>
                <div class="node-meta">
                  <el-tag size="small">{{ nodeInfo.objType }}</el-tag>
                  <span v-if="nodeInfo.createTime" class="meta-item">
                    创建于: {{ new Date(nodeInfo.createTime).toLocaleString() }}
                  </span>
                  <span v-if="nodeInfo.editTime" class="meta-item">
                    更新于: {{ new Date(nodeInfo.editTime).toLocaleString() }}
                  </span>
                </div>
              </div>
            </div>
            
            <el-divider />
            
            <div class="node-content">
              <el-empty description="文档内容需要通过飞书 API 获取">
                <el-button type="primary" @click="openInFeishu">
                  在飞书中查看完整内容
                </el-button>
              </el-empty>
            </div>
            
            <el-divider />
            
            <div class="node-actions">
              <el-button type="danger" plain @click="handleDelete">
                <el-icon><Delete /></el-icon>
                删除文档
              </el-button>
            </div>
          </template>
          
          <el-empty v-else-if="!loading" description="节点不存在或无权访问" />
        </el-card>
      </el-main>
    </el-container>
    
    <el-dialog v-model="editDialogVisible" title="编辑标题" width="480px">
      <el-form label-width="60px">
        <el-form-item label="标题">
          <el-input v-model="editTitle" placeholder="请输入新标题" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="editDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="handleSaveEdit">
          保存
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
  background-color: var(--bg-color);
  padding: 32px;
  transition: background-color var(--transition-normal);
}

.page-header {
  margin-bottom: 20px;
  animation: slideUp var(--transition-normal);
}

.page-header .el-button {
  font-weight: 500;
}

.el-card {
  animation: slideUp var(--transition-slow);
}

.node-header {
  display: flex;
  align-items: flex-start;
  gap: 24px;
  padding: 8px 0;
}

.node-icon {
  flex-shrink: 0;
  width: 72px;
  height: 72px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--primary-bg);
  border-radius: var(--border-radius-lg);
}

.node-icon .el-icon {
  color: var(--primary-color);
}

.node-info {
  flex: 1;
}

.node-info h1 {
  font-size: 26px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0 0 12px 0;
}

.node-meta {
  display: flex;
  align-items: center;
  gap: 16px;
  flex-wrap: wrap;
}

.meta-item {
  font-size: 13px;
  color: var(--text-secondary);
}

.el-divider {
  border-color: var(--border-color);
}

.node-content {
  min-height: 200px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.node-actions {
  display: flex;
  justify-content: flex-end;
  padding-top: 8px;
}
</style>
