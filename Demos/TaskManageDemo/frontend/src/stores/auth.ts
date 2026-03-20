/**
 * 认证状态管理
 */
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { User } from '../types'

export interface AuthState {
  token: string | null
  user: User | null
  loading: boolean
}

export const useAuthStore = defineStore('auth', () => {
  // 状态
  const token = ref<string | null>(localStorage.getItem('token'))
  const user = ref<User | null>(null)
  const loading = ref(false)

  // 计算属性
  const isAuthenticated = computed(() => !!token.value)
  const userName = computed(() => user.value?.name || '未登录')
  const userAvatar = computed(() => user.value?.avatarUrl || '')

  /**
   * 设置 Token
   */
  function setToken(newToken: string) {
    token.value = newToken
    localStorage.setItem('token', newToken)
  }

  /**
   * 设置用户信息
   */
  function setUser(newUser: User) {
    user.value = newUser
    localStorage.setItem('user', JSON.stringify(newUser))
  }

  /**
   * 从本地存储恢复用户信息
   */
  function restoreUser() {
    const storedUser = localStorage.getItem('user')
    if (storedUser) {
      try {
        user.value = JSON.parse(storedUser)
      } catch {
        user.value = null
      }
    }
  }

  /**
   * 登出
   */
  function logout() {
    token.value = null
    user.value = null
    localStorage.removeItem('token')
    localStorage.removeItem('user')
  }

  /**
   * 检查是否有权限
   */
  function hasPermission(permission: string): boolean {
    if (!isAuthenticated.value) {
      return false
    }

    // 管理员拥有所有权限
    if (user.value?.role === 'admin') {
      return true
    }

    // 检查用户权限列表
    const userPermissions = user.value?.permissions || []
    return userPermissions.includes(permission)
  }

  /**
   * 检查是否有任意一个权限
   */
  function hasAnyPermission(permissions: string[]): boolean {
    if (!isAuthenticated.value) {
      return false
    }

    // 管理员拥有所有权限
    if (user.value?.role === 'admin') {
      return true
    }

    const userPermissions = user.value?.permissions || []
    return permissions.some(p => userPermissions.includes(p))
  }

  /**
   * 检查是否有角色
   */
  function hasRole(role: string): boolean {
    if (!isAuthenticated.value) {
      return false
    }

    return user.value?.role === role
  }

  /**
   * 检查是否有任意一个角色
   */
  function hasAnyRole(roles: string[]): boolean {
    if (!isAuthenticated.value) {
      return false
    }

    const userRole = user.value?.role
    if (!userRole) {
      return false
    }

    return roles.includes(userRole)
  }

  // 初始化时恢复用户信息
  restoreUser()

  return {
    // 状态
    token,
    user,
    loading,
    // 计算属性
    isAuthenticated,
    userName,
    userAvatar,
    // 方法
    setToken,
    setUser,
    restoreUser,
    logout,
    hasPermission,
    hasAnyPermission,
    hasRole,
    hasAnyRole,
  }
})
