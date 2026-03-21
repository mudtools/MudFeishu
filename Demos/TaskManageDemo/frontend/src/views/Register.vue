<template>
  <div class="register-page" :class="{ dark: isDark }">
    <!-- 星空背景 -->
    <div class="stars-container">
      <div class="stars"></div>
      <div class="stars2"></div>
      <div class="stars3"></div>
    </div>

    <!-- 背景装饰 -->
    <div class="register-background">
      <div class="bg-circle circle-1"></div>
      <div class="bg-circle circle-2"></div>
      <div class="bg-grid"></div>
    </div>

    <!-- 注册卡片 -->
    <div class="register-container">
      <div class="register-brand">
        <div class="brand-logo">
          <el-icon :size="48">
            <List />
          </el-icon>
        </div>
        <h1 class="brand-name">TaskFlow</h1>
        <p class="brand-slogan">完善您的账户信息</p>
      </div>

      <el-card class="register-card" shadow="never">
        <!-- 飞书用户信息展示 -->
        <div v-if="feishuUser" class="feishu-user-info">
          <el-avatar :size="64" :src="feishuUser.avatarUrl" class="user-avatar">
            {{ feishuUser.name?.charAt(0) }}
          </el-avatar>
          <div class="user-details">
            <h3>{{ feishuUser.name }}</h3>
            <p v-if="feishuUser.email">{{ feishuUser.email }}</p>
            <el-tag type="success" size="small">飞书账号已验证</el-tag>
          </div>
        </div>

        <el-divider v-if="feishuUser" />

        <!-- 注册表单 -->
        <el-form
          ref="registerFormRef"
          :model="registerForm"
          :rules="registerRules"
          @submit.prevent="handleRegister"
        >
          <el-form-item prop="username">
            <el-input
              v-model="registerForm.username"
              placeholder="设置用户名"
              size="large"
              prefix-icon="User"
            />
          </el-form-item>
          <el-form-item prop="password">
            <el-input
              v-model="registerForm.password"
              type="password"
              placeholder="设置密码（至少6位）"
              size="large"
              prefix-icon="Lock"
              show-password
            />
          </el-form-item>
          <el-form-item prop="confirmPassword">
            <el-input
              v-model="registerForm.confirmPassword"
              type="password"
              placeholder="确认密码"
              size="large"
              prefix-icon="Lock"
              show-password
            />
          </el-form-item>
          <el-form-item>
            <el-button
              type="primary"
              size="large"
              :loading="loading"
              class="register-btn"
              native-type="submit"
            >
              {{ loading ? '注册中...' : '完成注册' }}
            </el-button>
          </el-form-item>
        </el-form>

        <div class="register-tips">
          <el-alert
            title="提示"
            description="设置用户名和密码后，您可以使用用户名密码或飞书账号登录系统"
            type="info"
            :closable="false"
            show-icon
          />
        </div>

        <div class="back-to-login">
          <el-button text type="primary" @click="goBackToLogin">
            <el-icon><ArrowLeft /></el-icon>
            返回登录
          </el-button>
        </div>
      </el-card>
    </div>

    <!-- 主题切换 -->
    <div class="theme-toggle">
      <el-button circle @click="toggleTheme">
        <el-icon :size="20">
          <Sunny v-show="themeStore.isDark" />
          <Moon v-show="!themeStore.isDark" />
        </el-icon>
      </el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue"
import { useRouter, useRoute } from "vue-router"
import { ElMessage, type FormInstance, type FormRules } from "element-plus"
import { List, Sunny, Moon, ArrowLeft } from "@element-plus/icons-vue"
import { useThemeStore } from "../stores/theme"
import { useAuthStore } from "../stores/auth"
import { register } from "../api"
import type { FeishuUserInfo, LoginResponse } from "../types"

const router = useRouter()
const route = useRoute()
const themeStore = useThemeStore()
const authStore = useAuthStore()

const loading = ref(false)
const registerFormRef = ref<FormInstance>()
const feishuUser = ref<FeishuUserInfo | null>(null)
const tempToken = ref<string | null>(null)

const registerForm = ref({
  username: '',
  password: '',
  confirmPassword: ''
})

const validateUsername = (_rule: unknown, value: string, callback: (error?: Error) => void) => {
  if (!value) {
    callback(new Error('请输入用户名'))
  } else if (value.length < 3) {
    callback(new Error('用户名至少3个字符'))
  } else if (!/^[a-zA-Z0-9_]+$/.test(value)) {
    callback(new Error('用户名只能包含字母、数字和下划线'))
  } else {
    callback()
  }
}

const validatePassword = (_rule: unknown, value: string, callback: (error?: Error) => void) => {
  if (!value) {
    callback(new Error('请输入密码'))
  } else if (value.length < 6) {
    callback(new Error('密码至少6个字符'))
  } else {
    if (registerForm.value.confirmPassword) {
      registerFormRef.value?.validateField('confirmPassword')
    }
    callback()
  }
}

