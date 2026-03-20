export interface ApiResponse<T> {
  success: boolean;
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
  members?: TaskMember[];
  checkItems?: TaskCheckItem[];
}

export interface TaskMember {
  id: number;
  taskId: number;
  userId: number;
  role: string;
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
