import api from './auth'

// ===================== 数据模型 =====================

/** 部门负责人 */
export interface DepartmentLeader {
  leaderType: number
  leaderID: string | null
}

/** 部门状态 */
export interface DepartmentStatus {
  is_deleted: boolean
}

/** 部门国际化名称 */
export interface I18nName {
  zh_cn?: string
  en_us?: string
  ja_jp?: string
  [key: string]: string | undefined
}

/** 部门基础信息（响应模型基类） */
export interface DepartmentBase {
  name: string
  i18n_name: I18nName
  parent_department_id: string
  department_id: string
  open_department_id: string
  leader_user_id: string
  chat_id: string
  order: string
  unit_ids: string[]
  member_count: number
  status: DepartmentStatus
  leaders: DepartmentLeader[]
}

/** 获取部门信息响应模型（含扩展字段） */
export interface GetDepartmentInfo extends DepartmentBase {
  group_chat_employee_types: number[]
  department_hrbps: string[]
  primary_member_count: number
}

/** 部门详细信息（创建/更新结果） */
export interface DepartmentDetail extends DepartmentBase {
  department_hrbps: string[]
}

/** 获取单个部门信息结果 */
export interface GetDepartmentInfoResult {
  department: GetDepartmentInfo | null
}

/** 批量获取部门信息结果 */
export interface BatchGetDepartmentResult {
  items: GetDepartmentInfo[]
}

/** 分页列表结果 */
export interface PageListResult<T> {
  items: T[]
  has_more: boolean
  page_token: string | null
}

/** 飞书 API 响应（含 code/msg/data） */
export interface FeishuApiResult<T> {
  code: number
  msg: string | null
  data: T | null
}

/** 创建/更新部门结果 */
export interface DepartmentCreateUpdateResult {
  department: DepartmentDetail
}

/** 更新部门结果 */
export interface DepartmentUpdateResult {
  department: DepartmentBase
}

// ===================== 请求模型 =====================

/** 创建部门请求 */
export interface DepartmentCreateRequest {
  name: string
  parent_department_id: string
  leader_user_id?: string
  order?: string
  create_group_chat?: boolean
  department_id?: string
  unit_ids?: string[]
  department_hrbps?: string[]
  leaders?: DepartmentLeader[]
  group_chat_employee_types?: number[]
}

/** 部分更新部门请求 */
export interface DepartmentPartUpdateRequest {
  name: string
  parent_department_id: string
  leader_user_id?: string
  order?: string
  create_group_chat?: boolean
  department_hrbps?: string[]
  leaders?: DepartmentLeader[]
  group_chat_employee_types?: number[]
}

/** 完全更新部门请求 */
export interface DepartmentUpdateRequest {
  name: string
  parent_department_id: string
  leader_user_id?: string
  order?: string
  create_group_chat?: boolean
  leaders?: DepartmentLeader[]
  group_chat_employee_types?: number[]
}

/** 更新部门 ID 请求 */
export interface DepartmentUpdateIdRequest {
  new_department_id: string
}

/** 解绑部门群聊请求 */
export interface DepartmentUnbindChatRequest {
  department_id: string
}

// ===================== API 方法 =====================

export const departmentApi = {
  /** 获取子部门列表 */
  getSubDepartments: async (
    departmentId: string,
    fetchChild = false,
    pageSize = 10,
    pageToken: string | null = null
  ): Promise<PageListResult<GetDepartmentInfo>> => {
    const params: Record<string, unknown> = {
      fetchChild,
      pageSize
    }
    if (pageToken) params.pageToken = pageToken
    const response = await api.get<PageListResult<GetDepartmentInfo>>(
      `/department/${departmentId}/children`,
      { params }
    )
    return response.data
  },

  /** 获取单个部门信息 */
  getDepartment: async (departmentId: string): Promise<GetDepartmentInfoResult> => {
    const response = await api.get<GetDepartmentInfoResult>(`/department/${departmentId}`)
    return response.data
  },

  /** 批量获取部门信息 */
  getDepartmentsByIds: async (departmentIds: string[]): Promise<BatchGetDepartmentResult> => {
    const response = await api.get<BatchGetDepartmentResult>('/department/batch', {
      params: { departmentIds }
    })
    return response.data
  },

  /** 获取父部门列表 */
  getParentDepartments: async (
    departmentId: string,
    pageSize = 10,
    pageToken: string | null = null
  ): Promise<PageListResult<GetDepartmentInfo>> => {
    const params: Record<string, unknown> = { pageSize }
    if (pageToken) params.pageToken = pageToken
    const response = await api.get<PageListResult<GetDepartmentInfo>>(
      `/department/${departmentId}/parents`,
      { params }
    )
    return response.data
  },

  /** 创建部门 */
  createDepartment: async (
    data: DepartmentCreateRequest
  ): Promise<FeishuApiResult<DepartmentCreateUpdateResult>> => {
    const response = await api.post<FeishuApiResult<DepartmentCreateUpdateResult>>(
      '/department',
      data
    )
    return response.data
  },

  /** 部分更新部门 */
  updatePartDepartment: async (
    departmentId: string,
    data: DepartmentPartUpdateRequest
  ): Promise<FeishuApiResult<DepartmentCreateUpdateResult>> => {
    const response = await api.patch<FeishuApiResult<DepartmentCreateUpdateResult>>(
      `/department/${departmentId}`,
      data
    )
    return response.data
  },

  /** 完全更新部门 */
  updateDepartment: async (
    departmentId: string,
    data: DepartmentUpdateRequest
  ): Promise<FeishuApiResult<DepartmentUpdateResult>> => {
    const response = await api.put<FeishuApiResult<DepartmentUpdateResult>>(
      `/department/${departmentId}`,
      data
    )
    return response.data
  },

  /** 更新部门 ID */
  updateDepartmentId: async (
    departmentId: string,
    newDepartmentId: string
  ): Promise<FeishuApiResult<null>> => {
    const response = await api.patch<FeishuApiResult<null>>(
      `/department/${departmentId}/update-department-id`,
      { new_department_id: newDepartmentId }
    )
    return response.data
  },

  /** 解绑部门群聊 */
  unbindDepartmentChat: async (
    departmentId: string
  ): Promise<FeishuApiResult<null>> => {
    const response = await api.post<FeishuApiResult<null>>(
      '/department/unbind-department-chat',
      { department_id: departmentId }
    )
    return response.data
  }
}
