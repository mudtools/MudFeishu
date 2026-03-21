<template>
  <div class="bind-feishu-page" :class="{ dark: isDark }">
    <div class="stars-container" v-show="isDark">
      <div class="stars"></div>
      <div class="stars2"></div>
      <div class="stars3"></div>
    </div>

    <div class="bind-background">
      <div class="bg-circle circle-1"></div>
      <div class="bg-circle circle-2"></div>
      <div class="bg-grid"></div>
    </div>

    <div class="bind-container">
      <div class="bind-brand">
        <div class="brand-logo">
          <el-icon :size="48">
            <Link />
          </el-icon>
        </div>
        <h1 class="brand-name">绑定飞书账号</h1>
        <p class="brand-slogan">绑定飞书账号以获得完整功能</p>
      </div>

      <el-card class="bind-card" shadow="never">
        <div class="bind-description">
          <el-alert
            title="需要绑定飞书账号"
            type="warning"
            :closable="false"
            show-icon
          >
            <template #default>
              <p>您当前使用的是本地账号登录，请绑定飞书账号以获得完整的任务管理功能。</p>
              <ul class="feature-list">
                <li>接收任务通知和提醒</li>
                <li>与团队成员协作</li>
                <li>同步飞书日历任务</li>
              </ul>
            </template>
          </el-alert>
        </div>

        <div class="bind-actions">
          <el-button
            class="feishu-btn"
            size="large"
            :loading="loading"
            @click="handleBindFeishu"
          >
            <img src="https://www.feishu.cn/favicon.ico" alt="Feishu" class="feishu-icon" />
            <span>{{ loading ? '正在跳转...' : '绑定飞书账号' }}</span>
          </el-button>
        </div>

        <div class="bind-tips">
          <el-divider>
            <span class="divider-text">或者</span>
          </el-divider>
          <el-button text type="primary" @click="skipBinding">
            暂时跳过，稍后绑定
          </el-button>
          <p class="skip-warning">跳过绑定将无法使用部分功能</p>
        </div>
      </el-card>
    </div>

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
import { ElMessage, ElMessageBox } from "element-plus"
import { Link, Sunny, Moon } from "@element-plus/icons-vue"
import { useThemeStore } from "../stores/theme"
import { getOAuthUrl, bindFeishu, buildFeishuCallbackUrl, generateState } from "../api"

const router = useRouter()
const route = useRoute()
const themeStore = useThemeStore()

const loading = ref(false)

const isDark = computed(() => themeStore.isDark())

onMounted(() => {
  const code = route.query.code as string
  const state = route.query.state as string

  if (code && state) {
    handleFeishuCallback(code, state)
  }
})

const toggleTheme = () => {
  themeStore.toggleTheme()
}

