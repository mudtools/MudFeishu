<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useUserStore } from '@/stores/user'
import { authApi } from '@/api'
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
    
    await authApi.logout()
    userStore.logout()
    ElMessage.success('已退出登录')
    router.push('/login')
  } catch (error) {
    // 用户取消
  }
}

function toggleSidebar() {
  collapsed.value = !collapsed.value
}
</script>

<template>
  <el-container class="main-layout">
    <AppSidebar :collapsed="collapsed" @logout="handleLogout" />
    
    <el-container>
      <AppHeader @toggle="toggleSidebar" @logout="handleLogout" />
      
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
  background-color: #f5f7fa;
  padding: 24px;
}

.welcome-section {
  margin-bottom: 32px;
}

.welcome-section h1 {
  font-size: 24px;
  color: #303133;
  margin: 0 0 8px 0;
}

.welcome-section p {
  color: #909399;
  margin: 0;
}

.action-card {
  cursor: pointer;
  text-align: center;
  padding: 24px;
  transition: all 0.3s;
}

.action-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12);
}

.action-card h3 {
  margin: 16px 0 8px 0;
  font-size: 16px;
  color: #303133;
}

.action-card p {
  margin: 0;
  color: #909399;
  font-size: 13px;
}
</style>
