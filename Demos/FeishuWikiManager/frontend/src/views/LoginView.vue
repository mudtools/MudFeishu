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
  background: linear-gradient(135deg, var(--primary-color) 0%, var(--primary-color-dark) 100%);
  position: relative;
  overflow: hidden;
}

.login-container::before {
  content: '';
  position: absolute;
  top: -50%;
  left: -50%;
  width: 200%;
  height: 200%;
  background: radial-gradient(circle, rgba(255,255,255,0.1) 0%, transparent 50%);
  animation: rotate 20s linear infinite;
}

@keyframes rotate {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

.login-card {
  width: 420px;
  padding: 48px 40px;
  background: var(--card-bg);
  border-radius: var(--border-radius-xl);
  box-shadow: var(--shadow-xl);
  position: relative;
  z-index: 1;
  animation: slideUp var(--transition-slow);
}

.login-header {
  text-align: center;
  margin-bottom: 48px;
}

.login-header h1 {
  font-size: 28px;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0 0 12px 0;
}

.login-header p {
  color: var(--text-secondary);
  margin: 0;
  font-size: 15px;
}

.login-content {
  margin-bottom: 32px;
}

.login-button {
  width: 100%;
  height: 52px;
  font-size: 16px;
  font-weight: 500;
  border-radius: var(--border-radius);
  transition: all var(--transition-fast);
}

.login-button:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(51, 112, 255, 0.4);
}

.login-footer {
  text-align: center;
  color: var(--text-tertiary);
  font-size: 12px;
}
</style>
