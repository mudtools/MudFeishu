export interface User {
  openId: string
  unionId: string
  name: string
  avatar?: string
  email?: string
}

export interface Space {
  spaceId: string
  name: string
  description?: string
  spaceType: string
  visibility: string
  nodeCount?: number
  lastModifiedTime?: string
}

export interface Node {
  nodeToken: string
  objToken: string
  objType: string
  title: string
  parentNodeToken?: string
  hasChildren: boolean
  children?: Node[]
  icon?: string
  createTime?: string
  editTime?: string
  creator?: string
}

export interface FavoriteNode {
  id: string
  spaceId: string
  nodeToken: string
  objToken?: string
  title: string
  objType?: string
  createdAt: string
}

export interface PagedResponse<T> {
  items: T[]
  hasMore: boolean
  pageToken?: string
}

export interface ApiResponse<T> {
  success: boolean
  message?: string
  data?: T
}

export interface LoginResponse {
  success: boolean
  message?: string
  token?: string
  user?: User
}

export interface AuthUrlResponse {
  success: boolean
  message?: string
  url?: string
  state?: string
}

export interface SearchRequest {
  query: string
  spaceId?: string
  nodeId?: string
  pageSize?: number
}

export interface CreateDocumentRequest {
  spaceId: string
  parentNodeToken?: string
  title: string
  objType: string
}
