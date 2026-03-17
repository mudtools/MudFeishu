import axios from 'axios'
import type { User, Space, Node, FavoriteNode, PagedResponse, ApiResponse, LoginResponse, AuthUrlResponse, SearchRequest, CreateDocumentRequest } from '@/types'

const api = axios.create({
  baseURL: '/api',
  timeout: 30000,
})

let isRefreshing = false
let refreshSubscribers: ((token: string) => void)[] = []

function subscribeTokenRefresh(callback: (token: string) => void) {
  refreshSubscribers.push(callback)
}

function onTokenRefreshed(token: string) {
  refreshSubscribers.forEach(callback => callback(token))
  refreshSubscribers = []
}

api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => Promise.reject(error)
)

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config
    
    if (error.response?.status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        return new Promise((resolve) => {
          subscribeTokenRefresh((token) => {
            originalRequest.headers.Authorization = `Bearer ${token}`
            resolve(api(originalRequest))
          })
        })
      }
      
      originalRequest._retry = true
      isRefreshing = true
      
      try {
        const response = await api.post('/oauth/refresh')
        
        if (response.data.success) {
          const newToken = response.data.accessToken
          localStorage.setItem('token', newToken)
          onTokenRefreshed(newToken)
          originalRequest.headers.Authorization = `Bearer ${newToken}`
          return api(originalRequest)
        }
      } catch (refreshError) {
        localStorage.removeItem('token')
        window.location.href = '/login'
        return Promise.reject(refreshError)
      } finally {
        isRefreshing = false
      }
    }
    
    if (error.response?.status === 401) {
      localStorage.removeItem('token')
      window.location.href = '/login'
    }
    
    return Promise.reject(error)
  }
)

export const authApi = {
  getAuthUrl: () => api.get<AuthUrlResponse>('/oauth/feishu/url'),
  callback: (code: string, state: string) => 
    api.post<LoginResponse>('/oauth/feishu/callback', { code, state }),
  getMe: () => api.get<User>('/oauth/me'),
  logout: () => api.post<ApiResponse<void>>('/oauth/logout'),
  getStatus: () => api.get<ApiResponse<{ hasValidToken: boolean; canRefresh: boolean }>>('/oauth/status'),
  refreshToken: () => api.post<ApiResponse<{ accessToken: string }>>('/oauth/refresh'),
}

export const wikiApi = {
  getSpaces: (pageSize = 20, pageToken?: string) =>
    api.get<PagedResponse<Space>>('/wiki/spaces', { params: { pageSize, pageToken } }),
  
  getSpaceInfo: (spaceId: string) =>
    api.get<ApiResponse<Space>>(`/wiki/spaces/${spaceId}`),
  
  createSpace: (title: string, description?: string) =>
    api.post<ApiResponse<Space>>('/wiki/spaces', { title, description }),
  
  getNodeTree: (spaceId: string, parentNodeToken?: string, pageSize = 50, pageToken?: string) =>
    api.get<PagedResponse<Node>>(`/wiki/nodes/tree/${spaceId}`, {
      params: { parentNodeToken, pageSize, pageToken }
    }),
  
  getNodeInfo: (nodeToken: string) =>
    api.get<ApiResponse<Node>>(`/wiki/nodes/${nodeToken}`),
  
  createNode: (spaceId: string, request: CreateDocumentRequest) =>
    api.post<ApiResponse<Node>>(`/wiki/nodes/${spaceId}`, request),
  
  updateNodeTitle: (spaceId: string, nodeToken: string, title: string) =>
    api.put<ApiResponse<void>>(`/wiki/nodes/${spaceId}/${nodeToken}/title`, { title }),
  
  moveNode: (spaceId: string, nodeToken: string, targetParentToken?: string) =>
    api.post<ApiResponse<Node>>(`/wiki/nodes/${spaceId}/${nodeToken}/move`, { targetParentToken }),
  
  search: (request: SearchRequest) =>
    api.post<PagedResponse<any>>('/wiki/nodes/search', request),
  
  getFavorites: () =>
    api.get<ApiResponse<FavoriteNode[]>>('/wiki/nodes/favorites'),
  
  addFavorite: (spaceId: string, nodeToken: string, title: string, objToken?: string, objType?: string) =>
    api.post<ApiResponse<void>>('/wiki/nodes/favorites', {
      spaceId, nodeToken, title, objToken, objType
    }),
  
  removeFavorite: (nodeToken: string) =>
    api.delete<ApiResponse<void>>(`/wiki/nodes/favorites/${nodeToken}`),
}

export default api
