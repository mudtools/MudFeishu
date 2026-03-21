<template>
  <div class="task-list-page">
    <!-- 页面标题和统计 -->
    <div class="page-header">
      <div class="header-content">
        <h1 class="page-title">任务列表</h1>
        <p class="page-subtitle">管理和跟踪您的所有任务</p>
      </div>
      <div class="header-actions">
        <el-button type="primary" size="large" @click="showCreateDialog">
          <el-icon>
            <Plus />
          </el-icon>
          新建任务
        </el-button>
      </div>
    </div>

    <!-- 统计卡片 -->
    <div class="stats-row">
      <el-card class="stat-card" shadow="hover">
        <div class="stat-content">
          <div class="stat-icon total">
            <el-icon>
              <Document />
            </el-icon>
          </div>
          <div class="stat-info">
            <span class="stat-value">{{ taskStore.total }}</span>
            <span class="stat-label">总任务</span>
          </div>
        </div>
      </el-card>
      <el-card class="stat-card" shadow="hover">
        <div class="stat-content">
          <div class="stat-icon pending">
            <el-icon>
              <Timer />
            </el-icon>
          </div>
          <div class="stat-info">
            <span class="stat-value">{{ pendingCount }}</span>
            <span class="stat-label">进行中</span>
          </div>
        </div>
      </el-card>
      <el-card class="stat-card" shadow="hover">
        <div class="stat-content">
          <div class="stat-icon completed">
            <el-icon>
              <CircleCheck />
            </el-icon>
          </div>
          <div class="stat-info">
            <span class="stat-value">{{ completedCount }}</span>
            <span class="stat-label">已完成</span>
          </div>
        </div>
      </el-card>
      <el-card class="stat-card" shadow="hover">
        <div class="stat-content">
          <div class="stat-icon overdue">
            <el-icon>
              <Warning />
            </el-icon>
          </div>
          <div class="stat-info">
            <span class="stat-value">{{ overdueCount }}</span>
            <span class="stat-label">已逾期</span>
          </div>
        </div>
      </el-card>
    </div>

    <!-- 搜索和筛选 -->
    <el-card class="search-card" shadow="never">
      <TaskSearchForm v-model:keyword="searchForm.keyword" v-model:status="searchForm.status" v-model:priority="searchForm.priority" @search="handleSearch" @reset="handleReset" />
    </el-card>

    <!-- 任务列表 -->
    <el-card class="task-card" shadow="never">
      <template #header>
        <div class="card-header">
          <div class="header-left">
            <span class="header-title">任务列表</span>
            <el-tag type="info" size="small">{{ taskStore.total }} 个任务</el-tag>
          </div>
          <div class="header-right">
            <el-radio-group v-model="viewMode" size="small">
              <el-radio-button value="list">
                <el-icon>
                  <List />
                </el-icon>
              </el-radio-button>
              <el-radio-button value="grid">
                <el-icon>
                  <Grid />
                </el-icon>
              </el-radio-button>
            </el-radio-group>
            <el-divider direction="vertical" />
            <el-dropdown trigger="click">
              <el-button text size="small">
                <el-icon>
                  <Sort />
                </el-icon>
                排序
              </el-button>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item>按创建时间</el-dropdown-item>
                  <el-dropdown-item>按截止时间</el-dropdown-item>
                  <el-dropdown-item>按优先级</el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
        </div>
      </template>

      <TableContainer :loading="taskStore.loading" :data="taskStore.tasks" :show-empty="true" empty-title="暂无任务" empty-description="还没有创建任何任务，点击上方按钮创建新任务" :show-pagination="true" :current-page="currentPage" :page-size="pageSize"
                      :total="taskStore.total" @page-change="handlePageChange" @size-change="handleSizeChange">
        <template #default>
          <TaskTable :tasks="taskStore.tasks" :view-mode="viewMode" @row-click="handleRowClick" @edit="handleEdit" @delete="handleDelete" @toggle-complete="handleToggleComplete" />
        </template>

        <template #empty-action>
          <el-button type="primary" @click="showCreateDialog">
            <el-icon>
              <Plus />
            </el-icon>
            创建第一个任务
          </el-button>
        </template>
      </TableContainer>
    </el-card>

    <!-- 创建/编辑对话框 -->
    <el-dialog v-model="createDialogVisible" :title="editingTask ? '编辑任务' : '新建任务'" width="600px" destroy-on-close class="task-dialog">
      <el-form ref="taskFormRef" :model="taskForm" :rules="taskRules" label-position="top" class="task-form">
        <el-form-item label="任务标题" prop="summary">
          <el-input v-model="taskForm.summary" placeholder="请输入任务标题" size="large" />
        </el-form-item>
        <el-form-item label="任务描述" prop="description">
          <el-input v-model="taskForm.description" type="textarea" :rows="3" placeholder="请输入任务描述" />
        </el-form-item>
        <div class="form-row">
          <el-form-item label="优先级" prop="priority" class="form-col">
            <el-select v-model="taskForm.priority" placeholder="选择优先级" style="width: 100%">
              <el-option label="低" :value="1" />
              <el-option label="中" :value="2" />
              <el-option label="高" :value="3" />
              <el-option label="紧急" :value="4" />
            </el-select>
          </el-form-item>
          <el-form-item label="截止时间" prop="dueTime" class="form-col">
            <el-date-picker v-model="taskForm.dueTime" type="datetime" placeholder="选择截止时间" format="YYYY-MM-DD HH:mm" value-format="YYYY-MM-DDTHH:mm:ss" style="width: 100%" />
          </el-form-item>
        </div>
      </el-form>
      <template #footer>
        <div class="dialog-footer">
          <el-button @click="createDialogVisible = false">取消</el-button>
          <el-button type="primary" :loading="submitting" @click="handleSubmit">
            {{ editingTask ? '保存' : '创建' }}
          </el-button>
        </div>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed } from "vue"
