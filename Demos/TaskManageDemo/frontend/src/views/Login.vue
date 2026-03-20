<template>
  <div class="login-page" :class="{ dark: isDark }">
    <!-- 亮色模式浮动几何图形背景 -->
    <div class="floating-shapes-container" v-show="!isDark">
      <div class="floating-shape shape-1"></div>
      <div class="floating-shape shape-2"></div>
      <div class="floating-shape shape-3"></div>
      <div class="floating-shape shape-4"></div>
      <div class="floating-shape shape-5"></div>
      <div class="floating-shape shape-6"></div>
      <div class="floating-shape shape-7"></div>
      <div class="floating-shape shape-8"></div>
      <div class="floating-shape shape-9"></div>
      <div class="floating-shape shape-10"></div>
      <div class="floating-shape shape-11"></div>
      <div class="floating-shape shape-12"></div>
    </div>

    <!-- 暗色模式星空背景 -->
    <div class="stars-container" v-show="isDark">
      <div class="stars"></div>
      <div class="stars2"></div>
      <div class="stars3"></div>
      <div class="stars4"></div>
      <div class="shooting-star star-tr-bl tail-reverse"></div>
      <div class="shooting-star star-tl-br"></div>
      <div class="shooting-star star-top-right tail-reverse"></div>
      <div class="shooting-star star-left-top"></div>
      <div class="shooting-star star-diagonal-1"></div>
      <div class="shooting-star star-diagonal-2 tail-reverse"></div>
    </div>

    <!-- 背景装饰 -->
    <div class="login-background">
      <div class="bg-circle circle-1"></div>
      <div class="bg-circle circle-2"></div>
      <div class="bg-circle circle-3"></div>
      <div class="bg-grid"></div>
      <div class="nebula"></div>
    </div>

    <!-- 登录卡片 -->
    <div class="login-container">
      <div class="login-brand">
        <div class="brand-logo">
          <el-icon :size="48">
            <List />
          </el-icon>
        </div>
        <h1 class="brand-name">Mud 任务管理</h1>
        <p class="brand-slogan">高效协作，轻松管理</p>
      </div>

      <el-card class="login-card" shadow="never">
        <div class="login-header">
          <h2>欢迎回来</h2>
          <p>请登录您的账号以继续</p>
        </div>

        <!-- 登录表单 -->
        <el-form v-if="loginMode === 'password'" ref="loginFormRef" :model="loginForm" :rules="loginRules" @submit.prevent="handlePasswordLogin">
          <el-form-item prop="username">
            <el-input v-model="loginForm.username" placeholder="用户名" size="large" prefix-icon="User" />
          </el-form-item>
          <el-form-item prop="password">
            <el-input v-model="loginForm.password" type="password" placeholder="密码" size="large" prefix-icon="Lock" show-password />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" size="large" :loading="loading" class="login-btn" native-type="submit">
              {{ loading ? '登录中...' : '登录' }}
            </el-button>
          </el-form-item>
        </el-form>

        <!-- 飞书登录 -->
        <div v-if="loginMode === 'feishu'" class="feishu-login">
          <div class="login-description">
            <p>请使用飞书账号登录以继续</p>
          </div>
          <el-button class="social-btn feishu" size="large" :loading="loading" @click="handleFeishuLogin">
            <img src="https://www.feishu.cn/favicon.ico" alt="Feishu" class="social-icon" />
            <span>{{ loading ? '登录中...' : '飞书登录' }}</span>
          </el-button>
        </div>

        <!-- 切换登录方式 -->
        <div class="login-switch">
          <el-divider>
            <span class="divider-text">或</span>
          </el-divider>
          <div class="switch-buttons">
            <el-button v-if="loginMode === 'feishu'" text type="primary" @click="loginMode = 'password'">
              <el-icon>
                <Key />
              </el-icon>
              用户名密码登录
            </el-button>
            <el-button v-if="loginMode === 'password'" text type="primary" @click="loginMode = 'feishu'">
              <img src="https://www.feishu.cn/favicon.ico" alt="Feishu" class="switch-icon" />
              飞书登录
            </el-button>
          </div>
        </div>

        <!-- 注册提示 -->
        <div class="login-tips">
          <el-alert v-if="loginMode === 'password'" title="首次使用？" description="如果您没有账号，请使用飞书登录进行注册" type="info" :closable="false" show-icon />
          <el-alert v-if="loginMode === 'feishu'" title="提示" description="首次登录将自动创建账号" type="info" :closable="false" show-icon />
        </div>
      </el-card>

      <div class="login-footer">
        <p>使用本系统即表示您同意我们的服务条款</p>
      </div>
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
import { List, Sunny, Moon, Key } from "@element-plus/icons-vue"
import { useThemeStore } from "../stores/theme"
import { useAuthStore } from "../stores/auth"
import {
  getOAuthUrl,
  getCurrentUser,
  buildFeishuCallbackUrl,
  generateState,
  passwordLogin,
  checkFeishuAuth,
} from "../api"
import type { LoginResponse } from "../types"

