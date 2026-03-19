<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useUserStore } from '@/stores/user'
import AppHeader from '@/components/common/AppHeader.vue'
import AppSidebar from '@/components/common/AppSidebar.vue'

const router = useRouter()
const userStore = useUserStore()
const collapsed = ref(false)

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
    // 用户取消或其他错误
  }
}

function toggleSidebar() {
  collapsed.value = !collapsed.value
}
</script>

<template>
  <el-container class="main-layout" direction="vertical">
    <AppHeader @toggle="toggleSidebar" @logout="handleLogout" />
    
    <el-container>
      <AppSidebar :collapsed="collapsed" @logout="handleLogout" />
      
      <el-main class="main-content">
        <div class="welcome-section">
          <h1>欢迎回来，{{ userStore.user?.name || '用户' }}</h1>
          <p>选择一个知识空间开始管理您的文档</p>
        </div>
        
        <div class="quick-actions">
          <el-row :gutter="20">
            <el-col :span="8">
              <el-card shadow="hover" class="action-card" @click="router.push('/spaces')">
                <el-icon :size="32" color="#3370ff"><Folder /></el-icon>
                <h3>知识空间</h3>
                <p>浏览和管理您的知识空间</p>
              </el-card>
            </el-col>
            <el-col :span="8">
              <el-card shadow="hover" class="action-card" @click="router.push('/search')">
                <el-icon :size="32" color="#67c23a"><Search /></el-icon>
                <h3>搜索文档</h3>
                <p>在知识库中搜索文档</p>
              </el-card>
            </el-col>
            <el-col :span="8">
              <el-card shadow="hover" class="action-card">
                <el-icon :size="32" color="#e6a23c"><Star /></el-icon>
                <h3>收藏夹</h3>
                <p>快速访问收藏的文档</p>
              </el-card>
            </el-col>
          </el-row>
        </div>
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

.welcome-section {
  margin-bottom: 40px;
  animation: slideUp var(--transition-normal);
}

.welcome-section h1 {
  font-size: 28px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0 0 8px 0;
}

.welcome-section p {
  color: var(--text-secondary);
  margin: 0;
  font-size: 15px;
}

.quick-actions {
  animation: slideUp var(--transition-slow);
}

.action-card {
  cursor: pointer;
  text-align: center;
  padding: 32px 24px;
  transition: all var(--transition-normal);
  border: 1px solid var(--card-border);
  background: var(--card-bg);
}

.action-card:hover {
  transform: translateY(-6px);
  box-shadow: var(--shadow-xl);
  border-color: var(--primary-color);
}

.action-card :deep(.el-card__body) {
  display: flex;
  flex-direction: column;
  align-items: center;
}

.action-card .el-icon {
  transition: transform var(--transition-normal);
}

.action-card:hover .el-icon {
  transform: scale(1.1);
}

.action-card h3 {
  margin: 20px 0 8px 0;
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
}

.action-card p {
  margin: 0;
  color: var(--text-secondary);
  font-size: 13px;
}
</style>