const handleBindFeishu = async () => {
  loading.value = true

  try {
    const redirectUri = buildFeishuCallbackUrl()
    const state = generateState()

    sessionStorage.setItem("feishu_bind_state", state)

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

const handleFeishuCallback = async (code: string, state: string) => {
  loading.value = true

  try {
    const storedState = sessionStorage.getItem("feishu_bind_state")
    if (storedState !== state) {
      ElMessage.error("State验证失败，请重新绑定")
      router.replace("/bind-feishu")
      return
    }

    sessionStorage.removeItem("feishu_bind_state")

    const response = await bindFeishu({ code, state })

    if (!response.success || !response.data?.success) {
      ElMessage.error(response.data?.message || response.message || "绑定失败")
      return
    }

    ElMessage.success({
      message: `飞书账号绑定成功！欢迎，${response.data.feishuName || "用户"}！`,
      duration: 2000,
    })

    router.replace("/tasks")
  } catch (error) {
    console.error("绑定飞书账号失败:", error)
    ElMessage.error("绑定失败，请稍后重试")
  } finally {
    loading.value = false
  }
}

const skipBinding = async () => {
  try {
    await ElMessageBox.confirm(
      "跳过绑定将无法使用任务通知、团队协作等功能。确定要跳过吗？",
      "提示",
      {
        confirmButtonText: "确定跳过",
        cancelButtonText: "取消",
        type: "warning",
      }
    )

    ElMessage.info("您可以稍后在个人设置中绑定飞书账号")
    router.replace("/tasks")
  } catch {
    // 用户取消
  }
}
</script>

<style scoped>
.bind-feishu-page {
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

.bind-feishu-page.dark {
  background: linear-gradient(
    135deg,
    #1e293b 0%,
    #1e3a5f 25%,
    #1e40af 50%,
    #3730a3 75%,
    #312e81 100%
  );
}

.bind-background {
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

.bind-feishu-page.dark .circle-1 {
  background: rgba(99, 102, 241, 0.12);
}

.circle-2 {
  width: 400px;
  height: 400px;
  background: rgba(148, 163, 184, 0.15);
  bottom: -100px;
  left: -100px;
}

.bind-feishu-page.dark .circle-2 {
  background: rgba(139, 92, 246, 0.1);
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

.bind-feishu-page.dark .bg-grid {
  background-image: linear-gradient(
      rgba(99, 102, 241, 0.06) 1px,
      transparent 1px
    ),
    linear-gradient(90deg, rgba(99, 102, 241, 0.06) 1px, transparent 1px);
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
    88vw 22vh rgba(255, 255, 255, 0.5), 94vw 38vh rgba(255, 255, 255, 0.8);
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
    92vw 11vh rgba(255, 255, 255, 0.6), 98vw 44vh rgba(255, 255, 255, 0.9);
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
    88vw 50vh rgba(255, 255, 255, 0.7), 96vw 25vh rgba(255, 255, 255, 1);
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

.bind-container {
  position: relative;
  z-index: 1;
  width: 100%;
  max-width: 440px;
  padding: 20px;
}

.bind-brand {
  text-align: center;
  margin-bottom: 32px;
}

.brand-logo {
  width: 80px;
  height: 80px;
  margin: 0 auto 16px;
  background: linear-gradient(135deg, #64748b 0%, #475569 100%);
  backdrop-filter: blur(10px);
  border-radius: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fff;
  box-shadow: 0 8px 24px rgba(100, 116, 139, 0.2);
  border: 1px solid rgba(255, 255, 255, 0.2);
  transition: all 0.3s ease;
}

.bind-feishu-page.dark .brand-logo {
  background: linear-gradient(135deg, #6366f1 0%, #4f46e5 100%);
  box-shadow: 0 8px 32px rgba(99, 102, 241, 0.25);
  border: 1px solid rgba(99, 102, 241, 0.3);
}

.brand-name {
  font-size: 28px;
  font-weight: 700;
  color: #1e293b;
  margin: 0 0 8px;
  letter-spacing: -0.5px;
  text-shadow: 0 2px 10px rgba(0, 0, 0, 0.05);
}

.bind-feishu-page.dark .brand-name {
  color: #e0e7ff;
  text-shadow: 0 2px 20px rgba(99, 102, 241, 0.3);
}

.brand-slogan {
  font-size: 14px;
  color: #475569;
  margin: 0;
  text-shadow: 0 1px 5px rgba(0, 0, 0, 0.05);
}

.bind-feishu-page.dark .brand-slogan {
  color: #cbd5e1;
}

.bind-card {
  background: rgba(255, 255, 255, 0.95);
  backdrop-filter: blur(20px);
  border-radius: 16px;
  border: 1px solid rgba(255, 255, 255, 0.2);
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.15);
}

.bind-feishu-page.dark .bind-card {
  background: rgba(15, 23, 42, 0.85);
  border: 1px solid rgba(99, 102, 241, 0.15);
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.4);
}

.bind-description {
  margin-bottom: 24px;
}

.feature-list {
  margin: 12px 0 0 0;
  padding-left: 20px;
  font-size: 13px;
  color: #64748b;
}

.bind-feishu-page.dark .feature-list {
  color: #94a3b8;
}

.feature-list li {
  margin: 4px 0;
}

.bind-actions {
  display: flex;
  justify-content: center;
}

.feishu-btn {
  width: 100%;
  height: 48px;
  font-size: 16px;
  font-weight: 500;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  background: linear-gradient(135deg, #3370ff 0%, #2b5ce6 100%);
  border: none;
  color: white;
}

.feishu-btn:hover {
  background: linear-gradient(135deg, #2b5ce6 0%, #1f3fd9 100%);
}

.feishu-icon {
  width: 20px;
  height: 20px;
}

.bind-tips {
  margin-top: 24px;
  text-align: center;
}

.divider-text {
  font-size: 12px;
  color: #94a3b8;
}

.bind-feishu-page.dark .divider-text {
  color: #64748b;
}

.skip-warning {
  font-size: 12px;
  color: #94a3b8;
  margin-top: 8px;
}

.bind-feishu-page.dark .skip-warning {
  color: #64748b;
}

.theme-toggle {
  position: fixed;
  top: 24px;
  right: 24px;
  z-index: 100;
}

.theme-toggle .el-button {
  background: rgba(100, 116, 139, 0.2);
  backdrop-filter: blur(10px);
  border: 1px solid rgba(100, 116, 139, 0.1);
  color: #475569;
}

.bind-feishu-page.dark .theme-toggle .el-button {
  background: rgba(255, 255, 255, 0.2);
  border: 1px solid rgba(255, 255, 255, 0.1);
  color: white;
}

.theme-toggle .el-button:hover {
  background: rgba(100, 116, 139, 0.3);
}

.bind-feishu-page.dark .theme-toggle .el-button:hover {
  background: rgba(255, 255, 255, 0.3);
}
</style>
