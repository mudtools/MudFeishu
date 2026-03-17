<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { authApi } from '@/api'

const router = useRouter()
const loading = ref(false)

async function handleLogin() {
  try {
    loading.value = true
    const response = await authApi.getAuthUrl()
    if (response.data.success && response.data.url) {
      window.location.href = response.data.url
    } else {
      ElMessage.error(response.data.message || '获取授权链接失败')
    }
  } catch (error: any) {
    ElMessage.error(error.message || '登录失败')
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  const token = localStorage.getItem('token')
  if (token) {
    router.replace('/')
  }
})
</script>

<template>
  <div class="login-container">
    <div class="login-card">
      <div class="login-header">
        <h1>飞书知识库管理</h1>
        <p>管理您的飞书个人知识库</p>
      </div>
      
      <div class="login-content">
        <el-button 
          type="primary" 
          size="large" 
          :loading="loading"
          @click="handleLogin"
          class="login-button"
        >
          <el-icon><User /></el-icon>
          使用飞书账号登录
        </el-button>
      </div>
      
      <div class="login-footer">
        <p>登录即表示您同意我们的服务条款和隐私政策</p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.login-container {
  height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.login-card {
  width: 400px;
  padding: 40px;
  background: white;
  border-radius: 12px;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
}

.login-header {
  text-align: center;
  margin-bottom: 40px;
}

.login-header h1 {
  font-size: 28px;
  color: #303133;
  margin: 0 0 12px 0;
}

.login-header p {
  color: #909399;
  margin: 0;
}

.login-content {
  margin-bottom: 30px;
}

.login-button {
  width: 100%;
  height: 48px;
  font-size: 16px;
}

.login-footer {
  text-align: center;
  color: #909399;
  font-size: 12px;
}
</style>
