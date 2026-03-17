<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useUserStore } from '@/stores/user'
import { wikiApi, authApi } from '@/api'
import AppHeader from '@/components/common/AppHeader.vue'
import AppSidebar from '@/components/common/AppSidebar.vue'

const router = useRouter()
const userStore = useUserStore()
const searchQuery = ref('')
const searchResults = ref<any[]>([])
const loading = ref(false)
const hasMore = ref(false)
const pageToken = ref<string | undefined>()

async function handleSearch() {
  if (!searchQuery.value.trim()) {
    ElMessage.warning('请输入搜索关键词')
    return
  }

  try {
    loading.value = true
    const response = await wikiApi.search({
      query: searchQuery.value,
      pageSize: 20
    })
    
    searchResults.value = response.data.items || []
    hasMore.value = response.data.hasMore
    pageToken.value = response.data.pageToken
  } catch (error: any) {
    ElMessage.error(error.message || '搜索失败')
  } finally {
    loading.value = false
  }
}

// ObjType 映射（飞书 API 返回的是数字）
const objTypeMap: Record<number, { icon: string; label: string; tagType: string }> = {
  1: { icon: 'Document', label: '文档', tagType: 'primary' },      // docx
  2: { icon: 'Grid', label: '表格', tagType: 'success' },         // sheet
  3: { icon: 'Table', label: '多维表格', tagType: 'warning' },    // bitable
  4: { icon: 'PictureFilled', label: '演示文稿', tagType: 'danger' }, // slides
  5: { icon: 'Share', label: '思维笔记', tagType: 'info' },       // mindnote
  6: { icon: 'Document', label: '文件', tagType: '' }             // file
}

function getObjTypeIcon(objType: number | string): string {
  if (typeof objType === 'number') {
    return objTypeMap[objType]?.icon || 'Document'
  }
  // 兼容字符串类型
  const icons: Record<string, string> = {
    docx: 'Document',
    sheet: 'Grid',
    bitable: 'Table',
    slides: 'PictureFilled',
    mindnote: 'Share',
    file: 'Document'
  }
  return icons[objType] || 'Document'
}

function getObjTypeLabel(objType: number | string): string {
  if (typeof objType === 'number') {
    return objTypeMap[objType]?.label || '文档'
  }
  // 兼容字符串类型
  const labels: Record<string, string> = {
    docx: '文档',
    sheet: '表格',
    bitable: '多维表格',
    slides: '演示文稿',
    mindnote: '思维笔记',
    file: '文件'
  }
  return labels[objType] || '文档'
}

function getObjTypeTagType(objType: number | string): string {
  if (typeof objType === 'number') {
    return objTypeMap[objType]?.tagType || 'info'
  }
  // 兼容字符串类型
  const types: Record<string, string> = {
    docx: 'primary',
    sheet: 'success',
    bitable: 'warning',
    slides: 'danger',
    mindnote: 'info',
    file: ''
  }
  return types[objType] || 'info'
}

function getDomainFromUrl(url: string): string {
  try {
    const urlObj = new URL(url)
    return urlObj.hostname.replace('www.', '')
  } catch {
    return '飞书文档'
  }
}

function handleResultClick(result: any) {
  // 优先使用飞书提供的 url
  if (result.url) {
    window.open(result.url, '_blank')
  } else if (result.objToken) {
    const url = `https://www.feishu.cn/docs/${result.objToken}`
    window.open(url, '_blank')
  } else if (result.nodeId && result.spaceId) {
    router.push(`/spaces/${result.spaceId}/nodes/${result.nodeId}`)
  } else {
    ElMessage.warning('无法打开该文档')
  }
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
</script>

<template>
  <el-container class="main-layout" direction="vertical">
    <AppHeader @logout="handleLogout" />
    
    <el-container>
      <AppSidebar @logout="handleLogout" />
      
      <el-main class="main-content">
        <div class="search-section">
          <el-input
            v-model="searchQuery"
            placeholder="搜索知识库文档..."
            size="large"
            clearable
            @keyup.enter="handleSearch"
          >
            <template #prefix>
              <el-icon><Search /></el-icon>
            </template>
            <template #append>
              <el-button type="primary" :loading="loading" @click="handleSearch">
                搜索
              </el-button>
            </template>
          </el-input>
        </div>
        
        <div v-if="searchResults.length > 0" class="results-section">
          <h3>搜索结果 ({{ searchResults.length }})</h3>
          
          <el-card 
            v-for="result in searchResults" 
            :key="result.nodeId || result.objToken" 
            class="result-card"
            shadow="hover"
            @click="handleResultClick(result)"
          >
            <div class="result-item">
              <el-icon :size="20" class="result-icon">
                <component :is="getObjTypeIcon(result.objType)" />
              </el-icon>
              <div class="result-content">
                <div class="result-title">{{ result.title || '无标题' }}</div>
                <div class="result-meta">
                  <el-tag size="small" :type="getObjTypeTagType(result.objType)">{{ getObjTypeLabel(result.objType) }}</el-tag>
                  <span v-if="result.url" class="result-url">{{ getDomainFromUrl(result.url) }}</span>
                </div>
              </div>
            </div>
          </el-card>
        </div>
        
        <el-empty 
          v-else-if="!loading && searchQuery" 
          description="未找到相关文档" 
        />
      </el-main>
    </el-container>
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

.search-section {
  max-width: 640px;
  margin: 0 auto 40px;
  animation: slideUp var(--transition-normal);
}

.search-section :deep(.el-input__wrapper) {
  border-radius: var(--border-radius-lg);
  padding: 8px 16px;
}

.search-section :deep(.el-input__inner) {
  font-size: 16px;
}

.results-section {
  max-width: 800px;
  margin: 0 auto;
  animation: slideUp var(--transition-slow);
}

.results-section h3 {
  margin-bottom: 20px;
  font-size: 18px;
  font-weight: 600;
  color: var(--text-primary);
}

.result-card {
  margin-bottom: 16px;
  cursor: pointer;
  transition: all var(--transition-normal);
}

.result-card:hover {
  transform: translateX(4px);
}

.result-item {
  display: flex;
  align-items: center;
}

.result-icon {
  margin-right: 16px;
  color: var(--primary-color);
}

.result-content {
  flex: 1;
}

.result-title {
  font-size: 15px;
  font-weight: 500;
  color: var(--text-primary);
  margin-bottom: 6px;
}

.result-meta {
  display: flex;
  align-items: center;
  gap: 10px;
}

.result-space {
  color: var(--text-secondary);
  font-size: 12px;
}

.result-url {
  color: var(--text-tertiary);
  font-size: 12px;
}

.result-url::before {
  content: '•';
  margin: 0 6px;
  color: var(--text-tertiary);
}
</style>
