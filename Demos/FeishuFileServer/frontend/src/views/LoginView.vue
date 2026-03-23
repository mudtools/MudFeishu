<template>
  <div class="login-container">
    <!-- 左侧品牌区域 (仅桌面端显示) -->
    <div class="brand-section">
      <div class="brand-content">
        <div class="brand-logo">
          <IconLogo :size="80" />
        </div>
        <h1 class="brand-title">飞书云文件管理系统</h1>
        <p class="brand-subtitle">安全 · 高效 · 智能</p>
        
        <div class="features">
          <div class="feature-item">
            <div class="feature-icon">
              <IconUpload :size="24" />
            </div>
            <div class="feature-text">
              <h3>快速上传</h3>
              <p>支持大文件分片上传，断点续传</p>
            </div>
          </div>
          <div class="feature-item">
            <div class="feature-icon">
              <IconSync :size="24" />
            </div>
            <div class="feature-text">
              <h3>云端同步</h3>
              <p>一键同步飞书云盘文件和文件夹</p>
            </div>
          </div>
          <div class="feature-item">
            <div class="feature-icon">
              <IconFolder :size="24" />
            </div>
            <div class="feature-text">
              <h3>版本管理</h3>
              <p>完整的文件版本历史记录</p>
            </div>
          </div>
        </div>
      </div>
      
      <!-- 装饰元素 -->
      <div class="decorations">
        <div class="deco-circle circle-1"></div>
        <div class="deco-circle circle-2"></div>
        <div class="deco-circle circle-3"></div>
      </div>
    </div>
    
    <!-- 右侧表单区域 -->
    <div class="form-section">
      <!-- 移动端 Logo -->
      <div class="mobile-logo">
        <IconLogo :size="48" />
        <h1>飞书云文件管理系统</h1>
      </div>
      
      <div class="form-container">
        <div class="form-header">
          <h2>{{ isLogin ? '欢迎回来' : '创建账户' }}</h2>
          <p>{{ isLogin ? '请登录您的账户继续' : '填写以下信息完成注册' }}</p>
        </div>

        <el-form
          ref="formRef"
          :model="form"
          :rules="rules"
          class="login-form"
          @keyup.enter="handleSubmit"
        >
          <el-form-item prop="username">
            <div class="input-group">
              <IconUser class="input-icon" :size="20" />
              <el-input
                v-model="form.username"
                placeholder="用户名"
                size="large"
                class="modern-input"
              />
            </div>
          </el-form-item>

          <el-form-item prop="password">
            <div class="input-group">
              <IconLock class="input-icon" :size="20" />
              <el-input
                v-model="form.password"
                type="password"
                placeholder="密码"
                size="large"
                show-password
                class="modern-input"
              />
            </div>
          </el-form-item>

          <template v-if="!isLogin">
            <el-form-item prop="confirmPassword">
              <div class="input-group">
                <IconLock class="input-icon" :size="20" />
                <el-input
                  v-model="form.confirmPassword"
                  type="password"
                  placeholder="确认密码"
                  size="large"
                  show-password
                  class="modern-input"
                />
              </div>
            </el-form-item>

            <el-form-item prop="email">
              <div class="input-group">
                <IconMail class="input-icon" :size="20" />
                <el-input
                  v-model="form.email"
                  placeholder="邮箱（可选）"
                  size="large"
                  class="modern-input"
                />
              </div>
            </el-form-item>

            <el-form-item prop="displayName">
              <div class="input-group">
                <IconUser class="input-icon" :size="20" />
                <el-input
                  v-model="form.displayName"
                  placeholder="显示名称（可选）"
                  size="large"
                  class="modern-input"
                />
              </div>
            </el-form-item>
          </template>

          <el-form-item class="submit-item">
            <button 
              type="button" 
              class="submit-btn"
              :disabled="authStore.loading"
              @click="handleSubmit"
            >
              <transition name="fade" mode="out-in">
                <span v-if="!authStore.loading" class="btn-content" key="normal">
                  <span>{{ isLogin ? '登录' : '注册' }}</span>
                  <svg class="btn-arrow" viewBox="0 0 24 24" width="20" height="20">
                    <path d="M5 12H19M19 12L12 5M19 12L12 19" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" fill="none"/>
                  </svg>
                </span>
                <span v-else class="loading-content" key="loading">
                  <IconLoading :size="20" />
                  <span>处理中...</span>
                </span>
              </transition>
            </button>
          </el-form-item>
        </el-form>

        <div class="form-footer">
          <p class="switch-mode">
            {{ isLogin ? '还没有账号？' : '已有账号？' }}
            <a class="switch-link" @click="toggleMode">
              {{ isLogin ? '立即注册' : '立即登录' }}
            </a>
          </p>
        </div>
      </div>
      
      <!-- 主题切换 -->
      <button class="theme-toggle" @click="appStore.toggleTheme" :title="appStore.isDark ? '切换到亮色模式' : '切换到暗色模式'">
        <transition name="theme-icon" mode="out-in">
          <IconSun v-if="appStore.isDark" :size="20" key="sun" />
          <IconMoon v-else :size="20" key="moon" />
        </transition>
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import type { FormInstance, FormRules } from 'element-plus'
import { useAuthStore } from '@/stores/authStore'
import { useAppStore } from '@/stores/appStore'
import { 
  IconLogo, 
  IconUser, 
  IconLock, 
  IconMail, 
  IconLoading, 
  IconMoon, 
  IconSun,
  IconUpload,
  IconSync,
  IconFolder
} from '@/components/icons'

