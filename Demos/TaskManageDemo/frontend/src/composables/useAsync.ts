/**
 * 异步操作组合式函数
 * 封装加载状态、错误处理和数据获取逻辑
 */
import { ref, type Ref } from 'vue'
import { ElMessage } from 'element-plus'

export interface UseAsyncOptions<T> {
  /** 初始数据 */
  initialData?: T
  /** 是否在错误时显示消息 */
  showError?: boolean
  /** 错误消息 */
  errorMessage?: string
  /** 成功回调 */
  onSuccess?: (data: T) => void
  /** 错误回调 */
  onError?: (error: Error) => void
}

export interface UseAsyncResult<T> {
  /** 响应式数据 */
  data: Ref<T | undefined>
  /** 加载状态 */
  loading: Ref<boolean>
  /** 错误信息 */
  error: Ref<Error | null>
  /** 执行异步操作 */
  execute: (asyncFn: () => Promise<T>) => Promise<T | undefined>
  /** 重置状态 */
  reset: () => void
}

/**
 * 异步操作组合式函数
 * @param options 配置选项
 * @returns 异步操作状态和方法
 */
export function useAsync<T>(options: UseAsyncOptions<T> = {}): UseAsyncResult<T> {
  const {
    initialData,
    showError = true,
    errorMessage = '操作失败，请稍后重试',
    onSuccess,
    onError,
  } = options

  const data = ref<T | undefined>(initialData) as Ref<T | undefined>
  const loading = ref(false)
  const error = ref<Error | null>(null)

  /**
   * 执行异步操作
   * @param asyncFn 异步函数
   * @returns 异步结果
   */
  const execute = async (asyncFn: () => Promise<T>): Promise<T | undefined> => {
    loading.value = true
    error.value = null

    try {
      const result = await asyncFn()
      data.value = result
      onSuccess?.(result)
      return result
    } catch (e) {
      const err = e instanceof Error ? e : new Error(String(e))
      error.value = err

      if (showError) {
        ElMessage.error(errorMessage)
      }

      onError?.(err)
      return undefined
    } finally {
      loading.value = false
    }
  }

  /**
   * 重置状态
   */
  const reset = () => {
    data.value = initialData
    loading.value = false
    error.value = null
  }

  return {
    data,
    loading,
    error,
    execute,
    reset,
  }
}

/**
 * 创建可复用的异步操作
 * @param asyncFn 异步函数
 * @param options 配置选项
 * @returns 异步操作状态和方法
 */
export function useAsyncFn<T, P extends unknown[]>(
  asyncFn: (...args: P) => Promise<T>,
  options: UseAsyncOptions<T> = {}
) {
  const { data, loading, error, execute: baseExecute, reset } = useAsync<T>(options)

  const execute = async (...args: P): Promise<T | undefined> => {
    return baseExecute(() => asyncFn(...args))
  }

  return {
    data,
    loading,
    error,
    execute,
    reset,
  }
}
