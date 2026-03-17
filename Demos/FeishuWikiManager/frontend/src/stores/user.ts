import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { User } from '@/types'
import { authApi } from '@/api'

const USER_STORAGE_KEY = 'user_info'

function getStoredUser(): User | null {
  try {
    const stored = localStorage.getItem(USER_STORAGE_KEY)
    return stored ? JSON.parse(stored) : null
  } catch {
    return null
  }
}

export const useUserStore = defineStore('user', () => {
  const user = ref<User | null>(getStoredUser())
  const token = ref<string | null>(localStorage.getItem('token'))
  const loading = ref(false)

  const isLoggedIn = computed(() => !!token.value && !!user.value)

  async function fetchUser() {
    if (!token.value) return
    
    try {
      loading.value = true
      const response = await authApi.getMe()
      user.value = response.data
      localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(response.data))
    } catch (error) {
      console.error('Failed to fetch user:', error)
      logout()
    } finally {
      loading.value = false
    }
  }

  function setToken(newToken: string) {
    token.value = newToken
    localStorage.setItem('token', newToken)
  }

  function setUser(newUser: User) {
    user.value = newUser
    localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(newUser))
  }

  function logout() {
    user.value = null
    token.value = null
    localStorage.removeItem('token')
    localStorage.removeItem(USER_STORAGE_KEY)
  }

  // 如果有 token 但没有用户信息，尝试获取
  if (token.value && !user.value) {
    fetchUser()
  }

  return {
    user,
    token,
    loading,
    isLoggedIn,
    fetchUser,
    setToken,
    setUser,
    logout
  }
})
