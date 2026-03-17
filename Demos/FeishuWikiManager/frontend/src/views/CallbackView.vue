<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { authApi } from '@/api'
import { useUserStore } from '@/stores/user'

const router = useRouter()
const route = useRoute()
const userStore = useUserStore()
const loading = ref(true)
const error = ref('')

async function handleCallback() {
  const code = route.query.code as string
  const state = route.query.state as string

  if (!code || !state) {
    error.value = '缺少必要的授权参数'
    loading.value = false
    return
  }

  try {
    const response = await authApi.callback(code, state)
    
    if (response.data.success && response.data.token) {
      userStore.setToken(response.data.token)
      if (response.data.user) {
        userStore.setUser(response.data.user)
      }
      ElMessage.success('登录成功')
      
      const redirect = route.query.redirect as string || '/'
      router.replace(redirect)
    } else {
      error.value = response.data.message || '登录失败'
    }
  } catch (err: any) {
    error.value = err.response?.data?.message || err.message || '登录失败'
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  handleCallback()
})
</script>

<template>
  <div class="callback-container">
    <div class="callback-card">
      <el-icon v-if="loading" class="loading-icon" :size="48">
        <Loading />
      </el-icon>
      <el-icon v-else-if="error" class="error-icon" :size="48">
        <CircleClose />
      </el-icon>
      
      <p v-if="loading">正在处理登录...</p>
      <p v-else-if="error" class="error-message">{{ error }}</p>
      
      <el-button v-if="error" type="primary" @click="router.push('/login')">
        返回登录
      </el-button>
    </div>
  </div>
</template>

<style scoped>
.callback-container {
  height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, var(--primary-color) 0%, var(--primary-color-dark) 100%);
}

.callback-card {
  text-align: center;
  padding: 48px 40px;
  background: var(--card-bg);
  border-radius: var(--border-radius-xl);
  box-shadow: var(--shadow-xl);
}

.loading-icon {
  color: var(--primary-color);
  animation: spin 1s linear infinite;
}

.error-icon {
  color: var(--danger-color);
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

.error-message {
  color: var(--danger-color);
  margin: 16px 0;
}

p {
  color: var(--text-primary);
  margin-top: 16px;
}
</style>
