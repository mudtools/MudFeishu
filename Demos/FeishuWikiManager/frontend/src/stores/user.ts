import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { User } from '@/types'
import { authApi } from '@/api'

export const useUserStore = defineStore('user', () => {
  const user = ref<User | null>(null)
  const token = ref<string | null>(localStorage.getItem('token'))
  const loading = ref(false)

  const isLoggedIn = computed(() => !!token.value && !!user.value)

  async function fetchUser() {
    if (!token.value) return
    
    try {
      loading.value = true
      const response = await authApi.getMe()
      user.value = response.data
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
  }

  function logout() {
    user.value = null
    token.value = null
    localStorage.removeItem('token')
  }

  if (token.value) {
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
