import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { Space, Node, PagedResponse } from '@/types'
import { wikiApi } from '@/api'

export const useWikiStore = defineStore('wiki', () => {
  const spaces = ref<Space[]>([])
  const currentSpace = ref<Space | null>(null)
  const nodeTree = ref<Map<string, Node[]>>(new Map())
  const loading = ref(false)
  const hasMoreSpaces = ref(false)
  const spacesPageToken = ref<string | undefined>()

  async function fetchSpaces(pageSize = 20, reset = false) {
    if (reset) {
      spaces.value = []
      spacesPageToken.value = undefined
    }

    try {
      loading.value = true
      const response = await wikiApi.getSpaces(pageSize, spacesPageToken.value)
      const data = response.data as PagedResponse<Space>
      
      spaces.value = [...spaces.value, ...data.items]
      hasMoreSpaces.value = data.hasMore
      spacesPageToken.value = data.pageToken
    } catch (error) {
      console.error('Failed to fetch spaces:', error)
      throw error
    } finally {
      loading.value = false
    }
  }

  async function fetchSpaceInfo(spaceId: string) {
    try {
      loading.value = true
      const response = await wikiApi.getSpaceInfo(spaceId)
      if (response.data.success && response.data.data) {
        currentSpace.value = response.data.data
      }
    } catch (error) {
      console.error('Failed to fetch space info:', error)
      throw error
    } finally {
      loading.value = false
    }
  }

  async function fetchNodeTree(spaceId: string, parentNodeToken?: string, reset = false) {
    try {
      loading.value = true
      const response = await wikiApi.getNodeTree(spaceId, parentNodeToken)
      const data = response.data as PagedResponse<Node>
      
      const key = parentNodeToken || 'root'
      if (reset) {
        nodeTree.value.clear()
      }
      nodeTree.value.set(key, data.items)
      
      return data
    } catch (error) {
      console.error('Failed to fetch node tree:', error)
      throw error
    } finally {
      loading.value = false
    }
  }

  async function createSpace(title: string, description?: string) {
    try {
      loading.value = true
      const response = await wikiApi.createSpace(title, description)
      if (response.data.success && response.data.data) {
        spaces.value.unshift(response.data.data)
        return response.data.data
      }
      return null
    } catch (error) {
      console.error('Failed to create space:', error)
      throw error
    } finally {
      loading.value = false
    }
  }

  function clearCurrentSpace() {
    currentSpace.value = null
    nodeTree.value.clear()
  }

  return {
    spaces,
    currentSpace,
    nodeTree,
    loading,
    hasMoreSpaces,
    fetchSpaces,
    fetchSpaceInfo,
    fetchNodeTree,
    createSpace,
    clearCurrentSpace
  }
})
