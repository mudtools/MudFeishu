<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useUserStore } from '@/stores/user'
import { useWikiStore } from '@/stores/wiki'
import AppHeader from '@/components/common/AppHeader.vue'
import AppSidebar from '@/components/common/AppSidebar.vue'
import SpaceCard from '@/components/wiki/SpaceCard.vue'
import type { Space } from '@/types'

const router = useRouter()
const userStore = useUserStore()
const wikiStore = useWikiStore()
const createDialogVisible = ref(false)
const newSpaceName = ref('')
const newSpaceDesc = ref('')
const creating = ref(false)

async function loadSpaces() {
  try {
    await wikiStore.fetchSpaces(20, true)
  } catch (error: any) {
    ElMessage.error(error.message || '加载知识空间失败')
  }
}

async function handleCreateSpace() {
  if (!newSpaceName.value.trim()) {
    ElMessage.warning('请输入知识空间名称')
    return
  }

  try {
    creating.value = true
    const space = await wikiStore.createSpace(newSpaceName.value, newSpaceDesc.value)
    if (space) {
      ElMessage.success('创建成功')
      createDialogVisible.value = false
      newSpaceName.value = ''
      newSpaceDesc.value = ''
    }
  } catch (error: any) {
    ElMessage.error(error.message || '创建失败')
  } finally {
    creating.value = false
  }
}

function handleSpaceClick(space: Space) {
  router.push(`/spaces/${space.spaceId}`)
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

onMounted(() => {
  loadSpaces()
})
</script>

<template>
  <el-container class="main-layout" direction="vertical">
    <AppHeader @logout="handleLogout">
      <template #extra>
        <el-button type="primary" @click="createDialogVisible = true">
          <el-icon><Plus /></el-icon>
          新建空间
        </el-button>
      </template>
    </AppHeader>
    
    <el-container>
      <AppSidebar @logout="handleLogout" />
      
      <el-main class="main-content">
        <div class="page-header">
          <h1>知识空间</h1>
          <p>管理您的所有知识空间</p>
        </div>
        
        <el-skeleton v-if="wikiStore.loading" :rows="3" animated />
        
        <el-row v-else :gutter="20">
          <el-col 
            v-for="space in wikiStore.spaces" 
            :key="space.spaceId" 
            :xs="24" 
            :sm="12" 
            :md="8" 
            :lg="6"
          >
            <SpaceCard :space="space" @click="handleSpaceClick(space)" />
          </el-col>
        </el-row>
        
        <el-empty v-if="!wikiStore.loading && wikiStore.spaces.length === 0" description="暂无知识空间">
          <el-button type="primary" @click="createDialogVisible = true">创建知识空间</el-button>
        </el-empty>
      </el-main>
    </el-container>
    
    <el-dialog v-model="createDialogVisible" title="新建知识空间" width="480px">
      <el-form label-width="80px">
        <el-form-item label="名称" required>
          <el-input v-model="newSpaceName" placeholder="请输入知识空间名称" />
        </el-form-item>
        <el-form-item label="描述">
          <el-input 
            v-model="newSpaceDesc" 
            type="textarea" 
            :rows="3"
            placeholder="请输入描述（可选）" 
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="createDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="creating" @click="handleCreateSpace">
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
</style>