const router = useRouter()
const authStore = useAuthStore()
const appStore = useAppStore()
const formRef = ref<FormInstance>()
const isLogin = ref(true)

const form = reactive({
  username: '',
  password: '',
  confirmPassword: '',
  email: '',
  displayName: ''
})

const validateConfirmPassword = (_rule: any, value: string, callback: any) => {
  if (!isLogin.value && value !== form.password) {
    callback(new Error('两次输入的密码不一致'))
  } else {
    callback()
  }
}

const rules: FormRules = {
  username: [
    { required: true, message: '请输入用户名', trigger: 'blur' },
    { min: 3, max: 50, message: '用户名长度应为3-50个字符', trigger: 'blur' }
  ],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, max: 100, message: '密码长度至少为6个字符', trigger: 'blur' }
  ],
  confirmPassword: [
    { required: !isLogin.value, message: '请确认密码', trigger: 'blur' },
    { validator: validateConfirmPassword, trigger: 'blur' }
  ],
  email: [
    { type: 'email', message: '请输入有效的邮箱地址', trigger: 'blur' }
  ]
}

const toggleMode = () => {
  isLogin.value = !isLogin.value
  formRef.value?.resetFields()
}

const handleSubmit = async () => {
  if (!formRef.value) return

  await formRef.value.validate(async (valid) => {
    if (valid) {
      let success = false
      if (isLogin.value) {
        success = await authStore.login({
          username: form.username,
          password: form.password
        })
      } else {
        success = await authStore.register({
          username: form.username,
          password: form.password,
          email: form.email || undefined,
          displayName: form.displayName || undefined
        })
      }

      if (success) {
        router.push('/')
      }
    }
  })
}
</script>

<style scoped lang="scss">
.login-container {
  min-height: 100vh;
  display: flex;
  background: var(--bg-color);
  position: relative;
  overflow: hidden;
}

