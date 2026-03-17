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

function getObjTypeIcon(objType: string) {
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
            :key="result.nodeToken" 
            class="result-card"
            shadow="hover"
          >
            <div class="result-item">
              <el-icon :size="20" class="result-icon">
                <component :is="getObjTypeIcon(result.objType)" />
              </el-icon>
              <div class="result-content">
                <div class="result-title">{{ result.title }}</div>
                <div class="result-meta">
                  <el-tag size="small" type="info">{{ result.objType }}</el-tag>
                  <span v-if="result.spaceName" class="result-space">{{ result.spaceName }}</span>
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
</style>
