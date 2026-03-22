/**
 * 认证相关 API
 */
import { apiClient } from "./client";
import type {
  ApiResponse,
  LoginRequest,
  LoginResponse,
  CurrentUserInfo,
  OAuthUrlRequest,
  OAuthUrlResponse,
  PasswordLoginRequest,
  RegisterRequest,
  BindFeishuRequest,
  BindFeishuResponse,
  FeishuAuthCheckResponse,
} from "../types";

/**
 * 获取飞书 OAuth 授权链接
 * @param request OAuth URL 请求参数
 * @returns 授权链接
 */
export async function getOAuthUrl(
  request: OAuthUrlRequest,
): Promise<ApiResponse<OAuthUrlResponse>> {
  const params = new URLSearchParams();
  if (request.redirectUri) {
    params.append("redirectUri", request.redirectUri);
  }
  const queryString = params.toString();
  const url = queryString
    ? `/auth/feishu/url?${queryString}`
    : "/auth/feishu/url";
  return apiClient.get<OAuthUrlResponse>(url);
}

/**
 * 使用飞书授权码登录
 * @param request 登录请求
 * @returns 登录响应
 */
export async function loginWithCode(
  request: LoginRequest,
): Promise<ApiResponse<LoginResponse>> {
  return apiClient.post<LoginResponse>("/auth/feishu/callback", request);
}

/**
 * 飞书登录后完成本地账户绑定
 * @param request 绑定请求
 * @returns 登录响应
 */
export async function completeFeishuBind(
  request: { tempToken: string; username: string; password: string },
): Promise<ApiResponse<LoginResponse>> {
  return apiClient.post<LoginResponse>("/auth/feishu/complete-bind", request);
}

/**
 * 用户名密码登录
 * @param request 登录请求
 * @returns 登录响应
 */
export async function passwordLogin(
  request: PasswordLoginRequest,
): Promise<ApiResponse<LoginResponse>> {
  return apiClient.post<LoginResponse>("/auth/login", request);
}

/**
 * 用户注册
 * @param request 注册请求
 * @returns 登录响应
 */
export async function register(
  request: RegisterRequest,
): Promise<ApiResponse<LoginResponse>> {
  return apiClient.post<LoginResponse>("/auth/register", request);
}

/**
 * 检查飞书授权状态
 * @param request 请求参数
 * @returns 飞书授权检查响应
 */
export async function checkFeishuAuth(
  request: LoginRequest,
): Promise<ApiResponse<FeishuAuthCheckResponse>> {
  return apiClient.post<FeishuAuthCheckResponse>("/auth/feishu/check", request);
}

/**
 * 绑定飞书账号
 * @param request 绑定请求
 * @returns 绑定响应
 */
export async function bindFeishu(
  request: BindFeishuRequest,
): Promise<ApiResponse<BindFeishuResponse>> {
  return apiClient.post<BindFeishuResponse>("/auth/feishu/bind", request);
}

/**
 * 获取当前登录用户信息
 * @returns 当前用户信息
 */
export async function getCurrentUser(): Promise<ApiResponse<CurrentUserInfo>> {
  return apiClient.get<CurrentUserInfo>("/auth/me");
}

/**
 * 退出登录
 * @returns 退出结果
 */
export async function logout(): Promise<ApiResponse<boolean>> {
  return apiClient.post<boolean>("/auth/logout");
}

/**
 * 刷新访问令牌
 * @returns 刷新结果
 */
export async function refreshToken(): Promise<ApiResponse<boolean>> {
  return apiClient.post<boolean>("/auth/refresh");
}

/**
 * 构建飞书 OAuth 回调 URL
 * 用于前端路由处理飞书回调
 * @returns 回调处理 URL
 */
export function buildFeishuCallbackUrl(): string {
  const currentUrl = new URL(window.location.href);
  return `${currentUrl.origin}/auth/callback`;
}

/**
 * 生成随机 state 参数
 * @returns 随机字符串
 */
export function generateState(): string {
  return (
    Math.random().toString(36).substring(2, 15) +
    Math.random().toString(36).substring(2, 15)
  );
}