// 左侧品牌区域
.brand-section {
  flex: 1;
  display: none;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  padding: 60px;
  position: relative;
  background: linear-gradient(135deg, var(--primary-color) 0%, #8b5cf6 50%, #ec4899 100%);
  color: white;
  overflow: hidden;
  transition: background 0.3s ease;

  @media (min-width: 1024px) {
    display: flex;
  }
}

.brand-content {
  position: relative;
  z-index: 2;
  text-align: center;
  max-width: 480px;
}

.brand-logo {
  margin-bottom: 24px;
  color: white;
  filter: drop-shadow(0 4px 20px rgba(255, 255, 255, 0.3));
  animation: float 6s ease-in-out infinite;
}

@keyframes float {
  0%, 100% { transform: translateY(0); }
  50% { transform: translateY(-10px); }
}

.brand-title {
  font-size: 32px;
  font-weight: 700;
  margin-bottom: 8px;
  letter-spacing: -0.5px;
}

.brand-subtitle {
  font-size: 18px;
  opacity: 0.9;
  margin-bottom: 48px;
  letter-spacing: 2px;
}

.features {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.feature-item {
  display: flex;
  align-items: center;
  gap: 16px;
  background: rgba(255, 255, 255, 0.1);
  backdrop-filter: blur(10px);
  padding: 16px 20px;
  border-radius: 16px;
  transition: all 0.3s ease;
  border: 1px solid rgba(255, 255, 255, 0.15);

  &:hover {
    background: rgba(255, 255, 255, 0.2);
    transform: translateX(8px);
  }
}

.feature-icon {
  width: 48px;
  height: 48px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(255, 255, 255, 0.2);
  border-radius: 12px;
  flex-shrink: 0;
}

.feature-text {
  text-align: left;

  h3 {
    font-size: 16px;
    font-weight: 600;
    margin-bottom: 4px;
  }

  p {
    font-size: 14px;
    opacity: 0.85;
  }
}

// 装饰圆圈
.decorations {
  position: absolute;
  inset: 0;
  pointer-events: none;
  overflow: hidden;
}

.deco-circle {
  position: absolute;
  border-radius: 50%;
  border: 1px solid rgba(255, 255, 255, 0.1);

  &.circle-1 {
    width: 400px;
    height: 400px;
    top: -100px;
    right: -100px;
    animation: pulse 8s ease-in-out infinite;
  }

  &.circle-2 {
    width: 300px;
    height: 300px;
    bottom: -50px;
    left: -50px;
    animation: pulse 8s ease-in-out infinite 2s;
  }

  &.circle-3 {
    width: 200px;
    height: 200px;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    animation: pulse 8s ease-in-out infinite 4s;
  }
}

@keyframes pulse {
  0%, 100% { opacity: 0.3; transform: scale(1); }
  50% { opacity: 0.6; transform: scale(1.05); }
}

// 右侧表单区域
.form-section {
  width: 100%;
  max-width: 520px;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  padding: 40px 24px;
  position: relative;
  background: var(--bg-color);
  transition: background 0.3s ease;

  @media (min-width: 768px) {
    padding: 60px;
  }

  @media (min-width: 1024px) {
    flex: 0 0 520px;
    max-width: none;
  }
}

.mobile-logo {
  display: flex;
  flex-direction: column;
  align-items: center;
  margin-bottom: 32px;
  color: var(--primary-color);

  @media (min-width: 1024px) {
    display: none;
  }

  h1 {
    font-size: 20px;
    font-weight: 600;
    margin-top: 12px;
    color: var(--text-primary);
  }
}

.form-container {
  width: 100%;
  max-width: 400px;
}

.form-header {
  margin-bottom: 32px;
  text-align: center;

  @media (min-width: 1024px) {
    text-align: left;
  }

  h2 {
    font-size: 28px;
    font-weight: 700;
    color: var(--text-primary);
    margin-bottom: 8px;
  }

  p {
    font-size: 15px;
    color: var(--text-secondary);
  }
}

.login-form {
  :deep(.el-form-item) {
    margin-bottom: 20px;
  }
}

.input-group {
  position: relative;
  width: 100%;

  .input-icon {
    position: absolute;
    left: 16px;
    top: 50%;
    transform: translateY(-50%);
    color: var(--text-tertiary);
    z-index: 1;
    transition: color 0.2s ease;
    pointer-events: none;
  }

  &:focus-within .input-icon {
    color: var(--primary-color);
  }
}

.modern-input {
  :deep(.el-input__wrapper) {
    background: var(--bg-secondary);
    border: 2px solid var(--border-color);
    border-radius: 12px;
    padding: 0 16px;
    padding-left: 48px;
    height: 52px;
    box-shadow: none;
    transition: all 0.2s ease;
    
    &:hover {
      border-color: var(--text-tertiary);
    }
    
    &.is-focus {
      border-color: var(--primary-color);
      box-shadow: 0 0 0 4px rgba(99, 102, 241, 0.1);
    }
  }
  
  :deep(.el-input__inner) {
    font-size: 15px;
    color: var(--text-primary);
    
    &::placeholder {
      color: var(--text-tertiary);
    }
  }

  :deep(.el-input__suffix) {
    right: 12px;
  }
}

.submit-item {
  margin-top: 32px;
  margin-bottom: 0;
}

.submit-btn {
  width: 100%;
  height: 52px;
  font-size: 16px;
  font-weight: 600;
  border: none;
  border-radius: 12px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  background: linear-gradient(135deg, var(--primary-color) 0%, #8b5cf6 100%);
  color: white;
  transition: all 0.3s ease;
  position: relative;
  overflow: hidden;

  &::before {
    content: '';
    position: absolute;
    inset: 0;
    background: linear-gradient(135deg, #8b5cf6 0%, #ec4899 100%);
    opacity: 0;
    transition: opacity 0.3s ease;
  }

  &:hover:not(:disabled) {
    transform: translateY(-2px);
    box-shadow: 0 8px 25px rgba(99, 102, 241, 0.4);

    &::before {
      opacity: 1;
    }

    .btn-arrow {
      transform: translateX(4px);
    }
  }
  
  &:active:not(:disabled) {
    transform: translateY(0);
  }
  
  &:disabled {
    opacity: 0.7;
    cursor: not-allowed;
  }

  .btn-content, .loading-content {
    position: relative;
    z-index: 1;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
  }

  .btn-arrow {
    transition: transform 0.2s ease;
  }

  .loading-content {
    svg {
      color: white;
    }
  }
}

.form-footer {
  margin-top: 24px;
  text-align: center;
}

.switch-mode {
  font-size: 14px;
  color: var(--text-secondary);
}

.switch-link {
  color: var(--primary-color);
  font-weight: 500;
  cursor: pointer;
  margin-left: 4px;
  transition: all 0.2s ease;

  &:hover {
    text-decoration: underline;
  }
}

// 主题切换按钮
.theme-toggle {
  position: absolute;
  top: 24px;
  right: 24px;
  width: 44px;
  height: 44px;
  border: none;
  background: var(--bg-secondary);
  border-radius: 12px;
  color: var(--text-secondary);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s ease;
  border: 1px solid var(--border-color);

  &:hover {
    background: var(--primary-bg);
    color: var(--primary-color);
    border-color: var(--primary-color);
  }
}

// 过渡动画
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

.theme-icon-enter-active,
.theme-icon-leave-active {
  transition: all 0.3s ease;
}

.theme-icon-enter-from {
  opacity: 0;
  transform: rotate(-90deg) scale(0.5);
}

.theme-icon-leave-to {
  opacity: 0;
  transform: rotate(90deg) scale(0.5);
}
</style>

<!-- 非 scoped 样式用于暗色模式 -->
<style lang="scss">
// 暗色模式下的左侧品牌区域
[data-theme="dark"] .brand-section {
  background: linear-gradient(135deg, #4c1d95 0%, #6b21a8 30%, #7c3aed 60%, #8b5cf6 100%);
}

// 暗色模式下的表单区域
[data-theme="dark"] .form-section {
  background: linear-gradient(135deg, #1a1a2e 0%, #16213e 50%, #0f172a 100%);
}

[data-theme="dark"] .form-section::before {
  content: '';
  position: absolute;
  left: 0;
  top: 0;
  bottom: 0;
  width: 120px;
  background: linear-gradient(90deg, rgba(139, 92, 246, 0.15) 0%, transparent 100%);
  pointer-events: none;
}

// 暗色模式下的移动端logo发光效果
[data-theme="dark"] .mobile-logo {
  filter: drop-shadow(0 0 20px rgba(139, 92, 246, 0.3));
}

// 暗色模式下的输入框样式
[data-theme="dark"] .modern-input .el-input__wrapper {
  background: rgba(30, 41, 59, 0.6);
  border-color: rgba(139, 92, 246, 0.2);
  backdrop-filter: blur(8px);
}

[data-theme="dark"] .modern-input .el-input__wrapper:hover {
  border-color: rgba(139, 92, 246, 0.4);
}

[data-theme="dark"] .modern-input .el-input__wrapper.is-focus {
  border-color: #8b5cf6;
  box-shadow: 0 0 0 4px rgba(139, 92, 246, 0.15);
}

// 暗色模式下的按钮发光效果
[data-theme="dark"] .submit-btn {
  box-shadow: 0 4px 15px rgba(139, 92, 246, 0.3);
}

[data-theme="dark"] .submit-btn:hover:not(:disabled) {
  box-shadow: 0 8px 30px rgba(139, 92, 246, 0.5), 0 0 40px rgba(139, 92, 246, 0.2);
}

// 暗色模式下的主题切换按钮
[data-theme="dark"] .theme-toggle {
  background: rgba(30, 41, 59, 0.6);
  border-color: rgba(139, 92, 246, 0.2);
  backdrop-filter: blur(8px);
}

[data-theme="dark"] .theme-toggle:hover {
  background: rgba(139, 92, 246, 0.15);
  color: #a78bfa;
  border-color: rgba(139, 92, 246, 0.4);
}
</style>
