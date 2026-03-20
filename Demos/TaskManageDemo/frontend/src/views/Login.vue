<template>
  <div class="login-page" :class="{ dark: isDark }">
    <!-- 星空背景 -->
    <div class="stars-container">
      <div class="stars"></div>
      <div class="stars2"></div>
      <div class="stars3"></div>
      <div class="stars4"></div>
      <!-- 多方向流星 -->
      <!-- translateX为负(向左运动)的需要tail-reverse -->
      <div class="shooting-star star-tr-bl tail-reverse"></div>
      <!-- translateX为正(向右运动)的默认 -->
      <div class="shooting-star star-tl-br"></div>
      <!-- translateX为负(向左运动)的需要tail-reverse -->
      <div class="shooting-star star-top-right tail-reverse"></div>
      <!-- translateX为正(向右运动)的默认 -->
      <div class="shooting-star star-left-top"></div>
      <!-- translateX为正(向右运动)的默认 -->
      <div class="shooting-star star-diagonal-1"></div>
      <!-- translateX为负(向左运动)的需要tail-reverse -->
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
        <h1 class="brand-name">TaskFlow</h1>
        <p class="brand-slogan">高效协作，轻松管理</p>
      </div>

      <el-card class="login-card" shadow="never">
        <div class="login-header">
          <h2>欢迎回来</h2>
          <p>请登录您的账号以继续</p>
        </div>

        <el-form ref="formRef" :model="loginForm" :rules="rules" class="login-form" @keyup.enter="handleLogin">
          <el-form-item prop="username">
            <el-input v-model="loginForm.username" placeholder="用户名" size="large" class="login-input">
              <template #prefix>
                <el-icon>
                  <User />
                </el-icon>
              </template>
            </el-input>
          </el-form-item>

          <el-form-item prop="password">
            <el-input v-model="loginForm.password" type="password" placeholder="密码" size="large" class="login-input" show-password>
              <template #prefix>
                <el-icon>
                  <Lock />
                </el-icon>
              </template>
            </el-input>
          </el-form-item>

          <div class="login-options">
            <el-checkbox v-model="rememberMe">记住我</el-checkbox>
            <el-button link type="primary" size="small">忘记密码？</el-button>
          </div>

          <el-form-item>
            <el-button type="primary" size="large" :loading="loading" class="login-button" @click="handleLogin">
              <el-icon v-if="!loading">
                <ArrowRight />
              </el-icon>
              <span>登录</span>
            </el-button>
          </el-form-item>
        </el-form>

        <div class="login-divider">
          <span>或使用以下方式登录</span>
        </div>

        <div class="social-login">
          <el-button class="social-btn feishu" @click="handleFeishuLogin">
            <img src="https://www.feishu.cn/favicon.ico" alt="Feishu" class="social-icon" />
            <span>飞书登录</span>
          </el-button>
        </div>
      </el-card>

      <div class="login-footer">
        <p>还没有账号？ <el-button link type="primary">立即注册</el-button></p>
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
import { ref, reactive, computed } from "vue"
import { useRouter, useRoute } from "vue-router"
import { ElMessage } from "element-plus"
import type { FormInstance, FormRules } from "element-plus"
import {
  User,
  Lock,
  ArrowRight,
  List,
  Sunny,
  Moon,
} from "@element-plus/icons-vue"
import { useThemeStore } from "../stores/theme"

const router = useRouter()
const route = useRoute()
const themeStore = useThemeStore()

const formRef = ref<FormInstance>()
const loading = ref(false)
const rememberMe = ref(false)

const isDark = computed(() => themeStore.isDark())

const loginForm = reactive({
  username: "",
  password: "",
})

const rules: FormRules = {
  username: [
    { required: true, message: "请输入用户名", trigger: "blur" },
    { min: 3, message: "用户名至少3个字符", trigger: "blur" },
  ],
  password: [
    { required: true, message: "请输入密码", trigger: "blur" },
    { min: 6, message: "密码至少6个字符", trigger: "blur" },
  ],
}

const toggleTheme = () => {
  themeStore.toggleTheme()
}

