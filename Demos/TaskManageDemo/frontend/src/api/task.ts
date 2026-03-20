import { apiClient } from './client'
import type {
  Task,
  PagedResponse,
  TaskSearchParams,
  CreateTaskRequest,
  UpdateTaskRequest,
} from '../types'

export const taskApi = {
  async getTasks(params: TaskSearchParams): Promise<PagedResponse<Task>> {
    const response = await apiClient.get<PagedResponse<Task>>('/tasks', { params })
    return response.data
  },

  async getTask(id: number): Promise<Task> {
    const response = await apiClient.get<Task>(`/tasks/${id}`)
    return response.data
  },

  async createTask(request: CreateTaskRequest): Promise<Task> {
    const response = await apiClient.post<Task>('/tasks', request)
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
}
