/**
 * 角色管理 API
 */
import { apiClient } from "./client";
import type {
  ApiResponse,
  PagedResponse,
  Role,
  Permission,
  PermissionGroup,
  User,
  RoleQueryParameters,
  CreateRoleRequest,
  UpdateRoleRequest,
  AssignRoleRequest,
  AssignPermissionRequest,
  UserPermissionDetail,
} from "../types";

/**
 * 获取角色列表
 * @param parameters 查询参数
 * @returns 角色列表
 */
export async function getRoles(
  parameters: RoleQueryParameters,
): Promise<ApiResponse<PagedResponse<Role>>> {
  const params = new URLSearchParams();
  params.append("page", parameters.page?.toString() || "1");
  params.append("pageSize", parameters.pageSize?.toString() || "20");
  if (parameters.keyword) params.append("keyword", parameters.keyword);
  if (parameters.isEnabled !== undefined)
    params.append("isEnabled", parameters.isEnabled.toString());

  return apiClient.get<PagedResponse<Role>>(`/roles?${params.toString()}`);
}

/**
 * 获取所有启用的角色
 * @returns 角色列表
 */
export async function getAllRoles(): Promise<ApiResponse<Role[]>> {
  return apiClient.get<Role[]>("/roles/all");
}

/**
 * 获取角色详情
 * @param id 角色ID
 * @returns 角色详情
 */
export async function getRoleById(id: number): Promise<ApiResponse<Role>> {
  return apiClient.get<Role>(`/roles/${id}`);
}

/**
 * 创建角色
 * @param request 创建请求
 * @returns 创建的角色
 */
export async function createRole(
  request: CreateRoleRequest,
): Promise<ApiResponse<Role>> {
  return apiClient.post<Role>("/roles", request);
}

/**
 * 更新角色
 * @param id 角色ID
 * @param request 更新请求
 * @returns 更新后的角色
 */
export async function updateRole(
  id: number,
  request: UpdateRoleRequest,
): Promise<ApiResponse<Role>> {
  return apiClient.put<Role>(`/roles/${id}`, request);
}

/**
 * 删除角色
 * @param id 角色ID
 * @returns 删除结果
 */
export async function deleteRole(id: number): Promise<ApiResponse<boolean>> {
  return apiClient.delete<boolean>(`/roles/${id}`);
}

/**
 * 获取角色的权限列表
 * @param id 角色ID
 * @returns 权限列表
 */
export async function getRolePermissions(
  id: number,
): Promise<ApiResponse<Permission[]>> {
  return apiClient.get<Permission[]>(`/roles/${id}/permissions`);
}

/**
 * 为角色分配权限
 * @param id 角色ID
 * @param permissionIds 权限ID列表
 * @returns 分配结果
 */
export async function assignRolePermissions(
  id: number,
  permissionIds: number[],
): Promise<ApiResponse<boolean>> {
  return apiClient.post<boolean>(`/roles/${id}/permissions`, permissionIds);
}

/**
 * 获取角色的用户列表
 * @param id 角色ID
 * @returns 用户列表
 */
export async function getRoleUsers(id: number): Promise<ApiResponse<User[]>> {
  return apiClient.get<User[]>(`/roles/${id}/users`);
}

/**
 * 获取所有权限列表
 * @returns 权限列表
 */
export async function getAllPermissions(): Promise<ApiResponse<Permission[]>> {
  return apiClient.get<Permission[]>("/permissions");
}

/**
 * 获取权限分组列表
 * @returns 权限分组列表
 */
export async function getPermissionGroups(): Promise<
  ApiResponse<PermissionGroup[]>
> {
  return apiClient.get<PermissionGroup[]>("/permissions/groups");
}

/**
 * 获取用户权限详情
 * @param userId 用户ID
 * @returns 用户权限详情
 */
export async function getUserPermissionDetail(
  userId: number,
): Promise<ApiResponse<UserPermissionDetail>> {
  return apiClient.get<UserPermissionDetail>(`/permissions/users/${userId}`);
}

/**
 * 为用户分配权限
 * @param request 分配请求
 * @returns 分配结果
 */
export async function assignUserPermissions(
  request: AssignPermissionRequest,
): Promise<ApiResponse<boolean>> {
  return apiClient.post<boolean>("/permissions/users/assign", request);
}

/**
 * 获取用户的角色列表
 * @param userId 用户ID
 * @returns 角色列表
 */
export async function getUserRoles(
  userId: number,
): Promise<ApiResponse<Role[]>> {
  return apiClient.get<Role[]>(`/permissions/users/${userId}/roles`);
}

/**
 * 为用户分配角色
 * @param request 分配请求
 * @returns 分配结果
 */
export async function assignUserRoles(
  request: AssignRoleRequest,
): Promise<ApiResponse<boolean>> {
  return apiClient.post<boolean>("/permissions/users/roles/assign", request);
}

/**
 * 移除用户的角色
 * @param userId 用户ID
 * @param roleIds 角色ID列表
 * @returns 移除结果
 */
export async function removeUserRoles(
  userId: number,
  roleIds: number[],
): Promise<ApiResponse<boolean>> {
  return apiClient.delete<boolean>(`/permissions/users/${userId}/roles`, {
    data: roleIds,
  });
}

/**
 * 初始化权限数据
 * @returns 初始化结果
 */
export async function initializePermissions(): Promise<ApiResponse<boolean>> {
  return apiClient.post<boolean>("/permissions/initialize");
}
