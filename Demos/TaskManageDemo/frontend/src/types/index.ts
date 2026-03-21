export interface ApiResponse<T> {
  success: boolean;
  code?: number;
  message: string;
  data: T;
}

export interface PagedResponse<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface Task {
  id: number;
  taskGuid: string;
  taskListId?: number;
  taskListGuid?: string;
  taskListName?: string;
  summary: string;
  description?: string;
  priority: number;
  isCompleted: boolean;
  completedTime?: string;
  dueTime?: string;
  startTime?: string;
  createdTime?: string;
  completedRatio?: number;
  source: number;
  createdAt: string;
  updatedAt: string;
  creatorName?: string;
  tags?: string[];
  members?: TaskMember[];
  checkItems?: TaskCheckItem[];
}

export interface TaskMember {
  id: number;
  taskId: number;
  userId: number;
  role: string;
  feishuId?: string;
  name?: string;
  avatarUrl?: string;
  user?: User;
}

export interface TaskCheckItem {
  id: number;
  taskId: number;
  content: string;
  isCompleted: boolean;
  sortKey: number;
}

export interface User {
  id: number;
  feishuId: string;
  name: string;
  englishName?: string;
  email?: string;
  avatarUrl?: string;
  departmentId?: number;
  department?: Department;
  role?: string;
  permissions?: string[];
  createdAt: string;
  updatedAt: string;
}

export interface Department {
  id: number;
  feishuId: string;
  name: string;
  parentId?: number;
  parent?: Department;
  createdAt: string;
  updatedAt: string;
}

export interface TaskList {
  id: number;
  taskListGuid: string;
  name: string;
  description?: string;
  createdAt: string;
  updatedAt: string;
  members?: TaskListMember[];
}

export interface TaskListMember {
  id: number;
  taskListId: number;
  userId: number;
  role: string;
  user?: User;
}

export interface TaskTemplate {
  id: number;
  name: string;
  description?: string;
  defaultSummary?: string;
  defaultDescription?: string;
  defaultPriority: number;
  defaultDueDays?: number;
  checkItems?: string;
  isPublic: boolean;
  createdAt: string;
}

export interface TaskSearchParams {
  keyword?: string;
  status?: "all" | "pending" | "completed";
  priority?: number;
  assigneeId?: number;
  taskListId?: number;
  dueDateFrom?: string;
  dueDateTo?: string;
  includeCompleted?: boolean;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDescending?: boolean;
}

export interface CreateTaskRequest {
  taskListGuid?: string;
  taskListId?: string;
  summary: string;
  description?: string;
  priority?: number;
  dueTime?: string;
  startTime?: string;
  assignees?: string[];
  checkItems?: string[];
}

export interface UpdateTaskRequest {
  summary?: string;
  description?: string;
  priority?: number;
  dueTime?: string;
  startTime?: string;
  isCompleted?: boolean;
}

export interface CreateTaskListRequest {
  name: string;
  description?: string;
}

export interface CreateTaskTemplateRequest {
  name: string;
  description?: string;
  defaultSummary?: string;
  defaultDescription?: string;
  defaultPriority?: number;
  defaultDueDays?: number;
  checkItems?: string[];
  isPublic?: boolean;
}

export interface Statistics {
  totalTasks: number;
  completedTasks: number;
  pendingTasks: number;
  overdueTasks: number;
  completionRate: number;
  tasksByPriority: { priority: number; count: number }[];
  tasksByStatus: { status: string; count: number }[];
  recentTasks: Task[];
}

// ==================== 认证相关类型 ====================

/**
 * 登录请求
 */
export interface LoginRequest {
  code: string;
  state?: string;
}

/**
 * 用户名密码登录请求
 */
export interface PasswordLoginRequest {
  username: string;
  password: string;
}

/**
 * 用户注册请求
 */
export interface RegisterRequest {
  username: string;
  password: string;
  confirmPassword: string;
  feishuCode?: string;
  feishuState?: string;
}

/**
 * 绑定飞书请求
 */
export interface BindFeishuRequest {
  code: string;
  state: string;
}

/**
 * 绑定飞书响应
 */
export interface BindFeishuResponse {
  success: boolean;
  feishuName?: string;
  feishuAvatar?: string;
  message?: string;
}

/**
 * 修改密码请求
 */
export interface ChangePasswordRequest {
  oldPassword: string;
  newPassword: string;
  confirmPassword: string;
}

