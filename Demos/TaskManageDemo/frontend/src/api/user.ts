/**
 * 用户管理 API
 */
import { apiClient } from './client'
import type {
  ApiResponse,
  PagedResponse,
  User,
  UserQueryParameters,
  UpdateUserRequest,
  UserStatisticsDto,
} from '../types'

/**
 * 获取用户列表
 * @param parameters 查询参数
 * @returns 用户列表
 */
export async function getUsers(
  parameters: UserQueryParameters
): Promise<ApiResponse<PagedResponse<User>>> {
  const params = new URLSearchParams()
  params.append('page', parameters.page?.toString() || '1')
  params.append('pageSize', parameters.pageSize?.toString() || '20')
  if (parameters.keyword) params.append('keyword', parameters.keyword)
  if (parameters.role) params.append('role', parameters.role)
  if (parameters.departmentId) params.append('departmentId', parameters.departmentId)
  if (parameters.isActive !== undefined) params.append('isActive', parameters.isActive.toString())

  return apiClient.get<PagedResponse<User>>(`/users?${params.toString()}`)
}

/**
 * 获取用户详情
 * @param id 用户ID
 * @returns 用户详情
 */
export async function getUserById(id: number): Promise<ApiResponse<User>> {
  return apiClient.get<User>(`/users/${id}`)
}

/**
 * 更新用户信息
 * @param id 用户ID
 * @param request 更新请求
 * @returns 更新后的用户信息
 */
export async function updateUser(
  id: number,
  request: UpdateUserRequest
): Promise<ApiResponse<User>> {
  return apiClient.put<User>(`/users/${id}`, request)
}

/**
 * 删除用户
 * @param id 用户ID
 * @returns 删除结果
 */
export async function deleteUser(id: number): Promise<ApiResponse<boolean>> {
  return apiClient.delete<boolean>(`/users/${id}`)
}

/**
 * 激活用户
 * @param id 用户ID
 * @returns 激活结果
 */
export async function activateUser(id: number): Promise<ApiResponse<boolean>> {
  return apiClient.post<boolean>(`/users/${id}/activate`)
}

/**
 * 禁用用户
 * @param id 用户ID
 * @returns 禁用结果
 */
export async function deactivateUser(id: number): Promise<ApiResponse<boolean>> {
  return apiClient.post<boolean>(`/users/${id}/deactivate`)
}

/**
 * 获取用户统计数据
 * @returns 用户统计数据
 */
export async function getUserStatistics(): Promise<ApiResponse<UserStatisticsDto>> {
  return apiClient.get<UserStatisticsDto>('/users/statistics')
}