const router = useRouter()
const route = useRoute()
const themeStore = useThemeStore()
const authStore = useAuthStore()

const loading = ref(false)
const loginMode = ref<"password" | "feishu">("password")
const loginFormRef = ref<FormInstance>()

const loginForm = ref({
  username: "",
  password: "",
})

const loginRules: FormRules = {
  username: [{ required: true, message: "请输入用户名", trigger: "blur" }],
  password: [{ required: true, message: "请输入密码", trigger: "blur" }],
}

const isDark = computed(() => themeStore.isDark())

onMounted(() => {
  const code = route.query.code as string
  const state = route.query.state as string

  if (code) {
    handleFeishuCallback(code, state)
  }
})

const toggleTheme = () => {
  themeStore.toggleTheme()
}

const handlePasswordLogin = async () => {
  const valid = await loginFormRef.value?.validate()
  if (!valid) return

  loading.value = true

  try {
    const response = await passwordLogin({
      username: loginForm.value.username,
      password: loginForm.value.password,
    })

    if (!response.success || !response.data) {
      ElMessage.error(response.message || "登录失败")
      return
    }

    await handleLoginSuccess(response.data)
  } catch (error) {
    console.error("登录失败:", error)
    ElMessage.error("登录失败，请稍后重试")
  } finally {
    loading.value = false
  }
}

const handleFeishuLogin = async () => {
  loading.value = true

  try {
    const redirectUri = buildFeishuCallbackUrl()
    const state = generateState()

    sessionStorage.setItem("feishu_oauth_state", state)

    const response = await getOAuthUrl({ redirectUri, state })

    if (response.success && response.data) {
      window.location.href = response.data.url
    } else {
      ElMessage.error(response.message || "获取授权链接失败")
    }
  } catch (error) {
    console.error("获取飞书授权链接失败:", error)
    ElMessage.error("获取授权链接失败，请稍后重试")
  } finally {
    loading.value = false
  }
}

const handleFeishuCallback = async (code: string, _state?: string) => {
  loading.value = true

  try {
    const checkResponse = await checkFeishuAuth({ code, state: _state || "" })

    if (!checkResponse.success || !checkResponse.data) {
      ElMessage.error(checkResponse.message || "飞书授权失败")
      return
    }

    const { userExists, isFeishuBound, feishuUser, tempToken } =
      checkResponse.data

    if (userExists && isFeishuBound && tempToken) {
      authStore.setToken(tempToken)
      const userResponse = await getCurrentUser()
      if (userResponse.success && userResponse.data) {
        authStore.setUser({
          id: userResponse.data.id,
          feishuId: userResponse.data.feishuId,
          name: userResponse.data.name,
          email: userResponse.data.email,
          avatarUrl: userResponse.data.avatarUrl,
          role: userResponse.data.role,
          permissions: userResponse.data.permissions,
          createdAt: userResponse.data.createdAt,
          updatedAt: userResponse.data.createdAt,
        })
      }

      ElMessage.success({
        message: `欢迎回来，${feishuUser?.name || "用户"}！`,
        duration: 2000,
      })

      const redirect = route.query.redirect as string
      router.replace(redirect || "/tasks")
    } else if (feishuUser && tempToken) {
      sessionStorage.setItem("temp_token", tempToken)
      sessionStorage.setItem("feishu_user", JSON.stringify(feishuUser))

      router.replace({
        path: "/register",
        query: { from: "feishu" },
      })
    } else {
      ElMessage.error("无法获取用户信息")
    }
  } catch (error) {
    console.error("飞书登录失败:", error)
    ElMessage.error("登录失败，请稍后重试")
  } finally {
    loading.value = false
  }
}

const handleLoginSuccess = async (data: LoginResponse) => {
  authStore.setToken(data.accessToken)

  if (data.user) {
    authStore.setUser({
      id: parseInt(data.user.userId) || 0,
      feishuId: data.user.feishuId,
      name: data.user.userName,
      email: undefined,
      avatarUrl: undefined,
      role: data.user.role,
      permissions: data.user.permissions,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    })
  }

  localStorage.setItem(
    "permissions",
    JSON.stringify(data.user.permissions || [])
  )

  const welcomeMessage = data.isFirstLogin
    ? `欢迎首次使用，${data.user.userName}！请绑定飞书账号以获得完整功能。`
    : `欢迎回来，${data.user.userName}！`

  ElMessage.success({
    message: welcomeMessage,
    duration: 2000,
  })

  const redirect = route.query.redirect as string
  router.replace(redirect || "/tasks")
}
</script>

