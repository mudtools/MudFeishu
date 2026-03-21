import axios, { AxiosError } from 'axios'
import type { AxiosInstance, AxiosRequestConfig, InternalAxiosRequestConfig } from 'axios'
import { ElMessage, ElNotification } from 'element-plus'
import type { ApiResponse } from '../types'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api'
const DEFAULT_TIMEOUT = Number(import.meta.env.VITE_API_TIMEOUT) || 30000

/**
 * API请求配置选项
 */
export interface ApiRequestConfig extends AxiosRequestConfig {
  /** 是否跳过错误通知 */
  skipErrorNotification?: boolean
  /** 自定义超时时间（毫秒） */
  timeout?: number
}

/**
 * 错误码映射
 */
const ERROR_MESSAGES: Record<number, string> = {
  400: '请求参数错误',
  401: '登录已过期，请重新登录',
  403: '没有权限访问此资源',
  404: '请求的资源不存在',
  405: '请求方法不允许',
  408: '请求超时',
  409: '资源冲突',
  422: '参数验证失败',
  429: '请求过于频繁，请稍后重试',
  500: '服务器内部错误',
  502: '网关错误',
  503: '服务暂时不可用',
  504: '网关超时',
}

/**
 * 是否为网络错误
 */
function isNetworkError(error: AxiosError): boolean {
  return !error.response && Boolean(error.code)
}

/**
 * 是否为取消请求
 */
function isCanceledError(error: AxiosError): boolean {
  return axios.isCancel(error)
}

/**
 * 获取错误消息
 */
function getErrorMessage(error: AxiosError): string {
  // 网络错误
  if (isNetworkError(error)) {
    return '网络连接失败，请检查网络设置'
  }

  // 请求被取消
  if (isCanceledError(error)) {
    return '请求已取消'
  }

  // HTTP 错误
  const status = error.response?.status
  if (status && ERROR_MESSAGES[status]) {
    return ERROR_MESSAGES[status]
  }

  // 后端返回的业务错误消息
  const data = error.response?.data as ApiResponse<unknown> | undefined
  if (data?.message) {
    return data.message
  }

  return '请求失败，请稍后重试'
}

/**
 * 是否需要显示错误提示
 */
function shouldShowError(config: InternalAxiosRequestConfig | undefined): boolean {
  return config?.headers?.['X-Skip-Error-Notification'] !== 'true'
}

class ApiClient {
  private client: AxiosInstance

  constructor() {
    this.client = axios.create({
      baseURL: API_BASE_URL,
      timeout: DEFAULT_TIMEOUT,
      headers: {
        'Content-Type': 'application/json',
      },
    })

    this.setupRequestInterceptor()
    this.setupResponseInterceptor()
  }

  /**
   * 创建取消令牌
   */
  createCancelToken() {
    return axios.CancelToken.source()
  }

  /**
   * 设置请求拦截器
   */
  private setupRequestInterceptor() {
    this.client.interceptors.request.use(
      (config) => {
        const token = localStorage.getItem('token')
        if (token) {
          config.headers.Authorization = `Bearer ${token}`
        }
        return config
      },
      (error) => Promise.reject(error)
    )
  }

  /**
   * 设置响应拦截器
   */
  private setupResponseInterceptor() {
    this.client.interceptors.response.use(
      (response) => {
        // 检查业务状态码
        const data = response.data as ApiResponse<unknown>
        if (data.code && data.code !== 0 && data.code !== 200) {
          // 业务错误
          if (shouldShowError(response.config)) {
            ElMessage.error(data.message || '操作失败')
          }
          return Promise.reject(new Error(data.message || '操作失败'))
        }
        return response
      },
      (error: AxiosError) => {
        // 忽略取消请求的错误
        if (isCanceledError(error)) {
          return Promise.reject(error)
        }

        const message = getErrorMessage(error)
        const config = error.config as InternalAxiosRequestConfig | undefined

        // 401 未授权 - 跳转登录
        if (error.response?.status === 401) {
          localStorage.removeItem('token')
          localStorage.removeItem('user')

          // 避免在登录页面重复跳转
          if (window.location.pathname !== '/login') {
            ElNotification.warning({
              title: '登录已过期',
              message: '请重新登录',
              duration: 3000,
            })
            window.location.href = '/login'
          }
        }
        // 403 禁止访问
        else if (error.response?.status === 403) {
          if (shouldShowError(config)) {
            ElNotification.error({
              title: '权限不足',
              message: '您没有权限访问此资源',
              duration: 3000,
            })
          }
        }
        // 500+ 服务器错误
        else if (error.response?.status && error.response.status >= 500) {
          if (shouldShowError(config)) {
            ElNotification.error({
              title: '服务器错误',
              message: '服务器暂时无法处理请求，请稍后重试',
              duration: 5000,
            })
          }
        }
        // 网络错误
        else if (isNetworkError(error)) {
          if (shouldShowError(config)) {
            ElNotification.error({
              title: '网络错误',
              message: '无法连接到服务器，请检查网络连接',
              duration: 5000,
            })
          }
        }
        // 其他错误
        else if (shouldShowError(config)) {
          ElMessage.error(message)
        }

        // 增强错误对象
        const enhancedError = error as AxiosError & { message: string }
        enhancedError.message = message

        return Promise.reject(enhancedError)
      }
    )
  }

  /**
   * GET 请求
   */
  async get<T>(url: string, config?: ApiRequestConfig): Promise<ApiResponse<T>> {
    const axiosConfig = this.buildAxiosConfig(config)
    const response = await this.client.get<ApiResponse<T>>(url, axiosConfig)
    return response.data
  }

  /**
   * POST 请求
   */
  async post<T>(url: string, data?: unknown, config?: ApiRequestConfig): Promise<ApiResponse<T>> {
    const axiosConfig = this.buildAxiosConfig(config)
    const response = await this.client.post<ApiResponse<T>>(url, data, axiosConfig)
    return response.data
  }

  /**
   * PUT 请求
   */
  async put<T>(url: string, data?: unknown, config?: ApiRequestConfig): Promise<ApiResponse<T>> {
    const axiosConfig = this.buildAxiosConfig(config)
    const response = await this.client.put<ApiResponse<T>>(url, data, axiosConfig)
    return response.data
  }

  /**
   * DELETE 请求
   */
  async delete<T>(url: string, config?: ApiRequestConfig): Promise<ApiResponse<T>> {
    const axiosConfig = this.buildAxiosConfig(config)
    const response = await this.client.delete<ApiResponse<T>>(url, axiosConfig)
    return response.data
  }

  /**
   * PATCH 请求
   */
  async patch<T>(url: string, data?: unknown, config?: ApiRequestConfig): Promise<ApiResponse<T>> {
    const axiosConfig = this.buildAxiosConfig(config)
    const response = await this.client.patch<ApiResponse<T>>(url, data, axiosConfig)
    return response.data
  }

  /**
   * 构建 Axios 配置
   */
  private buildAxiosConfig(config?: ApiRequestConfig): AxiosRequestConfig {
    if (!config) return {}

    const axiosConfig: AxiosRequestConfig = { ...config }

    // 处理 skipErrorNotification
    if (config.skipErrorNotification) {
      axiosConfig.headers = {
        ...axiosConfig.headers,
        'X-Skip-Error-Notification': 'true',
      }
    }

    // 处理自定义超时
    if (config.timeout) {
      axiosConfig.timeout = config.timeout
    }

    return axiosConfig
  }

  /**
   * 获取原始 Axios 实例（用于特殊场景）
   */
  getAxiosInstance(): AxiosInstance {
    return this.client
  }
}

export const apiClient = new ApiClient()
