import { apiClient } from './client'
import type { TaskList, CreateTaskListRequest, Task, PagedResponse } from '../types'

export const taskListApi = {
  async getTaskLists(): Promise<TaskList[]> {
    const response = await apiClient.get<TaskList[]>('/tasklists')
    return response.data
  },

  async getTaskList(id: number): Promise<TaskList> {
    const response = await apiClient.get<TaskList>(`/tasklists/${id}`)
    return response.data
  },

  async createTaskList(request: CreateTaskListRequest): Promise<TaskList> {
    const response = await apiClient.post<TaskList>('/tasklists', request)
    return response.data
  },

  async updateTaskList(id: number, request: CreateTaskListRequest): Promise<TaskList> {
    const response = await apiClient.put<TaskList>(`/tasklists/${id}`, request)
    return response.data
  },

  async deleteTaskList(id: number): Promise<void> {
    await apiClient.delete(`/tasklists/${id}`)
  },

  async getTaskListTasks(
    id: number,
    page: number = 1,
    pageSize: number = 20
  ): Promise<PagedResponse<Task>> {
    const response = await apiClient.get<PagedResponse<Task>>(`/tasklists/${id}/tasks`, {
      params: { page, pageSize },
    })
    return response.data
  },
}