import { useRouter } from "vue-router"
import { ElMessage, ElMessageBox } from "element-plus"
import type { FormInstance, FormRules } from "element-plus"
import { useTaskStore } from "../stores/task"
import type {
  Task,
  TaskSearchParams,
  CreateTaskRequest,
  UpdateTaskRequest,
} from "../types"
import { TaskSearchForm, TableContainer, TaskTable } from "../components"
import dayjs from "dayjs"
import {
  Plus,
  Document,
  Timer,
  CircleCheck,
  Warning,
  List,
  Grid,
  Sort,
} from "@element-plus/icons-vue"

const router = useRouter()
const taskStore = useTaskStore()

const searchForm = reactive<TaskSearchParams>({
  keyword: "",
  status: "all",
  priority: 0,
  page: 1,
  pageSize: 20,
})

const currentPage = ref(1)
const pageSize = ref(20)
const viewMode = ref<"list" | "grid">("list")

const createDialogVisible = ref(false)
const editingTask = ref<Task | null>(null)
const submitting = ref(false)
const taskFormRef = ref<FormInstance>()

const taskForm = reactive<CreateTaskRequest & { id?: number }>({
  summary: "",
  description: "",
  priority: 2,
  dueTime: "",
})

const taskRules: FormRules = {
  summary: [{ required: true, message: "请输入任务标题", trigger: "blur" }],
}

// 统计数据
const pendingCount = computed(
  () => taskStore.tasks.filter((t) => !t.isCompleted).length
)
const completedCount = computed(
  () => taskStore.tasks.filter((t) => t.isCompleted).length
)
const overdueCount = computed(
  () => taskStore.tasks.filter((t) => isOverdue(t)).length
)

const isOverdue = (task: Task) => {
  if (task.isCompleted || !task.dueTime) return false
  return dayjs(task.dueTime).isBefore(dayjs())
}

const handleSearch = () => {
  currentPage.value = 1
  taskStore.fetchTasks({ ...searchForm, page: 1 })
}

const handleReset = () => {
  searchForm.keyword = ""
  searchForm.status = "all"
  searchForm.priority = 0
  handleSearch()
}

const handleSizeChange = (size: number) => {
  pageSize.value = size
}

const handlePageChange = (page: number) => {
  currentPage.value = page
}

