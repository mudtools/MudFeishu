import { ref, computed } from 'vue'
import { useWikiStore } from '@/stores/wiki'
import { wikiApi } from '@/api'
import type { Node } from '@/types'

export function useNodeTree(spaceId: string) {
  const wikiStore = useWikiStore()
  const loading = ref(false)
  const error = ref<string | null>(null)
  const expandedNodes = ref<Set<string>>(new Set())

  const rootNodeKey = 'root'
  const rootNodes = computed(() => wikiStore.nodeTree.get(rootNodeKey) || [])

  function getChildren(parentToken: string): Node[] {
    return wikiStore.nodeTree.get(parentToken) || []
  }

  function isExpanded(nodeToken: string): boolean {
    return expandedNodes.value.has(nodeToken)
  }

  function toggleExpand(nodeToken: string) {
    if (expandedNodes.value.has(nodeToken)) {
      expandedNodes.value.delete(nodeToken)
    } else {
      expandedNodes.value.add(nodeToken)
    }
  }

  async function fetchRootNodes() {
    try {
      loading.value = true
      error.value = null
      await wikiStore.fetchNodeTree(spaceId, undefined, true)
    } catch (err: any) {
      error.value = err.message || '获取节点树失败'
      throw err
    } finally {
      loading.value = false
    }
  }

  async function fetchChildNodes(parentToken: string) {
    try {
      loading.value = true
      error.value = null
      await wikiStore.fetchNodeTree(spaceId, parentToken)
      expandedNodes.value.add(parentToken)
    } catch (err: any) {
      error.value = err.message || '获取子节点失败'
      throw err
    } finally {
      loading.value = false
    }
  }

  async function createNode(parentToken: string | undefined, title: string, objType: string = 'docx') {
    try {
      loading.value = true
      error.value = null
      const response = await wikiApi.createNode(spaceId, {
        spaceId,
        parentNodeToken: parentToken,
        title,
        objType
      })
      
      if (response.data.success) {
        await wikiStore.fetchNodeTree(spaceId, parentToken, true)
      }
      
      return response.data.data
    } catch (err: any) {
      error.value = err.message || '创建节点失败'
      throw err
    } finally {
      loading.value = false
    }
  }

  async function updateNodeTitle(nodeToken: string, title: string) {
    try {
      loading.value = true
      error.value = null
      await wikiApi.updateNodeTitle(spaceId, nodeToken, title)
      await fetchRootNodes()
    } catch (err: any) {
      error.value = err.message || '更新节点标题失败'
      throw err
    } finally {
      loading.value = false
    }
  }

  async function moveNode(nodeToken: string, targetParentToken: string | null) {
    try {
      loading.value = true
      error.value = null
      await wikiApi.moveNode(spaceId, nodeToken, targetParentToken ?? undefined)
      await fetchRootNodes()
    } catch (err: any) {
      error.value = err.message || '移动节点失败'
      throw err
    } finally {
      loading.value = false
    }
  }

  return {
    rootNodes,
    loading,
    error,
    expandedNodes,
    getChildren,
    isExpanded,
    toggleExpand,
    fetchRootNodes,
    fetchChildNodes,
    createNode,
    updateNodeTitle,
    moveNode
  }
}
