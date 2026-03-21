<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Loading, CircleClose } from '@element-plus/icons-vue'
import { loginWithCode, bindFeishu } from '../api'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()
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

  // 检查是否是绑定飞书账号的回调
  const bindState = sessionStorage.getItem('feishu_bind_state')
  if (bindState) {
    if (bindState !== state) {
      error.value = 'State 验证失败，请重新绑定'
      loading.value = false
      return
    }
    sessionStorage.removeItem('feishu_bind_state')
    await handleBindCallback(code, state)
    return
  }

  // 验证登录 state
  const storedState = sessionStorage.getItem('feishu_oauth_state')
  if (storedState && storedState !== state) {
    error.value = 'State 验证失败，请重新登录'
    loading.value = false
    return
  }
  sessionStorage.removeItem('feishu_oauth_state')

  await handleLoginCallback(code, state)
}

async function handleLoginCallback(code: string, state: string) {
  try {
    const response = await loginWithCode({ code, state })

    if (response.success && response.data?.accessToken) {
      authStore.setToken(response.data.accessToken)

      if (response.data.user) {
        authStore.setUser({
          id: response.data.user.id,
          feishuId: response.data.user.feishuId,
          name: response.data.user.name,
          email: response.data.user.email,
          avatarUrl: response.data.user.avatarUrl,
          role: response.data.user.role,
          permissions: response.data.user.permissions || [],
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
        })
      }

      localStorage.setItem(
        'permissions',
        JSON.stringify(response.data.user?.permissions || [])
      )

      const welcomeMessage = response.data.isFirstLogin
        ? `欢迎首次使用，${response.data.user?.name || '用户'}！`
        : `欢迎回来，${response.data.user?.name || '用户'}！`

      ElMessage.success({
        message: welcomeMessage,
        duration: 2000,
      })

      const redirect = (route.query.redirect as string) || '/tasks'
      router.replace(redirect)
    } else {
      error.value = response.message || '登录失败'
    }
  } catch (err: any) {
    console.error('飞书登录失败:', err)
    error.value = err.response?.data?.message || err.message || '登录失败'
  } finally {
    loading.value = false
  }
}

async function handleBindCallback(code: string, state: string) {
  try {
    const response = await bindFeishu({ code, state })

    if (response.success && response.data?.success) {
      ElMessage.success({
        message: `飞书账号绑定成功！欢迎，${response.data.feishuName || '用户'}！`,
        duration: 2000,
      })
      router.replace('/tasks')
    } else {
      error.value = response.data?.message || response.message || '绑定失败'
    }
  } catch (err: any) {
    console.error('绑定飞书账号失败:', err)
    error.value = err.response?.data?.message || err.message || '绑定失败'
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

      <p v-if="loading" class="status-text">正在处理登录...</p>
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
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.callback-card {
  text-align: center;
  padding: 48px 40px;
  background: var(--el-bg-color);
  border-radius: 16px;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.15);
  min-width: 320px;
}

.loading-icon {
  color: var(--el-color-primary);
  animation: spin 1s linear infinite;
}

.error-icon {
  color: var(--el-color-danger);
}

@keyframes spin {
  from {
    transform: rotate(0deg);
  }
  to {
    transform: rotate(360deg);
  }
}

.error-message {
  color: var(--el-color-danger);
  margin: 16px 0;
  font-size: 14px;
}

.status-text {
  color: var(--el-text-color-primary);
  margin-top: 16px;
  font-size: 16px;
}

.el-button {
  margin-top: 16px;
}
</style>