// @ts-ignore - used by TableContainer component
const handlePaginationChange = () => {
  taskStore.fetchTasks({
    ...searchForm,
    page: currentPage.value,
    pageSize: pageSize.value,
  })
}

const handleRowClick = (row: Task) => {
  router.push(`/tasks/${row.id}`)
}

const showCreateDialog = () => {
  editingTask.value = null
  Object.assign(taskForm, {
    summary: "",
    description: "",
    priority: 2,
    dueTime: "",
  })
  createDialogVisible.value = true
}

const handleEdit = (task: Task) => {
  editingTask.value = task
  Object.assign(taskForm, {
    id: task.id,
    summary: task.summary,
    description: task.description || "",
    priority: task.priority,
    dueTime: task.dueTime || "",
  })
  createDialogVisible.value = true
}

const handleSubmit = async () => {
  if (!taskFormRef.value) return
  await taskFormRef.value.validate(async (valid) => {
    if (!valid) return

    submitting.value = true
    try {
      if (editingTask.value) {
        const request: UpdateTaskRequest = {
          summary: taskForm.summary,
          description: taskForm.description,
          priority: taskForm.priority,
          dueTime: taskForm.dueTime || undefined,
        }
        await taskStore.updateTask(editingTask.value.id, request)
        ElMessage.success("任务更新成功")
      } else {
        const request: CreateTaskRequest = {
          summary: taskForm.summary,
          description: taskForm.description,
          priority: taskForm.priority,
          dueTime: taskForm.dueTime || undefined,
        }
        await taskStore.createTask(request)
        ElMessage.success("任务创建成功")
      }
      createDialogVisible.value = false
    } catch {
      ElMessage.error(editingTask.value ? "任务更新失败" : "任务创建失败")
    } finally {
      submitting.value = false
    }
  })
}

const toggleComplete = async (task: Task) => {
  try {
    if (task.isCompleted) {
      // 取消完成
      await taskStore.updateTask(task.id, { isCompleted: false })
      ElMessage.success("任务已恢复")
    } else {
      await taskStore.completeTask(task.id)
      ElMessage.success("任务已完成")
    }
  } catch {
    ElMessage.error("操作失败")
  }
}

// @ts-ignore - kept for potential future use
const handleCommand = (command: string, task: Task) => {
  switch (command) {
    case "edit":
      handleEdit(task)
      break
    case "complete":
      toggleComplete(task)
      break
    case "delete":
      handleDelete(task)
      break
  }
}

const handleToggleComplete = async (task: Task, completed: boolean) => {
  try {
    if (completed) {
      await taskStore.completeTask(task.id)
      ElMessage.success("任务已完成")
    } else {
      await taskStore.updateTask(task.id, { isCompleted: false })
      ElMessage.success("任务已恢复")
    }
  } catch {
    ElMessage.error("操作失败")
  }
}

const handleDelete = async (task: Task) => {
  try {
    await ElMessageBox.confirm(
      "确定要删除此任务吗？此操作不可恢复。",
      "确认删除",
      {
        type: "warning",
      }
    )
    await taskStore.deleteTask(task.id)
    ElMessage.success("任务已删除")
  } catch {
    // User cancelled
  }
}

onMounted(() => {
  taskStore.fetchTasks(searchForm)
})
</script>

<style scoped>
.task-list-page {
  padding: 0;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 24px;
}

