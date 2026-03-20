import { apiClient } from './client'
import type { TaskTemplate, CreateTaskTemplateRequest, Task } from '../types'

export const templateApi = {
  async getTemplates(): Promise<TaskTemplate[]> {
    const response = await apiClient.get<TaskTemplate[]>('/templates')
    return response.data
  },

  async getTemplate(id: number): Promise<TaskTemplate> {
    const response = await apiClient.get<TaskTemplate>(`/templates/${id}`)
    return response.data
  },

  async createTemplate(request: CreateTaskTemplateRequest): Promise<TaskTemplate> {
    const response = await apiClient.post<TaskTemplate>('/templates', request)
    return response.data
  },

  async updateTemplate(id: number, request: CreateTaskTemplateRequest): Promise<TaskTemplate> {
    const response = await apiClient.put<TaskTemplate>(`/templates/${id}`, request)
    return response.data
  },

  async deleteTemplate(id: number): Promise<void> {
    await apiClient.delete(`/templates/${id}`)
  },

  async createTaskFromTemplate(
    templateId: number,
    summary?: string,
    assignees?: string[]
  ): Promise<Task> {
    const response = await apiClient.post<Task>(`/templates/${templateId}/create-task`, {
      summary,
      assignees,
    })
    return response.data
  },
}
