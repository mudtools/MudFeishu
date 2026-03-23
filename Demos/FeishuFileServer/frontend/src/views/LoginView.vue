<template>
  <div class="login-container">
    <div class="login-background">
      <div class="gradient-orb orb-1"></div>
      <div class="gradient-orb orb-2"></div>
      <div class="gradient-orb orb-3"></div>
    </div>
    
    <div class="login-card glass-card">
      <div class="login-header">
        <div class="logo-icon gradient-bg">
          <el-icon size="32"><FolderOpened /></el-icon>
        </div>
        <div class="login-line"></div>
        <h1 class="gradient-text">飞书云文件管理系统</h1>
        <p class="subtitle">{{ isLogin ? '欢迎回来' : '创建账户' }}</p>
      </div>

      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        class="login-form"
        @keyup.enter="handleSubmit"
      >
        <el-form-item prop="username">
          <div class="input-wrapper">
            <el-icon class="input-icon"><User /></el-icon>
            <el-input
              v-model="form.username"
              placeholder="用户名"
              size="large"
              class="modern-input"
            />
          </div>
        </el-form-item>

        <el-form-item prop="password">
          <div class="input-wrapper">
            <el-icon class="input-icon"><Lock /></el-icon>
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
            <div class="input-wrapper">
              <el-icon class="input-icon"><Lock /></el-icon>
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
            <div class="input-wrapper">
              <el-icon class="input-icon"><Message /></el-icon>
              <el-input
                v-model="form.email"
                placeholder="邮箱（可选）"
                size="large"
                class="modern-input"
              />
            </div>
          </el-form-item>

          <el-form-item prop="displayName">
            <div class="input-wrapper">
              <el-icon class="input-icon"><UserFilled /></el-icon>
              <el-input
                v-model="form.displayName"
                placeholder="显示名称（可选）"
                size="large"
                class="modern-input"
              />
            </div>
          </el-form-item>
        </template>

        <el-form-item>
          <button 
            type="button" 
            class="submit-btn btn-primary"
            :disabled="authStore.loading"
            @click="handleSubmit"
          >
            <transition name="fade" mode="out-in">
              <span v-if="!authStore.loading">{{ isLogin ? '登录' : '注册' }}</span>
              <span v-else class="loading-text">
                <el-icon class="is-loading"><Loading /></el-icon>
                处理中...
              </span>
            </transition>
          </button>
        </el-form-item>
      </el-form>

      <div class="login-footer">
        <p class="switch-text">
          {{ isLogin ? '还没有账号？' : '已有账号？' }}
          <a class="switch-link" @click="toggleMode">
            {{ isLogin ? '立即注册' : '立即登录' }}
          </a>
        </p>
      </div>
      
      <div class="theme-toggle-login" @click="appStore.toggleTheme">
        <transition name="scale" mode="out-in">
          <el-icon :key="appStore.isDark ? 'dark' : 'light'" :size="20">
            <Moon v-if="!appStore.isDark" />
            <Sunny v-else />
          </el-icon>
        </transition>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { User, Lock, Message, UserFilled, Moon, Sunny, FolderOpened, Loading } from '@element-plus/icons-vue'
import type { FormInstance, FormRules } from 'element-plus'
import { useAuthStore } from '@/stores/authStore'
import { useAppStore } from '@/stores/appStore'

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
  justify-content: center;
  align-items: center;
  padding: var(--spacing-lg);
  position: relative;
  overflow: hidden;
  background: var(--bg-color);
}

.login-background {
  position: absolute;
  inset: 0;
  overflow: hidden;
  pointer-events: none;
}

