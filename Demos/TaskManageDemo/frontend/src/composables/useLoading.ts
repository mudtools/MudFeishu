/**
 * 加载状态组合式函数
 * 简单的加载状态管理
 */
import { ref, type Ref } from 'vue'

export interface UseLoadingResult {
  /** 加载状态 */
  loading: Ref<boolean>
  /** 开始加载 */
  start: () => void
  /** 停止加载 */
  stop: () => void
  /** 切换加载状态 */
  toggle: () => void
  /** 包装异步函数，自动管理加载状态 */
  withLoading: <T>(asyncFn: () => Promise<T>) => Promise<T>
}

/**
 * 加载状态组合式函数
 * @param initialState 初始加载状态
 * @returns 加载状态和方法
 */
export function useLoading(initialState = false): UseLoadingResult {
  const loading = ref(initialState)

  const start = () => {
    loading.value = true
  }

  const stop = () => {
    loading.value = false
  }

  const toggle = () => {
    loading.value = !loading.value
  }

  /**
   * 包装异步函数，自动管理加载状态
   * @param asyncFn 异步函数
   * @returns 异步结果
   */
  const withLoading = async <T>(asyncFn: () => Promise<T>): Promise<T> => {
    loading.value = true
    try {
      return await asyncFn()
    } finally {
      loading.value = false
    }
  }

  return {
    loading,
    start,
    stop,
    toggle,
    withLoading,
  }
}
