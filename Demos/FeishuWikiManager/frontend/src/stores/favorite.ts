import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { FavoriteNode } from '@/types'
import { wikiApi } from '@/api'

export const useFavoriteStore = defineStore('favorite', () => {
  const favorites = ref<FavoriteNode[]>([])
  const loading = ref(false)

  async function fetchFavorites() {
    try {
      loading.value = true
      const response = await wikiApi.getFavorites()
      if (response.data.success && response.data.data) {
        favorites.value = response.data.data
      }
    } catch (error: any) {
      // 超时错误不抛出，避免阻塞页面其他功能
      if (error.code === 'ECONNABORTED' || error.message?.includes('timeout')) {
        console.warn('获取收藏列表超时，使用缓存数据')
        // 保持现有的 favorites 数据不变
      } else {
        console.error('Failed to fetch favorites:', error)
      }
      // 不抛出错误，让页面继续加载
    } finally {
      loading.value = false
    }
  }

  async function addFavorite(spaceId: string, nodeToken: string, title: string, objToken?: string, objType?: string) {
    try {
      await wikiApi.addFavorite(spaceId, nodeToken, title, objToken, objType)
      await fetchFavorites()
    } catch (error) {
      console.error('Failed to add favorite:', error)
      throw error
    }
  }

  async function removeFavorite(nodeToken: string) {
    try {
      await wikiApi.removeFavorite(nodeToken)
      favorites.value = favorites.value.filter(f => f.nodeToken !== nodeToken)
    } catch (error) {
      console.error('Failed to remove favorite:', error)
      throw error
    }
  }

  function isFavorite(nodeToken: string) {
    return favorites.value.some(f => f.nodeToken === nodeToken)
  }

  return {
    favorites,
    loading,
    fetchFavorites,
    addFavorite,
    removeFavorite,
    isFavorite
  }
})