/**
 * 飞书用户信息（用于注册流程）
 */
export interface FeishuUserInfo {
  feishuId: string;
  openId?: string;
  name: string;
  englishName?: string;
  avatarUrl?: string;
  email?: string;
  mobile?: string;
  departmentId?: string;
}

/**
 * 飞书授权检查响应
 */
export interface FeishuAuthCheckResponse {
  userExists: boolean;
  isFeishuBound: boolean;
  feishuUser?: FeishuUserInfo;
  tempToken?: string;
}

/**
 * 登录响应
 */
export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  user: UserInfo;
  isFirstLogin: boolean;
  isFeishuBound: boolean;
}

/**
 * 用户信息（登录后返回）
 */
export interface UserInfo {
  id: number;
  name: string;
  feishuId: string;
  departmentId?: string;
  role: string;
  permissions: string[];
}

/**
 * 当前登录用户信息
 */
export interface CurrentUserInfo {
  id: number;
  feishuId: string;
  name: string;
  englishName?: string;
  email?: string;
  avatarUrl?: string;
  departmentId?: number;
  departmentName?: string;
  role: string;
  permissions: string[];
  createdAt: string;
}

/**
 * OAuth URL 请求
 */
export interface OAuthUrlRequest {
  redirectUri: string;
  state?: string;
}

/**
 * OAuth URL 响应
 */
export interface OAuthUrlResponse {
  url: string;
}

// ==================== 用户管理类型 ====================

/**
 * 用户查询参数
 */
export interface UserQueryParameters {
  page?: number;
  pageSize?: number;
  keyword?: string;
  role?: string;
  departmentId?: string;
  isActive?: boolean;
}

/**
 * 更新用户请求
 */
export interface UpdateUserRequest {
  name?: string;
  englishName?: string;
  mobile?: string;
  role?: string;
  isActive?: boolean;
}

/**
 * 用户统计数据
 */
export interface UserStatisticsDto {
  totalUsers: number;
  activeUsers: number;
  adminUsers: number;
  newUsersThisMonth: number;
}

// ==================== 角色管理类型 ====================

/**
 * 角色
 */
export interface Role {
  id: number;
  code: string;
  name: string;
  description?: string;
  isSystem: boolean;
  isEnabled: boolean;
  sortOrder: number;
  permissions?: Permission[];
  userCount?: number;
  createdAt: string;
}

/**
 * 权限
 */
export interface Permission {
  id: number;
  code: string;
  name: string;
  description?: string;
  group: string;
  isEnabled: boolean;
}

/**
 * 权限分组
 */
export interface PermissionGroup {
  group: string;
  permissions: Permission[];
}

/**
 * 角色查询参数
 */
export interface RoleQueryParameters {
  page?: number;
  pageSize?: number;
  keyword?: string;
  isEnabled?: boolean;
}

/**
 * 创建角色请求
 */
export interface CreateRoleRequest {
  code: string;
  name: string;
  description?: string;
  sortOrder?: number;
  permissionIds?: number[];
}

/**
 * 更新角色请求
 */
export interface UpdateRoleRequest {
  name?: string;
  description?: string;
  isEnabled?: boolean;
  sortOrder?: number;
  permissionIds?: number[];
}

/**
 * 分配角色请求
 */
export interface AssignRoleRequest {
  userId: number;
  roleIds: number[];
}

/**
 * 分配权限请求
 */
export interface AssignPermissionRequest {
  userId: number;
  permissionCodes: string[];
  isGranted: boolean;
}

/**
 * 用户权限详情
 */
export interface UserPermissionDetail {
  userId: number;
  userName: string;
  roles: Role[];
  grantedPermissions: Permission[];
  revokedPermissions: Permission[];
  effectivePermissions: string[];
}

// ==================== 评论相关类型 ====================

/**
 * 任务评论
 */
export interface TaskComment {
  id: number;
  taskId: number;
  userId: number;
  userName: string;
  userAvatar?: string;
  content: string;
  parentCommentId?: number;
  replies: TaskComment[];
  createdAt: string;
  updatedAt?: string;
}

/**
 * 创建评论请求
 */
export interface CreateCommentRequest {
  content: string;
  parentCommentId?: number;
}

/**
 * 更新评论请求
 */
export interface UpdateCommentRequest {
  content: string;
}
