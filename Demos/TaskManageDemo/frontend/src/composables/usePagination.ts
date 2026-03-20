/**
 * 分页组合式函数
 * 封装分页状态和分页逻辑
 */
import { ref, computed, type Ref, type ComputedRef } from 'vue'

export interface PaginationOptions {
  /** 默认页码 */
  defaultPage?: number
  /** 默认每页大小 */
  defaultPageSize?: number
  /** 每页大小选项 */
  pageSizes?: number[]
  /** 最大页码 */
  maxPage?: number
}

export interface PaginationResult {
  /** 当前页码 */
  page: Ref<number>
  /** 每页大小 */
  pageSize: Ref<number>
  /** 每页大小选项 */
  pageSizes: number[]
  /** 总条数 */
  total: Ref<number>
  /** 总页数 */
  totalPages: ComputedRef<number>
  /** 是否有下一页 */
  hasNextPage: ComputedRef<boolean>
  /** 是否有上一页 */
  hasPrevPage: ComputedRef<boolean>
  /** 页码变化 */
  handlePageChange: (newPage: number) => void
  /** 每页大小变化 */
  handleSizeChange: (newSize: number) => void
  /** 重置分页 */
  reset: () => void
  /** 设置总条数 */
  setTotal: (newTotal: number) => void
  /** 获取分页参数 */
  getPaginationParams: () => { page: number; pageSize: number }
}

/**
 * 分页组合式函数
 * @param options 配置选项
 * @returns 分页状态和方法
 */
export function usePagination(options: PaginationOptions = {}): PaginationResult {
  const {
    defaultPage = 1,
    defaultPageSize = 20,
    pageSizes = [10, 20, 50, 100],
    maxPage = 10000,
  } = options

  const page = ref(defaultPage)
  const pageSize = ref(defaultPageSize)
  const total = ref(0)

  // 计算总页数
  const totalPages = computed(() => {
    if (total.value === 0) return 0
    return Math.ceil(total.value / pageSize.value)
  })

  // 是否有下一页
  const hasNextPage = computed(() => page.value < totalPages.value)

  // 是否有上一页
  const hasPrevPage = computed(() => page.value > 1)

  /**
   * 页码变化
   * @param newPage 新页码
   */
  const handlePageChange = (newPage: number) => {
    const clampedPage = Math.min(Math.max(1, newPage), totalPages.value || 1)
    page.value = Math.min(clampedPage, maxPage)
  }

  /**
   * 每页大小变化（重置到第一页）
   * @param newSize 新每页大小
   */
  const handleSizeChange = (newSize: number) => {
    pageSize.value = newSize
    page.value = 1 // 重置到第一页
  }

  /**
   * 重置分页
   */
  const reset = () => {
    page.value = defaultPage
    pageSize.value = defaultPageSize
    total.value = 0
  }

  /**
   * 设置总条数
   * @param newTotal 新总条数
   */
  const setTotal = (newTotal: number) => {
    total.value = Math.max(0, newTotal)
  }

  /**
   * 获取分页参数
   */
  const getPaginationParams = () => ({
    page: page.value,
    pageSize: pageSize.value,
  })

  return {
    page,
    pageSize,
    pageSizes,
    total,
    totalPages,
    hasNextPage,
    hasPrevPage,
    handlePageChange,
    handleSizeChange,
    reset,
    setTotal,
    getPaginationParams,
  }
}