<style scoped>
.login-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
  overflow: hidden;
  background: linear-gradient(
    135deg,
    #f8fafc 0%,
    #e2e8f0 25%,
    #cbd5e1 50%,
    #94a3b8 75%,
    #64748b 100%
  );
  transition: all 0.5s ease;
}

.login-page.dark {
  background: linear-gradient(
    135deg,
    #1e293b 0%,
    #1e3a5f 25%,
    #1e40af 50%,
    #3730a3 75%,
    #312e81 100%
  );
}

.login-background {
  position: absolute;
  inset: 0;
  overflow: hidden;
  pointer-events: none;
}

.bg-circle {
  position: absolute;
  border-radius: 50%;
  filter: blur(80px);
  transition: all 0.5s ease;
}

.circle-1 {
  width: 600px;
  height: 600px;
  background: rgba(100, 116, 139, 0.15);
  top: -200px;
  right: -200px;
}

.login-page.dark .circle-1 {
  background: rgba(99, 102, 241, 0.12);
}

.circle-2 {
  width: 400px;
  height: 400px;
  background: rgba(148, 163, 184, 0.15);
  bottom: -100px;
  left: -100px;
}

.login-page.dark .circle-2 {
  background: rgba(139, 92, 246, 0.1);
}

.circle-3 {
  width: 300px;
  height: 300px;
  background: rgba(71, 85, 105, 0.12);
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
}

.login-page.dark .circle-3 {
  background: rgba(59, 130, 246, 0.08);
}

.bg-grid {
  position: absolute;
  inset: 0;
  background-image: linear-gradient(
      rgba(100, 116, 139, 0.08) 1px,
      transparent 1px
    ),
    linear-gradient(90deg, rgba(100, 116, 139, 0.08) 1px, transparent 1px);
  background-size: 50px 50px;
}

.login-page.dark .bg-grid {
  background-image: linear-gradient(
      rgba(99, 102, 241, 0.06) 1px,
      transparent 1px
    ),
    linear-gradient(90deg, rgba(99, 102, 241, 0.06) 1px, transparent 1px);
}

.nebula {
  position: absolute;
  width: 100%;
  height: 100%;
  background: radial-gradient(
      ellipse at 20% 80%,
      rgba(100, 116, 139, 0.12) 0%,
      transparent 50%
    ),
    radial-gradient(
      ellipse at 80% 20%,
      rgba(148, 163, 184, 0.1) 0%,
      transparent 50%
    ),
    radial-gradient(
      ellipse at 40% 40%,
      rgba(71, 85, 105, 0.08) 0%,
      transparent 50%
    );
  animation: nebula-move 20s ease-in-out infinite;
  opacity: 0.6;
}

.login-page.dark .nebula {
  background: radial-gradient(
      ellipse at 20% 80%,
      rgba(99, 102, 241, 0.15) 0%,
      transparent 50%
    ),
    radial-gradient(
      ellipse at 80% 20%,
      rgba(139, 92, 246, 0.12) 0%,
      transparent 50%
    ),
    radial-gradient(
      ellipse at 40% 40%,
      rgba(59, 130, 246, 0.1) 0%,
      transparent 50%
    );
  opacity: 0.8;
}

@keyframes nebula-move {
  0%,
  100% {
    transform: translate(0, 0) scale(1);
  }
  33% {
    transform: translate(30px, -30px) scale(1.1);
  }
  66% {
    transform: translate(-20px, 20px) scale(0.9);
  }
}

.floating-shapes-container {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  pointer-events: none;
  z-index: 0;
  overflow: hidden;
}

.floating-shape {
  position: absolute;
  border-radius: 50%;
  opacity: 0.15;
  animation: float-up linear infinite;
}

