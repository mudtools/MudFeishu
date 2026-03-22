<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Loading, CircleClose } from '@element-plus/icons-vue'
import { loginWithCode, bindFeishu, completeFeishuBind } from '../api'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()
const loading = ref(true)
const error = ref('')
const needBind = ref(false)
const tempToken = ref('')
const bindForm = ref({
  username: '',
  password: '',
  confirmPassword: ''
})
const binding = ref(false)
const bindFormRef = ref()

async function handleCallback() {
  const code = route.query.code as string
  const state = route.query.state as string

  if (!code || !state) {
    error.value = '缺少必要的授权参数'
    loading.value = false
    return
  }

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

    if (response.success && response.data) {
      if (!response.data.isFeishuBound) {
        needBind.value = true
        tempToken.value = response.data.accessToken
        loading.value = false
        return
      }

      if (response.data.accessToken && response.data.user && response.data.user.id > 0) {
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

async function handleBindSubmit() {
  if (!bindFormRef.value) return

  await bindFormRef.value.validate(async (valid: boolean) => {
    if (!valid) return

    if (bindForm.value.password !== bindForm.value.confirmPassword) {
      ElMessage.error('两次输入的密码不一致')
      return
    }

    if (bindForm.value.password.length < 6) {
      ElMessage.error('密码长度至少6位')
      return
    }

    binding.value = true

    try {
      const response = await completeFeishuBind({
        tempToken: tempToken.value,
        username: bindForm.value.username,
        password: bindForm.value.password
      })

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

        ElMessage.success({
          message: `欢迎首次使用，${response.data.user?.name || '用户'}！`,
          duration: 2000,
        })

        router.replace('/tasks')
      } else {
        ElMessage.error(response.message || '绑定失败')
      }
    } catch (err: any) {
      console.error('绑定失败:', err)
      ElMessage.error(err.response?.data?.message || err.message || '绑定失败')
    } finally {
      binding.value = false
    }
  })
}

async function handleBindCallback(code: string, state: string) {
  try {
    const response = await bindFeishu({ code, state })

    if (response.success && response.data?.success) {
      const currentUser = authStore.user
      if (currentUser && response.data) {
        authStore.setUser({
          ...currentUser,
          name: response.data.feishuName || currentUser.name,
          avatarUrl: response.data.feishuAvatar || currentUser.avatarUrl,
          email: response.data.email || currentUser.email,
          mobile: response.data.mobile,
          englishName: response.data.englishName,
        })
      }

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

const validateUsername = (rule: any, value: string, callback: any) => {
  if (!value) {
    callback(new Error('请输入用户名'))
  } else if (value.length < 3) {
    callback(new Error('用户名至少3个字符'))
  } else {
    callback()
  }
}

const validatePassword = (rule: any, value: string, callback: any) => {
  if (!value) {
    callback(new Error('请输入密码'))
  } else if (value.length < 6) {
    callback(new Error('密码至少6个字符'))
  } else {
    callback()
  }
}

const validateConfirmPassword = (rule: any, value: string, callback: any) => {
  if (!value) {
    callback(new Error('请再次输入密码'))
  } else if (value !== bindForm.value.password) {
    callback(new Error('两次输入的密码不一致'))
  } else {
    callback()
  }
}

const bindRules = {
  username: [{ validator: validateUsername, trigger: 'blur' }],
  password: [{ validator: validatePassword, trigger: 'blur' }],
  confirmPassword: [{ validator: validateConfirmPassword, trigger: 'blur' }]
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

      <div v-if="needBind && !loading" class="bind-form">
        <h3 class="bind-title">完成账户设置</h3>
        <p class="bind-desc">请设置本地账户的用户名和密码，以便下次登录使用</p>
        
        <el-form ref="bindFormRef" :model="bindForm" :rules="bindRules" label-position="top">
          <el-form-item label="用户名" prop="username">
            <el-input 
              v-model="bindForm.username" 
              placeholder="请输入用户名（至少3个字符）"
              :disabled="binding"
            />
          </el-form-item>
          <el-form-item label="密码" prop="password">
            <el-input 
              v-model="bindForm.password" 
              type="password"
              placeholder="请输入密码（至少6个字符）"
              show-password
              :disabled="binding"
            />
          </el-form-item>
          <el-form-item label="确认密码" prop="confirmPassword">
            <el-input 
              v-model="bindForm.confirmPassword" 
              type="password"
              placeholder="请再次输入密码"
              show-password
              :disabled="binding"
            />
          </el-form-item>
          <el-form-item>
            <el-button 
              type="primary" 
              :loading="binding" 
              class="bind-btn"
              @click="handleBindSubmit"
            >
              {{ binding ? '提交中...' : '完成绑定' }}
            </el-button>
          </el-form-item>
        </el-form>
      </div>
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
  min-width: 360px;
  max-width: 400px;
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

.bind-form {
  text-align: left;
  margin-top: 24px;
}

.bind-title {
  font-size: 20px;
  font-weight: 600;
  color: var(--el-text-color-primary);
  margin-bottom: 8px;
}

.bind-desc {
  font-size: 14px;
  color: var(--el-text-color-secondary);
  margin-bottom: 24px;
}

.bind-btn {
  width: 100%;
  margin-top: 8px;
}
</style>
