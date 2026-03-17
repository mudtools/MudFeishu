import { ref, computed } from 'vue'
import { useWikiStore } from '@/stores/wiki'

export function useWiki() {
  const wikiStore = useWikiStore()
  const loading = ref(false)
  const error = ref<string | null>(null)

  const spaces = computed(() => wikiStore.spaces)
  const currentSpace = computed(() => wikiStore.currentSpace)
  const hasMoreSpaces = computed(() => wikiStore.hasMoreSpaces)

  async function fetchSpaces(pageSize = 20, reset = false) {
    try {
      loading.value = true
      error.value = null
      await wikiStore.fetchSpaces(pageSize, reset)
    } catch (err: any) {
      error.value = err.message || '获取知识空间失败'
      throw err
    } finally {
      loading.value = false
    }
  }

  async function fetchSpaceInfo(spaceId: string) {
    try {
      loading.value = true
      error.value = null
      await wikiStore.fetchSpaceInfo(spaceId)
    } catch (err: any) {
      error.value = err.message || '获取知识空间详情失败'
      throw err
    } finally {
      loading.value = false
    }
  }

  async function createSpace(title: string, description?: string) {
    try {
      loading.value = true
      error.value = null
      const result = await wikiStore.createSpace(title, description)
      return result
    } catch (err: any) {
      error.value = err.message || '创建知识空间失败'
      throw err
    } finally {
      loading.value = false
    }
  }

  function clearCurrentSpace() {
    wikiStore.clearCurrentSpace()
  }

  return {
    spaces,
    currentSpace,
    hasMoreSpaces,
    loading,
    error,
    fetchSpaces,
    fetchSpaceInfo,
    createSpace,
    clearCurrentSpace
  }
}