.shape-1 {
  width: 80px;
  height: 80px;
  background: linear-gradient(135deg, #94a3b8 0%, #64748b 100%);
  left: 10%;
  animation-duration: 20s;
  animation-delay: 0s;
}

.shape-2 {
  width: 60px;
  height: 60px;
  background: linear-gradient(135deg, #cbd5e1 0%, #94a3b8 100%);
  left: 20%;
  animation-duration: 25s;
  animation-delay: 2s;
  border-radius: 30% 70% 70% 30% / 30% 30% 70% 70%;
}

.shape-3 {
  width: 100px;
  height: 100px;
  background: linear-gradient(135deg, #64748b 0%, #475569 100%);
  left: 30%;
  animation-duration: 22s;
  animation-delay: 4s;
  border-radius: 50% 50% 30% 70% / 50% 70% 30% 50%;
}

.shape-4 {
  width: 50px;
  height: 50px;
  background: linear-gradient(135deg, #e2e8f0 0%, #cbd5e1 100%);
  left: 40%;
  animation-duration: 18s;
  animation-delay: 1s;
}

.shape-5 {
  width: 90px;
  height: 90px;
  background: linear-gradient(135deg, #94a3b8 0%, #64748b 100%);
  left: 50%;
  animation-duration: 24s;
  animation-delay: 3s;
  border-radius: 40% 60% 60% 40% / 60% 40% 60% 40%;
}

.shape-6 {
  width: 70px;
  height: 70px;
  background: linear-gradient(135deg, #cbd5e1 0%, #94a3b8 100%);
  left: 60%;
  animation-duration: 21s;
  animation-delay: 5s;
}

.shape-7 {
  width: 110px;
  height: 110px;
  background: linear-gradient(135deg, #64748b 0%, #475569 100%);
  left: 70%;
  animation-duration: 26s;
  animation-delay: 2.5s;
  border-radius: 60% 40% 40% 60% / 40% 60% 40% 60%;
}

.shape-8 {
  width: 55px;
  height: 55px;
  background: linear-gradient(135deg, #e2e8f0 0%, #cbd5e1 100%);
  left: 80%;
  animation-duration: 19s;
  animation-delay: 1.5s;
  border-radius: 30% 70% 70% 30% / 30% 30% 70% 70%;
}

.shape-9 {
  width: 85px;
  height: 85px;
  background: linear-gradient(135deg, #94a3b8 0%, #64748b 100%);
  left: 90%;
  animation-duration: 23s;
  animation-delay: 4.5s;
}

.shape-10 {
  width: 65px;
  height: 65px;
  background: linear-gradient(135deg, #cbd5e1 0%, #94a3b8 100%);
  left: 15%;
  animation-duration: 27s;
  animation-delay: 6s;
  border-radius: 50% 50% 30% 70% / 50% 70% 30% 50%;
}

.shape-11 {
  width: 95px;
  height: 95px;
  background: linear-gradient(135deg, #64748b 0%, #475569 100%);
  left: 45%;
  animation-duration: 20s;
  animation-delay: 3.5s;
  border-radius: 40% 60% 60% 40% / 60% 40% 60% 40%;
}

.shape-12 {
  width: 75px;
  height: 75px;
  background: linear-gradient(135deg, #e2e8f0 0%, #cbd5e1 100%);
  left: 75%;
  animation-duration: 22s;
  animation-delay: 0.5s;
  border-radius: 60% 40% 40% 60% / 40% 60% 40% 60%;
}

@keyframes float-up {
  0% {
    transform: translateY(100vh) rotate(0deg) scale(0.8);
    opacity: 0;
  }
  10% {
    opacity: 0.15;
  }
  90% {
    opacity: 0.15;
  }
  100% {
    transform: translateY(-100px) rotate(360deg) scale(1);
    opacity: 0;
  }
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
  box-shadow: 3vw 15vh rgba(255, 255, 255, 0.8),
    7vw 8vh rgba(255, 255, 255, 0.6), 12vw 23vh rgba(255, 255, 255, 0.7),
    18vw 5vh rgba(255, 255, 255, 0.5), 24vw 31vh rgba(255, 255, 255, 0.8),
    29vw 12vh rgba(255, 255, 255, 0.6), 35vw 28vh rgba(255, 255, 255, 0.7),
    41vw 3vh rgba(255, 255, 255, 0.5), 47vw 19vh rgba(255, 255, 255, 0.8),
    53vw 35vh rgba(255, 255, 255, 0.6), 58vw 7vh rgba(255, 255, 255, 0.7),
    64vw 25vh rgba(255, 255, 255, 0.5), 70vw 11vh rgba(255, 255, 255, 0.8),
    76vw 33vh rgba(255, 255, 255, 0.6), 82vw 6vh rgba(255, 255, 255, 0.7),
    88vw 22vh rgba(255, 255, 255, 0.5), 94vw 38vh rgba(255, 255, 255, 0.8),
    9vw 42vh rgba(255, 255, 255, 0.6), 15vw 58vh rgba(255, 255, 255, 0.7),
    21vw 45vh rgba(255, 255, 255, 0.5), 27vw 62vh rgba(255, 255, 255, 0.8),
    33vw 48vh rgba(255, 255, 255, 0.6), 39vw 67vh rgba(255, 255, 255, 0.7),
    45vw 52vh rgba(255, 255, 255, 0.5), 51vw 71vh rgba(255, 255, 255, 0.8),
    57vw 55vh rgba(255, 255, 255, 0.6), 63vw 74vh rgba(255, 255, 255, 0.7),
    69vw 59vh rgba(255, 255, 255, 0.5), 75vw 78vh rgba(255, 255, 255, 0.8),
    81vw 63vh rgba(255, 255, 255, 0.6), 87vw 82vh rgba(255, 255, 255, 0.7),
    93vw 68vh rgba(255, 255, 255, 0.5), 6vw 85vh rgba(255, 255, 255, 0.8),
    14vw 92vh rgba(255, 255, 255, 0.6), 22vw 77vh rgba(255, 255, 255, 0.7),
    30vw 88vh rgba(255, 255, 255, 0.5), 38vw 73vh rgba(255, 255, 255, 0.8),
    46vw 95vh rgba(255, 255, 255, 0.6), 54vw 80vh rgba(255, 255, 255, 0.7),
    62vw 97vh rgba(255, 255, 255, 0.5), 70vw 83vh rgba(255, 255, 255, 0.8),
    78vw 91vh rgba(255, 255, 255, 0.6), 86vw 76vh rgba(255, 255, 255, 0.7),
    94vw 87vh rgba(255, 255, 255, 0.5), 2vw 98vh rgba(255, 255, 255, 0.8);
  animation: animate-stars 50s linear infinite;
}

.stars2 {
  width: 2px;
  height: 2px;
  background: transparent;
  box-shadow: 4vw 12vh rgba(255, 255, 255, 0.9),
    9vw 28vh rgba(255, 255, 255, 0.7), 14vw 5vh rgba(255, 255, 255, 0.8),
    19vw 41vh rgba(255, 255, 255, 0.6), 26vw 18vh rgba(255, 255, 255, 0.9),
    32vw 55vh rgba(255, 255, 255, 0.7), 38vw 8vh rgba(255, 255, 255, 0.8),
    44vw 36vh rgba(255, 255, 255, 0.6), 50vw 62vh rgba(255, 255, 255, 0.9),
    56vw 15vh rgba(255, 255, 255, 0.7), 62vw 48vh rgba(255, 255, 255, 0.8),
    68vw 3vh rgba(255, 255, 255, 0.6), 74vw 70vh rgba(255, 255, 255, 0.9),
    80vw 25vh rgba(255, 255, 255, 0.7), 86vw 58vh rgba(255, 255, 255, 0.8),
    92vw 11vh rgba(255, 255, 255, 0.6), 98vw 44vh rgba(255, 255, 255, 0.9),
    3vw 67vh rgba(255, 255, 255, 0.7), 11vw 32vh rgba(255, 255, 255, 0.8),
    17vw 89vh rgba(255, 255, 255, 0.6), 24vw 52vh rgba(255, 255, 255, 0.9),
    30vw 7vh rgba(255, 255, 255, 0.7), 36vw 75vh rgba(255, 255, 255, 0.8),
    42vw 20vh rgba(255, 255, 255, 0.6), 49vw 83vh rgba(255, 255, 255, 0.9),
    55vw 38vh rgba(255, 255, 255, 0.7), 61vw 95vh rgba(255, 255, 255, 0.8),
    67vw 14vh rgba(255, 255, 255, 0.6), 73vw 61vh rgba(255, 255, 255, 0.9),
    79vw 30vh rgba(255, 255, 255, 0.7), 85vw 78vh rgba(255, 255, 255, 0.8),
    91vw 43vh rgba(255, 255, 255, 0.6), 97vw 6vh rgba(255, 255, 255, 0.9);
  animation: animate-stars 100s linear infinite;
}

.stars3 {
  width: 3px;
  height: 3px;
  background: transparent;
  box-shadow: 6vw 20vh rgba(255, 255, 255, 1),
    13vw 45vh rgba(255, 255, 255, 0.8), 21vw 8vh rgba(255, 255, 255, 0.9),
    28vw 72vh rgba(255, 255, 255, 0.7), 36vw 33vh rgba(255, 255, 255, 1),
    43vw 58vh rgba(255, 255, 255, 0.8), 51vw 15vh rgba(255, 255, 255, 0.9),
    58vw 88vh rgba(255, 255, 255, 0.7), 66vw 40vh rgba(255, 255, 255, 1),
    73vw 67vh rgba(255, 255, 255, 0.8), 81vw 3vh rgba(255, 255, 255, 0.9),
    88vw 50vh rgba(255, 255, 255, 0.7), 96vw 25vh rgba(255, 255, 255, 1),
    4vw 82vh rgba(255, 255, 255, 0.8), 11vw 55vh rgba(255, 255, 255, 0.9),
    19vw 12vh rgba(255, 255, 255, 0.7), 26vw 78vh rgba(255, 255, 255, 1),
    34vw 30vh rgba(255, 255, 255, 0.8), 41vw 95vh rgba(255, 255, 255, 0.9),
    49vw 7vh rgba(255, 255, 255, 0.7), 56vw 62vh rgba(255, 255, 255, 1),
    64vw 18vh rgba(255, 255, 255, 0.8), 71vw 85vh rgba(255, 255, 255, 0.9),
    79vw 43vh rgba(255, 255, 255, 0.7), 86vw 10vh rgba(255, 255, 255, 1),
    94vw 70vh rgba(255, 255, 255, 0.8), 2vw 37vh rgba(255, 255, 255, 0.9),
    9vw 98vh rgba(255, 255, 255, 0.7), 17vw 52vh rgba(255, 255, 255, 1),
    24vw 23vh rgba(255, 255, 255, 0.8), 32vw 75vh rgba(255, 255, 255, 0.9),
    39vw 5vh rgba(255, 255, 255, 0.7), 47vw 66vh rgba(255, 255, 255, 1),
    54vw 28vh rgba(255, 255, 255, 0.8), 62vw 91vh rgba(255, 255, 255, 0.9),
    69vw 48vh rgba(255, 255, 255, 0.7), 77vw 14vh rgba(255, 255, 255, 1),
    84vw 80vh rgba(255, 255, 255, 0.8), 92vw 35vh rgba(255, 255, 255, 0.9),
    7vw 60vh rgba(255, 255, 255, 0.7), 15vw 2vh rgba(255, 255, 255, 1),
    22vw 56vh rgba(255, 255, 255, 0.8), 30vw 87vh rgba(255, 255, 255, 0.9),
    38vw 21vh rgba(255, 255, 255, 0.7), 45vw 74vh rgba(255, 255, 255, 1),
    53vw 9vh rgba(255, 255, 255, 0.8), 61vw 42vh rgba(255, 255, 255, 0.9),
    68vw 97vh rgba(255, 255, 255, 0.7), 76vw 19vh rgba(255, 255, 255, 1),
    83vw 63vh rgba(255, 255, 255, 0.8), 91vw 32vh rgba(255, 255, 255, 0.9);
  animation: animate-stars 150s linear infinite;
}

.stars4 {
  width: 1px;
  height: 1px;
  background: transparent;
  box-shadow: 2vw 10vh rgba(255, 255, 255, 0.6),
    8vw 35vh rgba(255, 255, 255, 0.4), 15vw 60vh rgba(255, 255, 255, 0.5),
    22vw 85vh rgba(255, 255, 255, 0.3), 29vw 20vh rgba(255, 255, 255, 0.6),
    36vw 45vh rgba(255, 255, 255, 0.4), 43vw 70vh rgba(255, 255, 255, 0.5),
    50vw 95vh rgba(255, 255, 255, 0.3), 57vw 15vh rgba(255, 255, 255, 0.6),
    64vw 40vh rgba(255, 255, 255, 0.4), 71vw 65vh rgba(255, 255, 255, 0.5),
    78vw 90vh rgba(255, 255, 255, 0.3), 85vw 5vh rgba(255, 255, 255, 0.6),
    92vw 30vh rgba(255, 255, 255, 0.4), 99vw 55vh rgba(255, 255, 255, 0.5);
  animation: twinkle 3s ease-in-out infinite alternate;
}

@keyframes twinkle {
  0% {
    opacity: 0.3;
  }
  100% {
    opacity: 1;
  }
}

@keyframes animate-stars {
  from {
    transform: translateY(0);
  }
  to {
    transform: translateY(-100vh);
  }
}

.shooting-star {
  position: absolute;
  width: 4px;
  height: 4px;
  background: rgba(255, 255, 255, 0.9);
  border-radius: 50%;
  box-shadow: 0 0 0 4px rgba(255, 255, 255, 0.1),
    0 0 0 8px rgba(255, 255, 255, 0.05), 0 0 20px rgba(255, 255, 255, 0.8);
  opacity: 0;
}

.shooting-star::before {
  content: "";
  position: absolute;
  top: 50%;
  left: 0;
  transform: translateY(-50%);
  width: 300px;
  height: 1px;
  background: linear-gradient(90deg, rgba(255, 255, 255, 0.8), transparent);
}

.shooting-star.tail-reverse::before {
  left: auto;
  right: 0;
  background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.8));
}

.star-tr-bl {
  top: 10%;
  left: 90%;
  animation: shoot-tr-bl 4s linear infinite;
  animation-delay: 0s;
}

.star-tl-br {
  top: 5%;
  left: 10%;
  animation: shoot-tl-br 5s linear infinite;
  animation-delay: 1s;
}

.star-top-right {
  top: 0%;
  left: 70%;
  animation: shoot-top-right 3.5s linear infinite;
  animation-delay: 2s;
}

.star-left-top {
  top: 60%;
  left: 0%;
  animation: shoot-left-top 4.5s linear infinite;
  animation-delay: 0.5s;
}

.star-diagonal-1 {
  top: 15%;
  left: 5%;
  animation: shoot-diagonal-1 6s linear infinite;
  animation-delay: 3s;
}

.star-diagonal-2 {
  top: 80%;
  left: 85%;
  animation: shoot-diagonal-2 5.5s linear infinite;
  animation-delay: 1.5s;
}

@keyframes shoot-tr-bl {
  0% {
    transform: rotate(315deg) translateX(0);
    opacity: 1;
  }
  70% {
    opacity: 1;
  }
  100% {
    transform: rotate(315deg) translateX(-120vw);
    opacity: 0;
  }
}

@keyframes shoot-tl-br {
  0% {
    transform: rotate(45deg) translateX(0);
    opacity: 1;
  }
  70% {
    opacity: 1;
  }
  100% {
    transform: rotate(45deg) translateX(120vw);
    opacity: 0;
  }
}

@keyframes shoot-top-right {
  0% {
    transform: rotate(290deg) translateX(0);
    opacity: 1;
  }
  70% {
    opacity: 1;
  }
  100% {
    transform: rotate(290deg) translateX(-120vw);
    opacity: 0;
  }
}

@keyframes shoot-left-top {
  0% {
    transform: rotate(60deg) translateX(0);
    opacity: 1;
  }
  70% {
    opacity: 1;
  }
  100% {
    transform: rotate(60deg) translateX(120vw);
    opacity: 0;
  }
}

@keyframes shoot-diagonal-1 {
  0% {
    transform: rotate(30deg) translateX(0);
    opacity: 1;
  }
  70% {
    opacity: 1;
  }
  100% {
    transform: rotate(30deg) translateX(150vw);
    opacity: 0;
  }
}

@keyframes shoot-diagonal-2 {
  0% {
    transform: rotate(120deg) translateX(0);
    opacity: 1;
  }
  70% {
    opacity: 1;
  }
  100% {
    transform: rotate(120deg) translateX(-150vw);
    opacity: 0;
  }
}

.login-container {
  position: relative;
  z-index: 1;
  width: 100%;
  max-width: 420px;
  padding: 20px;
}

.login-brand {
  text-align: center;
  margin-bottom: 32px;
}

.brand-logo {
  width: 80px;
  height: 80px;
  margin: 0 auto 16px;
  background: linear-gradient(135deg, #64748b 0%, #475569 100%);
  backdrop-filter: blur(10px);
  border-radius: var(--radius-xl);
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fff;
  box-shadow: 0 8px 24px rgba(100, 116, 139, 0.2);
  border: 1px solid rgba(255, 255, 255, 0.2);
  transition: all 0.3s ease;
}

.login-page.dark .brand-logo {
  background: linear-gradient(135deg, #6366f1 0%, #4f46e5 100%);
  box-shadow: 0 8px 32px rgba(99, 102, 241, 0.25);
  border: 1px solid rgba(99, 102, 241, 0.3);
}

.brand-name {
  font-size: 32px;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 8px;
  letter-spacing: -0.5px;
  text-shadow: 0 2px 10px rgba(0, 0, 0, 0.05);
}

.login-page.dark .brand-name {
  color: #e0e7ff;
  text-shadow: 0 2px 20px rgba(99, 102, 241, 0.3);
}

.brand-slogan {
  font-size: 16px;
  color: #475569;
  margin: 0;
  text-shadow: 0 1px 5px rgba(0, 0, 0, 0.05);
}

.login-page.dark .brand-slogan {
  color: #cbd5e1;
}

.login-card {
  background: rgba(255, 255, 255, 0.95);
  backdrop-filter: blur(20px);
  border-radius: var(--radius-xl);
  border: 1px solid rgba(226, 232, 240, 0.8);
  box-shadow: 0 20px 60px rgba(71, 85, 105, 0.1),
    0 0 0 1px rgba(71, 85, 105, 0.05);
  transition: all 0.3s ease;
}

.login-page.dark .login-card {
  background: rgba(30, 41, 59, 0.95);
  border: 1px solid rgba(99, 102, 241, 0.2);
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3), 0 0 40px rgba(99, 102, 241, 0.1);
}

.login-header {
  text-align: center;
  margin-bottom: 24px;
}

.login-header h2 {
  font-size: 24px;
  font-weight: 600;
  color: #1e293b;
  margin: 0 0 8px;
  transition: color 0.3s ease;
}

.login-page.dark .login-header h2 {
  color: #e0e7ff;
}

.login-header p {
  font-size: 14px;
  color: #64748b;
  margin: 0;
  transition: color 0.3s ease;
}

.login-page.dark .login-header p {
  color: #94a3b8;
}

.login-btn {
  width: 100%;
  height: 44px;
  font-size: 16px;
  font-weight: 500;
  background: linear-gradient(135deg, #64748b 0%, #475569 100%);
  border: none;
  border-radius: var(--radius-lg);
  transition: all 0.3s ease;
}

.login-btn:hover {
  background: linear-gradient(135deg, #475569 0%, #334155 100%);
  transform: translateY(-1px);
  box-shadow: 0 4px 15px rgba(71, 85, 105, 0.3);
}

.login-page.dark .login-btn {
  background: linear-gradient(135deg, #6366f1 0%, #4f46e5 100%);
}

.login-page.dark .login-btn:hover {
  background: linear-gradient(135deg, #4f46e5 0%, #4338ca 100%);
  box-shadow: 0 4px 15px rgba(99, 102, 241, 0.4);
}

.login-switch {
  margin-top: 20px;
}

.divider-text {
  font-size: 12px;
  color: #94a3b8;
  padding: 0 16px;
  transition: color 0.3s ease;
}

.login-page.dark .divider-text {
  color: #64748b;
}

.switch-buttons {
  display: flex;
  justify-content: center;
  gap: 16px;
}

.switch-buttons .el-button {
  color: #6366f1;
  transition: color 0.3s ease;
}

.login-page.dark .switch-buttons .el-button {
  color: #a5b4fc;
}

.switch-buttons .el-button:hover {
  color: #4f46e5;
}

.login-page.dark .switch-buttons .el-button:hover {
  color: #c4b5fd;
}

.switch-icon {
  width: 16px;
  height: 16px;
  margin-right: 4px;
}

.login-tips {
  margin-top: 20px;
}

.login-tips :deep(.el-alert) {
  background: rgba(71, 85, 105, 0.08);
  border: 1px solid rgba(71, 85, 105, 0.15);
  transition: all 0.3s ease;
}

.login-page.dark .login-tips :deep(.el-alert) {
  background: rgba(99, 102, 241, 0.1);
  border: 1px solid rgba(99, 102, 241, 0.2);
}

.login-tips :deep(.el-alert__title) {
  color: #475569;
}

.login-page.dark .login-tips :deep(.el-alert__title) {
  color: #cbd5e1;
}

.login-tips :deep(.el-alert__description) {
  color: #64748b;
}

.login-page.dark .login-tips :deep(.el-alert__description) {
  color: #94a3b8;
}

.feishu-login {
  text-align: center;
}

.login-description {
  margin-bottom: 20px;
}

.login-description p {
  font-size: 14px;
  color: #6366f1;
  margin: 0;
  transition: color 0.3s ease;
}

.login-page.dark .login-description p {
  color: #a5b4fc;
}

.social-btn {
  width: 100%;
  height: 48px;
  font-size: 16px;
  font-weight: 500;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  transition: all 0.3s ease;
}

.social-btn.feishu {
  background: linear-gradient(135deg, #3370ff 0%, #2b5ce6 100%);
  border: none;
  color: white;
}

.social-btn.feishu:hover {
  background: linear-gradient(135deg, #2b5ce6 0%, #1f4cd4 100%);
  transform: translateY(-1px);
  box-shadow: 0 4px 15px rgba(51, 112, 255, 0.4);
}

.social-icon {
  width: 20px;
  height: 20px;
}

.login-footer {
  text-align: center;
  margin-top: 24px;
}

.login-footer p {
  font-size: 12px;
  color: #64748b;
  margin: 0;
  text-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
  transition: color 0.3s ease;
}

.login-page.dark .login-footer p {
  color: #94a3b8;
}

.theme-toggle {
  position: fixed;
  top: 24px;
  right: 24px;
  z-index: 100;
}

.theme-toggle .el-button {
  background: rgba(255, 255, 255, 0.9);
  backdrop-filter: blur(10px);
  border: 1px solid rgba(226, 232, 240, 0.8);
  color: #475569;
  transition: all 0.3s ease;
}

.theme-toggle .el-button:hover {
  background: rgba(255, 255, 255, 1);
  transform: scale(1.05);
  box-shadow: 0 4px 12px rgba(71, 85, 105, 0.15);
}

.login-page.dark .theme-toggle .el-button {
  background: rgba(30, 41, 59, 0.9);
  border: 1px solid rgba(99, 102, 241, 0.3);
  color: #e0e7ff;
}

.login-page.dark .theme-toggle .el-button:hover {
  background: rgba(30, 41, 59, 1);
  box-shadow: 0 4px 12px rgba(99, 102, 241, 0.2);
}
</style>
