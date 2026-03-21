import { apiClient } from './client'
import type {
  Task,
  PagedResponse,
  TaskSearchParams,
  CreateTaskRequest,
  UpdateTaskRequest,
  TaskComment,
  CreateCommentRequest,
  UpdateCommentRequest,
} from '../types'

export const taskApi = {
  async getTasks(params: TaskSearchParams): Promise<PagedResponse<Task>> {
    const backendParams: Record<string, unknown> = {
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
      sortBy: params.sortBy,
      isDescending: params.sortDescending ?? true,
      keyword: params.keyword || undefined,
      priority: params.priority,
      assigneeId: params.assigneeId,
      taskListGuid: params.taskListId,
      dueTimeFrom: params.dueDateFrom,
      dueTimeTo: params.dueDateTo,
    }
    if (params.status === 'pending') {
      backendParams.isCompleted = false
    } else if (params.status === 'completed') {
      backendParams.isCompleted = true
    } else if (params.includeCompleted !== undefined) {
      backendParams.isCompleted = params.includeCompleted ? undefined : false
    }
    Object.keys(backendParams).forEach(key => {
      if (backendParams[key] === undefined || backendParams[key] === '') {
        delete backendParams[key]
      }
    })
    const response = await apiClient.get<PagedResponse<Task>>('/tasks', { params: backendParams })
    return response.data
  },

  async getTask(id: number): Promise<Task> {
    const response = await apiClient.get<Task>(`/tasks/${id}`)
    return response.data
  },

  async createTask(request: CreateTaskRequest): Promise<Task> {
    // 字段映射：前端 assignees -> 后端 assigneeIds
    const backendRequest = {
      ...request,
      assigneeIds: request.assignees,
    }
    delete (backendRequest as Record<string, unknown>).assignees
    const response = await apiClient.post<Task>('/tasks', backendRequest)
    return response.data
  },

  async updateTask(id: number, request: UpdateTaskRequest): Promise<Task> {
    const response = await apiClient.put<Task>(`/tasks/${id}`, request)
    return response.data
  },

  async deleteTask(id: number): Promise<void> {
    await apiClient.delete(`/tasks/${id}`)
  },

  async completeTask(id: number): Promise<Task> {
    const response = await apiClient.put<Task>(`/tasks/${id}/status`, { isCompleted: true })
    return response.data
  },

  async assignTask(id: number, assigneeIds: string[], followerIds?: string[]): Promise<boolean> {
    const response = await apiClient.post<boolean>(`/tasks/${id}/assign`, { assigneeIds, followerIds })
    return response.data
  },

  async searchTasks(params: TaskSearchParams): Promise<PagedResponse<Task>> {
    const response = await apiClient.get<PagedResponse<Task>>('/tasks/search', { params })
    return response.data
  },

  async getComments(taskId: number): Promise<TaskComment[]> {
    const response = await apiClient.get<TaskComment[]>(`/tasks/${taskId}/comments`)
    return response.data
  },

  async createComment(taskId: number, request: CreateCommentRequest): Promise<TaskComment> {
    const response = await apiClient.post<TaskComment>(`/tasks/${taskId}/comments`, request)
    return response.data
  },

  async updateComment(taskId: number, commentId: number, request: UpdateCommentRequest): Promise<TaskComment> {
    const response = await apiClient.put<TaskComment>(`/tasks/${taskId}/comments/${commentId}`, request)
    return response.data
  },

  async deleteComment(taskId: number, commentId: number): Promise<void> {
    await apiClient.delete(`/tasks/${taskId}/comments/${commentId}`)
  },
}