.header-content {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.page-title {
  font-size: 28px;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
}

.page-subtitle {
  font-size: 14px;
  color: var(--text-secondary);
  margin: 0;
}

.header-actions {
  display: flex;
  gap: 12px;
}

.stats-row {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16px;
  margin-bottom: 24px;
}

.stat-card {
  border-radius: var(--radius-lg);
}

.stat-card :deep(.el-card__body) {
  padding: 20px;
}

.stat-content {
  display: flex;
  align-items: center;
  gap: 16px;
}

.stat-icon {
  width: 48px;
  height: 48px;
  border-radius: var(--radius-lg);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 24px;
}

.stat-icon.total {
  background: linear-gradient(
    135deg,
    var(--primary-color) 0%,
    var(--primary-light) 100%
  );
  color: white;
}

.stat-icon.pending {
  background: linear-gradient(135deg, var(--warning-color) 0%, #fbbf24 100%);
  color: white;
}

.stat-icon.completed {
  background: linear-gradient(135deg, var(--success-color) 0%, #34d399 100%);
  color: white;
}

.stat-icon.overdue {
  background: linear-gradient(135deg, var(--danger-color) 0%, #f87171 100%);
  color: white;
}

.stat-info {
  display: flex;
  flex-direction: column;
}

.stat-value {
  font-size: 24px;
  font-weight: 700;
  color: var(--text-primary);
}

.stat-label {
  font-size: 13px;
  color: var(--text-secondary);
}

.search-card {
  margin-bottom: 20px;
  border-radius: var(--radius-lg);
}

.task-card {
  border-radius: var(--radius-lg);
}

.task-card :deep(.el-card__header) {
  padding: 16px 20px;
  border-bottom: 1px solid var(--border-light);
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.header-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
}

.header-right {
  display: flex;
  align-items: center;
  gap: 12px;
}

/* 列表视图 */
.task-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.task-item {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 16px;
  border-radius: var(--radius-md);
  background: var(--bg-card);
  border: 1px solid var(--border-light);
  transition: all var(--transition-fast);
  cursor: pointer;
}

.task-item:hover {
  border-color: var(--primary-color);
  box-shadow: var(--shadow-md);
}

.task-item.completed {
  opacity: 0.7;
  background: var(--bg-secondary);
}

.task-item.overdue {
  border-left: 4px solid var(--danger-color);
}

.task-checkbox {
  flex-shrink: 0;
}

.task-content {
  flex: 1;
  min-width: 0;
}

.task-header {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
}

.task-title {
  font-size: 15px;
  font-weight: 500;
  color: var(--text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.task-meta {
  display: flex;
  align-items: center;
  gap: 16px;
  flex-wrap: wrap;
}

.meta-item {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 13px;
  color: var(--text-secondary);
}

.meta-item .el-icon {
  font-size: 14px;
}

.task-assignees {
  display: flex;
  align-items: center;
  flex-shrink: 0;
}

.member-avatar {
  margin-left: -8px;
  border: 2px solid var(--bg-card);
}

.member-avatar:first-child {
  margin-left: 0;
}

.task-actions {
  display: flex;
  align-items: center;
  gap: 4px;
  opacity: 0;
  transition: opacity var(--transition-fast);
}

.task-item:hover .task-actions {
  opacity: 1;
}

/* 网格视图 */
.task-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 16px;
}

.task-card-item {
  border-radius: var(--radius-lg);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.task-card-item:hover {
  transform: translateY(-2px);
  box-shadow: var(--shadow-lg);
}

.task-card-item.completed {
  opacity: 0.7;
}

.task-card-item.overdue {
  border-top: 4px solid var(--danger-color);
}

.task-card-item :deep(.el-card__body) {
  padding: 16px;
}

.task-card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.task-card-title {
  font-size: 15px;
  font-weight: 500;
  color: var(--text-primary);
  margin-bottom: 12px;
  line-height: 1.5;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.task-card-meta {
  margin-bottom: 16px;
}

.task-card-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding-top: 12px;
  border-top: 1px solid var(--border-light);
}

/* 对话框 */
.task-dialog :deep(.el-dialog__header) {
  padding: 20px 24px;
  border-bottom: 1px solid var(--border-light);
}

.task-dialog :deep(.el-dialog__body) {
  padding: 24px;
}

.task-dialog :deep(.el-dialog__footer) {
  padding: 16px 24px;
  border-top: 1px solid var(--border-light);
}

.task-form :deep(.el-form-item__label) {
  font-weight: 500;
  color: var(--text-primary);
  padding-bottom: 8px;
}

.form-row {
  display: flex;
  gap: 16px;
}

.form-col {
  flex: 1;
}

.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}

.text-muted {
  color: var(--text-muted);
}

.text-danger {
  color: var(--danger-color);
}
</style>
