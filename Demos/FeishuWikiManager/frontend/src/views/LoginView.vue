<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { authApi } from '@/api'
import { useThemeStore } from '@/stores/theme'
import ThemeSwitch from '@/components/common/ThemeSwitch.vue'

const router = useRouter()
const themeStore = useThemeStore()
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
  // 初始化主题
  themeStore.initTheme()
})
</script>

<template>
  <div class="login-container">
    <div class="theme-toggle">
      <ThemeSwitch />
    </div>
    
    <div class="login-card">
      <div class="login-header">
        <h1>
          <svg class="logo-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path d="M4 4C4 2.89543 4.89543 2 6 2H14L20 8V20C20 21.1046 19.1046 22 18 22H6C4.89543 22 4 21.1046 4 20V4Z" fill="currentColor" fill-opacity="0.15"/>
            <path d="M14 2V8H20" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            <path d="M8 13H16M8 17H13" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
          </svg>
          飞书知识库管理
        </h1>
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

/* 暗色模式下的背景 */
:root.dark .login-container {
  background: linear-gradient(135deg, #1e3a5f 0%, #0f172a 100%);
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

/* 暗色模式下的装饰效果 */
:root.dark .login-container::before {
  background: radial-gradient(circle, rgba(59, 130, 246, 0.15) 0%, transparent 50%);
}

@keyframes rotate {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

.theme-toggle {
  position: absolute;
  top: 24px;
  right: 24px;
  z-index: 10;
}

.theme-toggle :deep(.theme-switch) {
  background: rgba(255, 255, 255, 0.2);
  backdrop-filter: blur(10px);
  border: 1px solid rgba(255, 255, 255, 0.3);
}

.theme-toggle :deep(.theme-switch:hover) {
  background: rgba(255, 255, 255, 0.3);
}

:root.dark .theme-toggle :deep(.theme-switch) {
  background: rgba(0, 0, 0, 0.3);
  border-color: rgba(255, 255, 255, 0.1);
}

:root.dark .theme-toggle :deep(.theme-switch:hover) {
  background: rgba(0, 0, 0, 0.4);
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
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 12px;
}

.logo-icon {
  width: 36px;
  height: 36px;
  color: var(--primary-color);
  flex-shrink: 0;
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

:root.dark .login-button:hover {
  box-shadow: 0 4px 12px rgba(59, 130, 246, 0.3);
}

.login-footer {
  text-align: center;
  color: var(--text-tertiary);
  font-size: 12px;
}
</style>