const validateConfirmPassword = (_rule: unknown, value: string, callback: (error?: Error) => void) => {
  if (!value) {
    callback(new Error('请确认密码'))
  } else if (value !== registerForm.value.password) {
    callback(new Error('两次输入的密码不一致'))
  } else {
    callback()
  }
}

const registerRules: FormRules = {
  username: [
    { required: true, validator: validateUsername, trigger: 'blur' }
  ],
  password: [
    { required: true, validator: validatePassword, trigger: 'blur' }
  ],
  confirmPassword: [
    { required: true, validator: validateConfirmPassword, trigger: 'blur' }
  ]
}

const isDark = computed(() => themeStore.isDark())

onMounted(() => {
  const from = route.query.from as string
  if (from !== 'feishu') {
    router.replace('/login')
    return
  }

  const storedFeishuUser = sessionStorage.getItem('feishu_user')
  const storedTempToken = sessionStorage.getItem('temp_token')

  if (!storedFeishuUser || !storedTempToken) {
    ElMessage.warning('请先进行飞书授权')
    router.replace('/login')
    return
  }

  try {
    feishuUser.value = JSON.parse(storedFeishuUser)
    tempToken.value = storedTempToken

    if (feishuUser.value?.name) {
      const baseUsername = feishuUser.value.name.toLowerCase().replace(/\s+/g, '_')
      registerForm.value.username = baseUsername.replace(/[^a-z0-9_]/g, '').substring(0, 20)
    }
  } catch {
    ElMessage.error('获取飞书用户信息失败')
    router.replace('/login')
  }
})

const toggleTheme = () => {
  themeStore.toggleTheme()
}

const handleRegister = async () => {
  const valid = await registerFormRef.value?.validate()
  if (!valid) return

  if (!tempToken.value) {
    ElMessage.error('会话已过期，请重新授权')
    router.replace('/login')
    return
  }

  loading.value = true

  try {
    authStore.setToken(tempToken.value)

    const response = await register({
      username: registerForm.value.username,
      password: registerForm.value.password,
      confirmPassword: registerForm.value.confirmPassword
    })

    if (!response.success || !response.data) {
      ElMessage.error(response.message || "注册失败")
      return
    }

    sessionStorage.removeItem('feishu_user')
    sessionStorage.removeItem('temp_token')

    await handleRegisterSuccess(response.data)
  } catch (error) {
    console.error("注册失败:", error)
    ElMessage.error("注册失败，请稍后重试")
  } finally {
    loading.value = false
  }
}

const handleRegisterSuccess = async (data: LoginResponse) => {
  authStore.setToken(data.accessToken)

  if (data.user) {
    authStore.setUser({
      id: data.user.id,
      feishuId: data.user.feishuId,
      name: data.user.name,
      email: undefined,
      avatarUrl: undefined,
      role: data.user.role,
      permissions: data.user.permissions,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    })
  }

  localStorage.setItem("permissions", JSON.stringify(data.user.permissions || []))

  ElMessage.success({
    message: `注册成功，欢迎加入，${data.user.name}！`,
    duration: 2000,
  })

  router.replace("/tasks")
}

const goBackToLogin = () => {
  sessionStorage.removeItem('feishu_user')
  sessionStorage.removeItem('temp_token')
  router.replace('/login')
}
</script>

<style scoped>
.register-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
  overflow: hidden;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  transition: background var(--transition-normal);
}

