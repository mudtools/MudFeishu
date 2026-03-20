/**
 * 认证相关 API
 */
import { apiClient } from './client'
import type {
  ApiResponse,
  LoginRequest,
  LoginResponse,
  CurrentUserInfo,
  OAuthUrlRequest,
  OAuthUrlResponse,
} from '../types'

/**
 * 获取飞书 OAuth 授权链接
 * @param request OAuth URL 请求参数
 * @returns 授权链接
 */
export async function getOAuthUrl(
  request: OAuthUrlRequest
): Promise<ApiResponse<OAuthUrlResponse>> {
  const params = new URLSearchParams()
  params.append('redirectUri', request.redirectUri)
  if (request.state) {
    params.append('state', request.state)
  }
  return apiClient.get<OAuthUrlResponse>(`/auth/oauth-url?${params.toString()}`)
}

/**
 * 使用飞书授权码登录
 * @param request 登录请求
 * @returns 登录响应
 */
export async function loginWithCode(
  request: LoginRequest
): Promise<ApiResponse<LoginResponse>> {
  return apiClient.post<LoginResponse>('/auth/login', request)
}

/**
 * 获取当前登录用户信息
 * @returns 当前用户信息
 */
export async function getCurrentUser(): Promise<ApiResponse<CurrentUserInfo>> {
  return apiClient.get<CurrentUserInfo>('/auth/me')
}

/**
 * 退出登录
 * @returns 退出结果
 */
export async function logout(): Promise<ApiResponse<boolean>> {
  return apiClient.post<boolean>('/auth/logout')
}

/**
 * 刷新访问令牌
 * @returns 刷新结果
 */
export async function refreshToken(): Promise<ApiResponse<boolean>> {
  return apiClient.post<boolean>('/auth/refresh')
}

/**
 * 构建飞书 OAuth 回调 URL
 * 用于前端路由处理飞书回调
 * @returns 回调处理 URL
 */
export function buildFeishuCallbackUrl(): string {
  const currentUrl = new URL(window.location.href)
  return `${currentUrl.origin}/auth/callback`
}

/**
 * 生成随机 state 参数
 * @returns 随机字符串
 */
export function generateState(): string {
  return Math.random().toString(36).substring(2, 15) + Math.random().toString(36).substring(2, 15)
}
