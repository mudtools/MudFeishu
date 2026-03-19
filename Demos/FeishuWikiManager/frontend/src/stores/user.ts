import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { User, UserDetail } from '@/types'
import { authApi } from '@/api'

const USER_STORAGE_KEY = 'user_info'
const USER_DETAIL_STORAGE_KEY = 'user_detail_info'

function getStoredUser(): User | null {
  try {
    const stored = localStorage.getItem(USER_STORAGE_KEY)
    return stored ? JSON.parse(stored) : null
  } catch {
    return null
  }
}

function getStoredUserDetail(): UserDetail | null {
  try {
    const stored = localStorage.getItem(USER_DETAIL_STORAGE_KEY)
    return stored ? JSON.parse(stored) : null
  } catch {
    return null
  }
}

export const useUserStore = defineStore('user', () => {
  const user = ref<User | null>(getStoredUser())
  const userDetail = ref<UserDetail | null>(getStoredUserDetail())
  const token = ref<string | null>(localStorage.getItem('token'))
  const loading = ref(false)
  const detailLoading = ref(false)

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

  async function fetchUserDetail() {
    if (!token.value) return
    
    try {
      detailLoading.value = true
      const response = await authApi.getMeDetail()
      if (response.data.success && response.data.data) {
        userDetail.value = response.data.data
        localStorage.setItem(USER_DETAIL_STORAGE_KEY, JSON.stringify(response.data.data))
      }
    } catch (error) {
      console.error('Failed to fetch user detail:', error)
    } finally {
      detailLoading.value = false
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

  async function logout(skipApiCall = false) {
    // 如果有token且不需要跳过API调用，则通知后端
    if (token.value && !skipApiCall) {
      try {
        await authApi.logout()
      } catch (error: any) {
        // 401表示token已过期，也视为退出成功；其他错误记录到控制台
        if (error.response?.status !== 401) {
          console.error('Logout API call failed:', error)
        }
        // 即使 API 调用失败，也要继续清除本地状态
      }
    }
    
    // 清除本地状态
    user.value = null
    userDetail.value = null
    token.value = null
    localStorage.removeItem('token')
    localStorage.removeItem(USER_STORAGE_KEY)
    localStorage.removeItem(USER_DETAIL_STORAGE_KEY)
  }

  // 如果有 token 但没有用户信息，尝试获取
  if (token.value && !user.value) {
    fetchUser()
  }

  return {
    user,
    userDetail,
    token,
    loading,
    detailLoading,
    isLoggedIn,
    fetchUser,
    fetchUserDetail,
    setToken,
    setUser,
    logout
  }
})