.register-page.dark {
  background: linear-gradient(135deg, #1e1b4b 0%, #312e81 100%);
}

.register-background {
  position: absolute;
  inset: 0;
  overflow: hidden;
  pointer-events: none;
}

.bg-circle {
  position: absolute;
  border-radius: 50%;
  opacity: 0.1;
  filter: blur(60px);
}

.circle-1 {
  width: 600px;
  height: 600px;
  background: #fff;
  top: -200px;
  right: -200px;
}

.circle-2 {
  width: 400px;
  height: 400px;
  background: #f093fb;
  bottom: -100px;
  left: -100px;
}

.bg-grid {
  position: absolute;
  inset: 0;
  background-image: linear-gradient(
      rgba(255, 255, 255, 0.03) 1px,
      transparent 1px
    ),
    linear-gradient(90deg, rgba(255, 255, 255, 0.03) 1px, transparent 1px);
  background-size: 50px 50px;
}

.stars-container {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  pointer-events: none;
  z-index: 0;
}

.stars {
  width: 1px;
  height: 1px;
  background: transparent;
  box-shadow:
    3vw 15vh #fff, 7vw 8vh #fff, 12vw 23vh #fff, 18vw 5vh #fff, 24vw 31vh #fff,
    29vw 12vh #fff, 35vw 28vh #fff, 41vw 3vh #fff, 47vw 19vh #fff, 53vw 35vh #fff,
    58vw 7vh #fff, 64vw 25vh #fff, 70vw 11vh #fff, 76vw 33vh #fff, 82vw 6vh #fff,
    88vw 22vh #fff, 94vw 38vh #fff, 9vw 42vh #fff, 15vw 58vh #fff, 21vw 45vh #fff,
    27vw 62vh #fff, 33vw 48vh #fff, 39vw 67vh #fff, 45vw 52vh #fff, 51vw 71vh #fff,
    57vw 55vh #fff, 63vw 74vh #fff, 69vw 59vh #fff, 75vw 78vh #fff, 81vw 63vh #fff,
    87vw 82vh #fff, 93vw 68vh #fff, 6vw 85vh #fff, 14vw 92vh #fff, 22vw 77vh #fff,
    30vw 88vh #fff, 38vw 73vh #fff, 46vw 95vh #fff, 54vw 80vh #fff, 62vw 97vh #fff,
    70vw 83vh #fff, 78vw 91vh #fff, 86vw 76vh #fff, 94vw 87vh #fff, 2vw 98vh #fff;
  animation: animate-stars 50s linear infinite;
}

.stars2 {
  width: 2px;
  height: 2px;
  background: transparent;
  box-shadow:
    4vw 12vh #fff, 9vw 28vh #fff, 14vw 5vh #fff, 19vw 41vh #fff, 26vw 18vh #fff,
    32vw 55vh #fff, 38vw 8vh #fff, 44vw 36vh #fff, 50vw 62vh #fff, 56vw 15vh #fff,
    62vw 48vh #fff, 68vw 3vh #fff, 74vw 70vh #fff, 80vw 25vh #fff, 86vw 58vh #fff,
    92vw 11vh #fff, 98vw 44vh #fff, 3vw 67vh #fff, 11vw 32vh #fff, 17vw 89vh #fff;
  animation: animate-stars 100s linear infinite;
}

.stars3 {
  width: 3px;
  height: 3px;
  background: transparent;
  box-shadow:
    6vw 20vh #fff, 13vw 45vh #fff, 21vw 8vh #fff, 28vw 72vh #fff, 36vw 33vh #fff,
    43vw 58vh #fff, 51vw 15vh #fff, 58vw 88vh #fff, 66vw 40vh #fff, 73vw 67vh #fff,
    81vw 3vh #fff, 88vw 50vh #fff, 96vw 25vh #fff, 4vw 82vh #fff, 11vw 55vh #fff,
    19vw 12vh #fff, 26vw 78vh #fff, 34vw 30vh #fff, 41vw 95vh #fff, 49vw 7vh #fff,
    56vw 62vh #fff, 64vw 18vh #fff, 71vw 85vh #fff, 79vw 43vh #fff, 86vw 10vh #fff,
    94vw 70vh #fff, 2vw 37vh #fff, 9vw 98vh #fff, 17vw 52vh #fff, 24vw 23vh #fff;
  animation: animate-stars 150s linear infinite;
}

@keyframes animate-stars {
  from {
    transform: translateY(0);
  }
  to {
    transform: translateY(-100vh);
  }
}

.register-container {
  position: relative;
  z-index: 1;
  width: 100%;
  max-width: 420px;
  padding: 20px;
}

.register-brand {
  text-align: center;
  margin-bottom: 32px;
}

.brand-logo {
  width: 80px;
  height: 80px;
  margin: 0 auto 16px;
  background: rgba(255, 255, 255, 0.2);
  backdrop-filter: blur(10px);
  border-radius: var(--radius-xl);
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fff;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.1);
}

.brand-name {
  font-size: 32px;
  font-weight: 700;
  color: #fff;
  margin: 0 0 8px;
  letter-spacing: -0.5px;
}

.brand-slogan {
  font-size: 16px;
  color: rgba(255, 255, 255, 0.8);
  margin: 0;
}

.register-card {
  background: rgba(255, 255, 255, 0.95);
  backdrop-filter: blur(20px);
  border-radius: var(--radius-xl);
  border: 1px solid rgba(255, 255, 255, 0.2);
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.15);
}

.feishu-user-info {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 16px;
  background: var(--bg-secondary);
  border-radius: var(--radius-lg);
  margin-bottom: 8px;
}

.user-avatar {
  flex-shrink: 0;
  background: linear-gradient(135deg, #3370ff 0%, #2b5ce6 100%);
  color: white;
  font-size: 24px;
  font-weight: 600;
}

.user-details {
  flex: 1;
  min-width: 0;
}

.user-details h3 {
  margin: 0 0 4px;
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
}

.user-details p {
  margin: 0 0 8px;
  font-size: 13px;
  color: var(--text-secondary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.register-btn {
  width: 100%;
  height: 44px;
  font-size: 16px;
  font-weight: 500;
}

.register-tips {
  margin-top: 20px;
}

.back-to-login {
  margin-top: 20px;
  text-align: center;
}

.theme-toggle {
  position: fixed;
  top: 24px;
  right: 24px;
  z-index: 100;
}

.theme-toggle .el-button {
  background: rgba(255, 255, 255, 0.2);
  backdrop-filter: blur(10px);
  border: 1px solid rgba(255, 255, 255, 0.1);
  color: white;
}

.theme-toggle .el-button:hover {
  background: rgba(255, 255, 255, 0.3);
}
</style>
