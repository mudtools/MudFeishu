<script setup lang="ts">
import { ref } from 'vue'
import { ElMessage } from 'element-plus'
import { wikiApi } from '@/api'
import AppHeader from '@/components/common/AppHeader.vue'
import AppSidebar from '@/components/common/AppSidebar.vue'

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
</script>

<template>
  <el-container class="main-layout">
    <AppSidebar />
    
    <el-container>
      <AppHeader />
      
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
  background-color: #f5f7fa;
  padding: 24px;
}

.search-section {
  max-width: 600px;
  margin: 0 auto 32px;
}

.results-section {
  max-width: 800px;
  margin: 0 auto;
}

.results-section h3 {
  margin-bottom: 16px;
  color: #303133;
}

.result-card {
  margin-bottom: 12px;
  cursor: pointer;
}

.result-item {
  display: flex;
  align-items: center;
}

.result-icon {
  margin-right: 12px;
  color: #3370ff;
}

.result-content {
  flex: 1;
}

.result-title {
  font-size: 15px;
  color: #303133;
  margin-bottom: 4px;
}

.result-meta {
  display: flex;
  align-items: center;
  gap: 8px;
}

.result-space {
  color: #909399;
  font-size: 12px;
}
</style>