.gradient-orb {
  position: absolute;
  border-radius: 50%;
  filter: blur(80px);
  opacity: 0.5;
  animation: float 20s ease-in-out infinite;
  
  &.orb-1 {
    width: 600px;
    height: 600px;
    background: linear-gradient(135deg, var(--primary-color), var(--primary-light));
    top: -200px;
    right: -200px;
    animation-delay: 0s;
  }
  
  &.orb-2 {
    width: 500px;
    height: 500px;
    background: linear-gradient(135deg, #a855f7, #ec4899);
    bottom: -150px;
    left: -150px;
    animation-delay: -5s;
  }
  
  &.orb-3 {
    width: 400px;
    height: 400px;
    background: linear-gradient(135deg, #22c55e, #3b82f6);
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    animation-delay: -10s;
  }
}

@keyframes float {
  0%, 100% {
    transform: translate(0, 0) scale(1);
  }
  25% {
    transform: translate(50px, -50px) scale(1.1);
  }
  50% {
    transform: translate(-30px, 30px) scale(0.95);
  }
  75% {
    transform: translate(-50px, -30px) scale(1.05);
  }
}

.login-card {
  width: 100%;
  max-width: 420px;
  padding: var(--spacing-xl);
  position: relative;
  z-index: 1;
}

.login-header {
  text-align: center;
  margin-bottom: var(--spacing-xl);
  
  .logo-icon {
    width: 64px;
    height: 64px;
    border-radius: var(--radius-lg);
    display: inline-flex;
    align-items: center;
    justify-content: center;
    color: white;
    margin-bottom: var(--spacing-md);
    box-shadow: var(--shadow-lg), 0 8px 24px rgba(99, 102, 241, 0.3);
  }
  
  h1 {
    font-size: 28px;
    font-weight: 700;
    margin-bottom: var(--spacing-xs);
  }
  
  .subtitle {
    color: var(--text-secondary);
    font-size: 24px;
  }

  .login-line{
    width: 100%;
    height: 1px;
    background: var(--border-color);
    margin: var(--spacing-md) 0;
  }
}

.login-form {
  .el-form-item {
    margin-bottom: var(--spacing-md);
  }
}

.input-wrapper {
  position: relative;
  width: 100%;
  
  .input-icon {
    position: absolute;
    left: var(--spacing-md);
    top: 50%;
    transform: translateY(-50%);
    color: var(--text-tertiary);
    z-index: 1;
    transition: color var(--transition-fast);
  }
  
  &:focus-within .input-icon {
    color: var(--primary-color);
  }
}

.modern-input {
  :deep(.el-input__wrapper) {
    background: var(--bg-secondary);
    border: 1px solid var(--border-color);
    border-radius: var(--radius-md);
    padding: 0 var(--spacing-md);
    padding-left: 44px;
    box-shadow: none;
    transition: all var(--transition-fast);
    
    &:hover {
      border-color: var(--text-tertiary);
    }
    
    &.is-focus {
      border-color: var(--primary-color);
      box-shadow: 0 0 0 3px var(--primary-bg);
    }
  }
  
  :deep(.el-input__inner) {
    color: var(--text-primary);
    
    &::placeholder {
      color: var(--text-tertiary);
    }
  }
}

.submit-btn {
  width: 100%;
  height: 48px;
  font-size: 16px;
  font-weight: 600;
  border: none;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  
  &:disabled {
    opacity: 0.7;
    cursor: not-allowed;
  }
  
  .loading-text {
    display: flex;
    align-items: center;
    gap: var(--spacing-sm);
    
    .el-icon {
      animation: spin 1s linear infinite;
    }
  }
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.login-footer {
  text-align: center;
  margin-top: var(--spacing-lg);
}

.switch-text {
  color: var(--text-secondary);
  font-size: 14px;
}

.switch-link {
  color: var(--primary-color);
  font-weight: 500;
  cursor: pointer;
  transition: all var(--transition-fast);
  
  &:hover {
    text-decoration: underline;
  }
}

.theme-toggle-login {
  position: absolute;
  top: var(--spacing-md);
  right: var(--spacing-md);
  width: 40px;
  height: 40px;
  border: none;
  background: var(--bg-tertiary);
  border-radius: var(--radius-full);
  color: var(--text-secondary);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all var(--transition-fast);
  
  &:hover {
    background: var(--primary-bg);
    color: var(--primary-color);
  }
}
</style>