const handleLogin = async () => {
  if (!formRef.value) return

  await formRef.value.validate(async (valid) => {
    if (!valid) return

    loading.value = true
    try {
      // TODO: 调用实际的登录API
      const mockToken = "mock-jwt-token-" + Date.now()
      localStorage.setItem("token", mockToken)
      localStorage.setItem("username", loginForm.username)

      if (rememberMe.value) {
        localStorage.setItem("rememberMe", "true")
      }

      ElMessage.success({
        message: "登录成功，欢迎回来！",
        duration: 2000,
      })

      const redirect = route.query.redirect as string
      router.push(redirect || "/tasks")
    } catch {
      ElMessage.error("登录失败，请检查用户名和密码")
    } finally {
      loading.value = false
    }
  })
}

const handleFeishuLogin = () => {
  ElMessage.info("飞书登录功能开发中...")
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
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  transition: background var(--transition-normal);
}

.login-page.dark {
  background: linear-gradient(135deg, #1e1b4b 0%, #312e81 100%);
}

/* 背景装饰 */
.login-background {
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

.circle-3 {
  width: 300px;
  height: 300px;
  background: #4facfe;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
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

/* 星云效果 */
.nebula {
  position: absolute;
  width: 100%;
  height: 100%;
  background: radial-gradient(
      ellipse at 20% 80%,
      rgba(120, 119, 198, 0.3) 0%,
      transparent 50%
    ),
    radial-gradient(
      ellipse at 80% 20%,
      rgba(255, 119, 198, 0.3) 0%,
      transparent 50%
    ),
    radial-gradient(
      ellipse at 40% 40%,
      rgba(99, 102, 241, 0.2) 0%,
      transparent 50%
    );
  animation: nebula-move 20s ease-in-out infinite;
}

@keyframes nebula-move {
  0%,
  100% {
    transform: translate(0, 0) scale(1);
    opacity: 0.5;
  }
  33% {
    transform: translate(30px, -30px) scale(1.1);
    opacity: 0.7;
  }
  66% {
    transform: translate(-20px, 20px) scale(0.9);
    opacity: 0.4;
  }
}

/* 星空背景 */
.stars-container {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  pointer-events: none;
  z-index: 0;
}

/* 星星动画 - 大量随机分布的小星星 */
.stars {
  width: 1px;
  height: 1px;
  background: transparent;
  box-shadow:
    /* 第一组 - 随机分布 */ 3vw 15vh #fff, 7vw 8vh #fff,
    12vw 23vh #fff, 18vw 5vh #fff, 24vw 31vh #fff, 29vw 12vh #fff,
    35vw 28vh #fff, 41vw 3vh #fff, 47vw 19vh #fff, 53vw 35vh #fff, 58vw 7vh #fff,
    64vw 25vh #fff, 70vw 11vh #fff, 76vw 33vh #fff, 82vw 6vh #fff,
    88vw 22vh #fff, 94vw 38vh #fff, 9vw 42vh #fff, 15vw 58vh #fff,
    21vw 45vh #fff, 27vw 62vh #fff, 33vw 48vh #fff, 39vw 67vh #fff,
    45vw 52vh #fff, 51vw 71vh #fff, 57vw 55vh #fff, 63vw 74vh #fff,
    69vw 59vh #fff, 75vw 78vh #fff, 81vw 63vh #fff, 87vw 82vh #fff,
    93vw 68vh #fff, 6vw 85vh #fff, 14vw 92vh #fff, 22vw 77vh #fff,
    30vw 88vh #fff, 38vw 73vh #fff, 46vw 95vh #fff, 54vw 80vh #fff,
    62vw 97vh #fff, 70vw 83vh #fff, 78vw 91vh #fff, 86vw 76vh #fff,
    94vw 87vh #fff, 2vw 98vh #fff, /* 第二组 - 更随机 */ 5vw 3vh #fff,
    11vw 18vh #fff, 17vw 36vh #fff, 23vw 9vh #fff, 29vw 44vh #fff,
    37vw 15vh #fff, 43vw 53vh #fff, 49vw 27vh #fff, 55vw 64vh #fff,
    61vw 41vh #fff, 67vw 8vh #fff, 73vw 56vh #fff, 79vw 29vh #fff,
    85vw 72vh #fff, 91vw 47vh #fff, 97vw 14vh #fff, 4vw 69vh #fff,
    10vw 34vh #fff, 16vw 89vh #fff, 26vw 50vh #fff, 32vw 17vh #fff,
    40vw 75vh #fff, 48vw 40vh #fff, 56vw 6vh #fff, 62vw 84vh #fff,
    68vw 37vh #fff, 74vw 93vh #fff, 80vw 20vh #fff, 86vw 60vh #fff,
    92vw 2vh #fff, 98vw 54vh #fff, 1vw 79vh #fff, 8vw 46vh #fff, 19vw 10vh #fff,
    25vw 66vh #fff, 31vw 30vh #fff, 44vw 86vh #fff, 50vw 13vh #fff,
    59vw 57vh #fff, 65vw 39vh #fff, 71vw 96vh #fff, 77vw 24vh #fff,
    83vw 70vh #fff, 89vw 49vh #fff, 95vw 4vh #fff,
    /* 第三组 - 填充 */ 2vw 26vh #fff, 13vw 61vh #fff, 20vw 94vh #fff,
    28vw 16vh #fff, 36vw 43vh #fff, 42vw 81vh #fff, 52vw 32vh #fff,
    60vw 7vh #fff, 66vw 54vh #fff, 72vw 21vh #fff, 78vw 90vh #fff,
    84vw 65vh #fff, 90vw 11vh #fff, 96vw 48vh #fff, 4vw 75vh #fff,
    12vw 51vh #fff, 21vw 2vh #fff, 27vw 38vh #fff, 34vw 84vh #fff,
    44vw 19vh #fff, 51vw 99vh #fff, 57vw 44vh #fff, 63vw 8vh #fff,
    69vw 62vh #fff, 76vw 28vh #fff, 82vw 95vh #fff, 88vw 53vh #fff,
    91vw 1vh #fff, 99vw 36vh #fff, 6vw 64vh #fff, 15vw 80vh #fff, 23vw 5vh #fff,
    31vw 72vh #fff, 39vw 25vh #fff, 48vw 57vh #fff, 54vw 12vh #fff,
    60vw 87vh #fff, 67vw 33vh #fff, 74vw 9vh #fff, 80vw 50vh #fff;
  animation: animate-stars 50s linear infinite;
}

/* 中等星星 */
.stars2 {
  width: 2px;
  height: 2px;
  background: transparent;
  box-shadow: 4vw 12vh #fff, 9vw 28vh #fff, 14vw 5vh #fff, 19vw 41vh #fff,
    26vw 18vh #fff, 32vw 55vh #fff, 38vw 8vh #fff, 44vw 36vh #fff,
    50vw 62vh #fff, 56vw 15vh #fff, 62vw 48vh #fff, 68vw 3vh #fff,
    74vw 70vh #fff, 80vw 25vh #fff, 86vw 58vh #fff, 92vw 11vh #fff,
    98vw 44vh #fff, 3vw 67vh #fff, 11vw 32vh #fff, 17vw 89vh #fff,
    24vw 52vh #fff, 30vw 7vh #fff, 36vw 75vh #fff, 42vw 20vh #fff,
    49vw 83vh #fff, 55vw 38vh #fff, 61vw 95vh #fff, 67vw 14vh #fff,
    73vw 61vh #fff, 79vw 30vh #fff, 85vw 78vh #fff, 91vw 43vh #fff,
    97vw 6vh #fff, 6vw 54vh #fff, 13vw 21vh #fff, 22vw 86vh #fff, 28vw 47vh #fff,
    35vw 2vh #fff, 41vw 69vh #fff, 47vw 34vh #fff, 53vw 97vh #fff,
    59vw 10vh #fff, 65vw 56vh #fff, 71vw 23vh #fff, 77vw 81vh #fff,
    83vw 40vh #fff, 89vw 5vh #fff, 95vw 72vh #fff, 2vw 37vh #fff, 8vw 94vh #fff,
    16vw 16vh #fff, 25vw 63vh #fff, 33vw 29vh #fff, 40vw 77vh #fff,
    46vw 1vh #fff, 52vw 50vh #fff, 58vw 85vh #fff, 64vw 22vh #fff,
    70vw 66vh #fff, 76vw 9vh #fff, 82vw 45vh #fff, 88vw 91vh #fff,
    94vw 26vh #fff, 7vw 74vh #fff, 15vw 49vh #fff, 23vw 4vh #fff, 31vw 59vh #fff,
    37vw 31vh #fff, 43vw 88vh #fff, 51vw 13vh #fff, 57vw 76vh #fff,
    63vw 35vh #fff, 69vw 8vh #fff, 75vw 53vh #fff, 81vw 99vh #fff,
    87vw 17vh #fff, 93vw 64vh #fff, 99vw 42vh #fff, 5vw 80vh #fff,
    12vw 24vh #fff, 20vw 71vh #fff, 28vw 46vh #fff, 34vw 93vh #fff,
    42vw 6vh #fff, 48vw 39vh #fff;
  animation: animate-stars 100s linear infinite;
}

/* 大星星 */
.stars3 {
  width: 3px;
  height: 3px;
  background: transparent;
  box-shadow: 6vw 20vh #fff, 13vw 45vh #fff, 21vw 8vh #fff, 28vw 72vh #fff,
    36vw 33vh #fff, 43vw 58vh #fff, 51vw 15vh #fff, 58vw 88vh #fff,
    66vw 40vh #fff, 73vw 67vh #fff, 81vw 3vh #fff, 88vw 50vh #fff,
    96vw 25vh #fff, 4vw 82vh #fff, 11vw 55vh #fff, 19vw 12vh #fff,
    26vw 78vh #fff, 34vw 30vh #fff, 41vw 95vh #fff, 49vw 7vh #fff,
    56vw 62vh #fff, 64vw 18vh #fff, 71vw 85vh #fff, 79vw 43vh #fff,
    86vw 10vh #fff, 94vw 70vh #fff, 2vw 37vh #fff, 9vw 98vh #fff, 17vw 52vh #fff,
    24vw 23vh #fff, 32vw 75vh #fff, 39vw 5vh #fff, 47vw 66vh #fff,
    54vw 28vh #fff, 62vw 91vh #fff, 69vw 48vh #fff, 77vw 14vh #fff,
    84vw 80vh #fff, 92vw 35vh #fff, 7vw 60vh #fff, 15vw 2vh #fff, 22vw 56vh #fff,
    30vw 87vh #fff, 38vw 21vh #fff, 45vw 74vh #fff, 53vw 9vh #fff,
    61vw 42vh #fff, 68vw 97vh #fff, 76vw 19vh #fff, 83vw 63vh #fff,
    91vw 32vh #fff, 5vw 84vh #fff, 14vw 47vh #fff, 23vw 11vh #fff,
    31vw 69vh #fff, 40vw 26vh #fff, 48vw 93vh #fff, 55vw 4vh #fff,
    63vw 54vh #fff, 72vw 16vh #fff, 80vw 77vh #fff, 89vw 38vh #fff,
    95vw 1vh #fff, 3vw 65vh #fff, 10vw 29vh #fff, 18vw 90vh #fff, 27vw 53vh #fff,
    35vw 6vh #fff, 44vw 79vh #fff, 52vw 22vh #fff, 60vw 46vh #fff,
    67vw 100vh #fff, 75vw 13vh #fff, 82vw 59vh #fff, 90vw 31vh #fff,
    98vw 86vh #fff, 1vw 41vh #fff, 8vw 96vh #fff, 16vw 17vh #fff, 25vw 73vh #fff;
  animation: animate-stars 150s linear infinite;
}

/* 闪烁的星星 */
.stars4 {
  width: 1px;
  height: 1px;
  background: transparent;
  box-shadow: 2vw 10vh #fff, 8vw 35vh #fff, 15vw 60vh #fff, 22vw 85vh #fff,
    29vw 20vh #fff, 36vw 45vh #fff, 43vw 70vh #fff, 50vw 95vh #fff,
    57vw 15vh #fff, 64vw 40vh #fff, 71vw 65vh #fff, 78vw 90vh #fff,
    85vw 5vh #fff, 92vw 30vh #fff, 99vw 55vh #fff, 5vw 80vh #fff, 12vw 25vh #fff,
    19vw 50vh #fff, 26vw 75vh #fff, 33vw 100vh #fff, 40vw 12vh #fff,
    47vw 37vh #fff, 54vw 62vh #fff, 61vw 87vh #fff, 68vw 8vh #fff,
    75vw 33vh #fff, 82vw 58vh #fff, 89vw 83vh #fff, 96vw 18vh #fff,
    3vw 43vh #fff, 10vw 68vh #fff, 17vw 93vh #fff, 24vw 3vh #fff, 31vw 28vh #fff,
    38vw 53vh #fff, 45vw 78vh #fff, 52vw 100vh #fff, 59vw 22vh #fff,
    66vw 47vh #fff, 73vw 72vh #fff, 80vw 97vh #fff, 87vw 14vh #fff,
    94vw 39vh #fff, 7vw 64vh #fff, 14vw 89vh #fff, 21vw 6vh #fff, 28vw 31vh #fff,
    35vw 56vh #fff, 42vw 81vh #fff, 49vw 100vh #fff, 56vw 17vh #fff,
    63vw 42vh #fff, 70vw 67vh #fff, 77vw 92vh #fff, 84vw 11vh #fff,
    91vw 36vh #fff, 98vw 61vh #fff, 4vw 86vh #fff, 13vw 1vh #fff, 20vw 26vh #fff,
    27vw 51vh #fff, 34vw 76vh #fff, 41vw 100vh #fff, 48vw 19vh #fff,
    55vw 44vh #fff, 62vw 69vh #fff, 69vw 94vh #fff, 76vw 9vh #fff,
    83vw 34vh #fff, 90vw 59vh #fff, 97vw 84vh #fff, 6vw 100vh #fff,
    11vw 23vh #fff, 18vw 48vh #fff, 25vw 73vh #fff, 32vw 98vh #fff,
    39vw 13vh #fff, 46vw 38vh #fff, 53vw 63vh #fff, 60vw 88vh #fff,
    67vw 4vh #fff, 74vw 29vh #fff, 81vw 54vh #fff, 88vw 79vh #fff,
    95vw 100vh #fff;
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

/* 闪烁效果 */
.stars::after,
.stars2::after,
.stars3::after {
  content: " ";
  position: absolute;
  top: 100vh;
  width: inherit;
  height: inherit;
  background: inherit;
  box-shadow: inherit;
}

/* 流星效果 - 多方向 */
.shooting-star {
  position: absolute;
  width: 4px;
  height: 4px;
  background: #fff;
  border-radius: 50%;
  box-shadow: 0 0 0 4px rgba(255, 255, 255, 0.1),
    0 0 0 8px rgba(255, 255, 255, 0.1), 0 0 20px rgba(255, 255, 255, 1);
  opacity: 0;
}

/* 流星尾迹 - 使用旋转后的坐标系，尾迹始终指向运动的反方向 */
.shooting-star::before {
  content: "";
  position: absolute;
  top: 50%;
  left: 0;
  transform: translateY(-50%);
  width: 300px;
  height: 1px;
  background: linear-gradient(90deg, #fff, transparent);
}

/* 向左运动的流星需要反转尾迹方向 */
.shooting-star.tail-reverse::before {
  left: auto;
  right: 0;
  background: linear-gradient(90deg, transparent, #fff);
}

/* 右上到左下 (原角度 315deg) */
.star-tr-bl {
  top: 10%;
  left: 90%;
  animation: shoot-tr-bl 4s linear infinite;
  animation-delay: 0s;
}

/* 左上到右下 (角度 45deg) */
.star-tl-br {
  top: 5%;
  left: 10%;
  animation: shoot-tl-br 5s linear infinite;
  animation-delay: 1s;
}

/* 顶部偏右到左下 (角度 290deg) */
.star-top-right {
  top: 0%;
  left: 70%;
  animation: shoot-top-right 3.5s linear infinite;
  animation-delay: 2s;
}

/* 左侧到右上 (角度 60deg) */
.star-left-top {
  top: 60%;
  left: 0%;
  animation: shoot-left-top 4.5s linear infinite;
  animation-delay: 0.5s;
}

/* 对角线1 (角度 30deg) */
.star-diagonal-1 {
  top: 15%;
  left: 5%;
  animation: shoot-diagonal-1 6s linear infinite;
  animation-delay: 3s;
}

/* 对角线2 (角度 120deg) */
.star-diagonal-2 {
  top: 80%;
  left: 85%;
  animation: shoot-diagonal-2 5.5s linear infinite;
  animation-delay: 1.5s;
}

/* 右上到左下 */
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

/* 左上到右下 */
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

/* 顶部到左下 */
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

/* 左侧到右上 */
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

/* 对角线1 - 浅角度 */
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

/* 对角线2 - 陡峭角度 */
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

/* 登录容器 */
.login-container {
  position: relative;
  z-index: 1;
  width: 100%;
  max-width: 420px;
  padding: 20px;
}

/* 品牌区域 */
.login-brand {
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

/* 登录卡片 */
.login-card {
  background: rgba(255, 255, 255, 0.95);
  backdrop-filter: blur(20px);
  border: 1px solid rgba(255, 255, 255, 0.2);
  border-radius: var(--radius-xl);
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
}

.dark .login-card {
  background: rgba(31, 41, 55, 0.95);
  border-color: rgba(255, 255, 255, 0.1);
}

.login-card :deep(.el-card__body) {
  padding: 32px;
}

.login-header {
  text-align: center;
  margin-bottom: 28px;
}

.login-header h2 {
  font-size: 24px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0 0 8px;
}

.login-header p {
  font-size: 14px;
  color: var(--text-secondary);
  margin: 0;
}

/* 登录表单 */
.login-form {
  margin-bottom: 24px;
}

.login-input :deep(.el-input__wrapper) {
  border-radius: var(--radius-lg);
  padding: 4px 16px;
  box-shadow: 0 1px 2px 0 rgba(0, 0, 0, 0.05);
  border: 1px solid var(--border-color);
  transition: all var(--transition-fast);
}

.login-input :deep(.el-input__wrapper:hover) {
  border-color: var(--primary-color);
}

.login-input :deep(.el-input__wrapper.is-focus) {
  border-color: var(--primary-color);
  box-shadow: 0 0 0 3px var(--primary-bg);
}

.login-input :deep(.el-input__inner) {
  height: 44px;
  font-size: 15px;
}

.login-input :deep(.el-input__prefix) {
  color: var(--text-muted);
  margin-right: 10px;
}

.login-options {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin: -8px 0 20px;
}

.login-button {
  width: 100%;
  height: 48px;
  border-radius: var(--radius-lg);
  font-size: 16px;
  font-weight: 500;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  background: linear-gradient(
    135deg,
    var(--primary-color) 0%,
    var(--primary-light) 100%
  );
  border: none;
  box-shadow: 0 4px 14px 0 rgba(79, 70, 229, 0.39);
  transition: all var(--transition-fast);
}

.login-button:hover {
  transform: translateY(-2px);
  box-shadow: 0 6px 20px rgba(79, 70, 229, 0.23);
}

.login-button:active {
  transform: translateY(0);
}

/* 分隔线 */
.login-divider {
  position: relative;
  text-align: center;
  margin: 24px 0;
}

.login-divider::before {
  content: "";
  position: absolute;
  top: 50%;
  left: 0;
  right: 0;
  height: 1px;
  background: var(--border-color);
}

.login-divider span {
  position: relative;
  background: var(--bg-card);
  padding: 0 16px;
  font-size: 13px;
  color: var(--text-muted);
}

/* 社交登录 */
.social-login {
  display: flex;
  justify-content: center;
}

.social-btn {
  width: 100%;
  height: 44px;
  border-radius: var(--radius-lg);
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 10px;
  border: 1px solid var(--border-color);
  background: var(--bg-secondary);
  transition: all var(--transition-fast);
}

.social-btn:hover {
  background: var(--bg-tertiary);
  border-color: var(--border-dark);
}

.social-icon {
  width: 20px;
  height: 20px;
}

/* 登录页脚 */
.login-footer {
  text-align: center;
  margin-top: 24px;
  color: rgba(255, 255, 255, 0.8);
  font-size: 14px;
}

.login-footer :deep(.el-button) {
  color: #fff;
  font-weight: 500;
}

/* 主题切换 */
.theme-toggle {
  position: fixed;
  top: 20px;
  right: 20px;
  z-index: 10;
}

.theme-toggle :deep(.el-button) {
  background: rgba(255, 255, 255, 0.2);
  backdrop-filter: blur(10px);
  border: none;
  color: #fff;
  width: 44px;
  height: 44px;
  font-size: 20px;
}

.theme-toggle :deep(.el-button:hover) {
  background: rgba(255, 255, 255, 0.3);
}

/* 响应式 */
@media (max-width: 480px) {
  .login-container {
    padding: 16px;
  }

  .login-card :deep(.el-card__body) {
    padding: 24px;
  }

  .brand-logo {
    width: 64px;
    height: 64px;
  }

  .brand-name {
    font-size: 28px;
  }
}
</style>
