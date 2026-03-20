<template>
  <div class="kanban-page">
    <!-- 页面标题 -->
    <div class="page-header">
      <div class="header-content">
        <h1 class="page-title">任务看板</h1>
        <p class="page-subtitle">拖拽任务卡片以更新状态</p>
      </div>
      <div class="header-actions">
        <el-button type="primary" size="large" @click="showCreateDialog">
          <el-icon><Plus /></el-icon>
          新建任务
        </el-button>
      </div>
    </div>

    <!-- 看板 -->
    <div class="kanban-board">
      <div
        v-for="column in columns"
        :key="column.status"
        class="kanban-column"
        :class="{ 'drag-over': dragOverColumn === column.status }"
        @dragover.prevent="handleDragOver(column.status)"
        @dragleave="handleDragLeave"
        @drop="handleDrop($event, column.status)"
      >
        <div class="column-header" :class="`header-${column.status}`">
          <div class="column-title-wrapper">
            <div class="column-icon">
              <el-icon :size="18">
                <component :is="column.icon" />
              </el-icon>
            </div>
            <span class="column-title">{{ column.title }}</span>
            <el-badge :value="getTasksByStatus(column.status).length" class="column-badge" type="primary" />
          </div>
          <el-button text circle size="small" @click="showCreateDialog(column.status)">
            <el-icon><Plus /></el-icon>
          </el-button>
        </div>

        <div class="column-content">
          <transition-group name="task-card" tag="div" class="task-list">
            <div
              v-for="task in getTasksByStatus(column.status)"
              :key="task.id"
              class="task-card"
              :class="{ dragging: draggedTask?.id === task.id, overdue: isOverdue(task) }"
              draggable="true"
              @dragstart="handleDragStart($event, task)"
              @dragend="handleDragEnd"
              @click="goToTask(task.id)"
            >
              <div class="task-card-header">
                <div class="task-card-badges">
                  <TaskPriorityTag :priority="task.priority" />
                  <TaskStatusTag :is-completed="task.isCompleted" :is-overdue="isOverdue(task)" size="small" />
                </div>
                <el-dropdown trigger="click" @command="(cmd: string) => handleTaskCommand(cmd, task)">
                  <el-button text circle size="small" @click.stop>
                    <el-icon><More /></el-icon>
                  </el-button>
                  <template #dropdown>
                    <el-dropdown-menu>
                      <el-dropdown-item command="edit">
                        <el-icon><Edit /></el-icon> 编辑
                      </el-dropdown-item>
                      <el-dropdown-item command="complete" v-if="!task.isCompleted">
                        <el-icon><CircleCheck /></el-icon> 完成
                      </el-dropdown-item>
                      <el-dropdown-item command="delete" divided>
                        <el-icon><Delete /></el-icon> 删除
                      </el-dropdown-item>
                    </el-dropdown-menu>
                  </template>
                </el-dropdown>
              </div>
              
              <h4 class="task-card-title" :class="{ completed: task.isCompleted }">
                {{ task.summary }}
              </h4>
              
              <p v-if="task.description" class="task-card-desc">
                {{ task.description }}
              </p>
              
              <div class="task-card-tags" v-if="task.tags && task.tags.length > 0">
                <el-tag
                  v-for="tag in task.tags.slice(0, 3)"
                  :key="tag"
                  size="small"
                  effect="plain"
                  class="task-tag"
                >
                  {{ tag }}
                </el-tag>
              </div>
              
              <div class="task-card-footer">
                <div v-if="task.dueTime" class="due-time" :class="{ overdue: isOverdue(task) }">
                  <el-icon><Clock /></el-icon>
                  <span>{{ formatDate(task.dueTime) }}</span>
                </div>
                <div v-else></div>
                <div v-if="task.members && task.members.length > 0" class="members">
                  <UserAvatar
                    v-for="member in task.members.filter((m: any) => m.role === 'assignee').slice(0, 2)"
                    :key="member.id"
                    :name="member.name || member.user?.name || 'U'"
                    :avatar-url="member.avatarUrl || member.user?.avatarUrl"
                    :size="24"
                    class="member-avatar"
                  />
                </div>
              </div>
            </div>
          </transition-group>

          <div v-if="getTasksByStatus(column.status).length === 0" class="empty-column">
            <el-empty description="暂无任务" :image-size="80">
              <el-button type="primary" text @click="showCreateDialog(column.status)">
                添加任务
              </el-button>
            </el-empty>
          </div>
        </div>
      </div>
    </div>

    <!-- 新建任务对话框 -->
    <el-dialog
      v-model="createDialogVisible"
      title="新建任务"
      width="500px"
      destroy-on-close
      class="kanban-dialog"
    >
      <el-form
        ref="formRef"
        :model="taskForm"
        :rules="formRules"
        label-position="top"
        class="task-form"
      >
        <el-form-item label="任务标题" prop="summary">
          <el-input v-model="taskForm.summary" placeholder="请输入任务标题" size="large" />
        </el-form-item>
        <el-form-item label="任务描述" prop="description">
          <el-input
            v-model="taskForm.description"
            type="textarea"
            :rows="3"
            placeholder="请输入任务描述（可选）"
          />
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
            <el-date-picker
              v-model="taskForm.dueTime"
              type="datetime"
              placeholder="选择截止时间"
              format="YYYY-MM-DD HH:mm"
              value-format="YYYY-MM-DDTHH:mm:ss"
              style="width: 100%"
            />
          </el-form-item>
        </div>
      </el-form>
      <template #footer>
        <div class="dialog-footer">
          <el-button @click="createDialogVisible = false">取消</el-button>
          <el-button type="primary" :loading="submitting" @click="handleCreate">创建</el-button>
        </div>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import {
  Plus,
  Clock,
  More,
  Edit,
  CircleCheck,
  Delete
} from '@element-plus/icons-vue'
import { useTaskStore } from '../stores/task'
import type { Task, CreateTaskRequest } from '../types'
import { TaskPriorityTag, UserAvatar, TaskStatusTag } from '../components'
import dayjs from 'dayjs'

const router = useRouter()
const taskStore = useTaskStore()

const columns = [
  { status: 'pending', title: '待处理', icon: 'Timer' },
  { status: 'in_progress', title: '进行中', icon: 'Loading' },
  { status: 'completed', title: '已完成', icon: 'CircleCheckFilled' },
]

const createDialogVisible = ref(false)
const submitting = ref(false)
const formRef = ref<FormInstance>()
const draggedTask = ref<Task | null>(null)
const dragOverColumn = ref<string | null>(null)

const taskForm = reactive<CreateTaskRequest>({
  summary: '',
  priority: 2,
})

const formRules: FormRules = {
  summary: [{ required: true, message: '请输入任务标题', trigger: 'blur' }],
}

const formatDate = (date: string) => dayjs(date).format('MM-DD HH:mm')

const isOverdue = (task: Task) => {
  if (task.isCompleted || !task.dueTime) return false
  return dayjs(task.dueTime).isBefore(dayjs())
}

const getTasksByStatus = (status: string): Task[] => {
  if (status === 'completed') {
    return taskStore.tasks.filter((t) => t.isCompleted)
  }
  return taskStore.tasks.filter((t) => !t.isCompleted)
}

const handleDragStart = (_event: DragEvent, task: Task) => {
  draggedTask.value = task
}

const handleDragEnd = () => {
  draggedTask.value = null
  dragOverColumn.value = null
}

const handleDragOver = (status: string) => {
  dragOverColumn.value = status
}

const handleDragLeave = () => {
  dragOverColumn.value = null
}

const handleDrop = async (_event: DragEvent, status: string) => {
  dragOverColumn.value = null
  if (!draggedTask.value) return

  const shouldComplete = status === 'completed'
  if (shouldComplete !== draggedTask.value.isCompleted) {
    try {
      if (shouldComplete) {
        await taskStore.completeTask(draggedTask.value.id)
        ElMessage.success('任务已完成')
      }
    } catch {
      ElMessage.error('操作失败')
    }
  }
  draggedTask.value = null
}

const goToTask = (id: number) => router.push(`/tasks/${id}`)

const showCreateDialog = (_status?: string) => {
  Object.assign(taskForm, { summary: '', priority: 2, dueTime: '' })
  createDialogVisible.value = true
}

const handleTaskCommand = async (command: string, task: Task) => {
  switch (command) {
    case 'edit':
      router.push(`/tasks/${task.id}`)
      break
    case 'complete':
      try {
        await taskStore.completeTask(task.id)
        ElMessage.success('任务已完成')
      } catch {
        ElMessage.error('操作失败')
      }
      break
    case 'delete':
      try {
        await ElMessageBox.confirm('确定要删除此任务吗？', '确认删除', { type: 'warning' })
        await taskStore.deleteTask(task.id)
        ElMessage.success('任务已删除')
      } catch {
        // User cancelled
      }
      break
  }
}

const handleCreate = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    submitting.value = true
    try {
      await taskStore.createTask(taskForm)
      ElMessage.success('任务创建成功')
      createDialogVisible.value = false
    } catch {
      ElMessage.error('任务创建失败')
    } finally {
      submitting.value = false
    }
  })
}

onMounted(() => {
  taskStore.fetchTasks({ includeCompleted: true, pageSize: 100 })
})
</script>

<style scoped>
.kanban-page {
  height: calc(100vh - 120px);
  display: flex;
  flex-direction: column;
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

.kanban-board {
  display: flex;
  gap: 20px;
  flex: 1;
  overflow-x: auto;
  padding-bottom: 8px;
}

.kanban-column {
  flex: 1;
  min-width: 320px;
  max-width: 380px;
  background: var(--bg-tertiary);
  border-radius: var(--radius-xl);
  display: flex;
  flex-direction: column;
  border: 2px solid transparent;
  transition: all var(--transition-fast);
}

.kanban-column.drag-over {
  border-color: var(--primary-color);
  background: var(--primary-bg);
}

.column-header {
  padding: 16px 20px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-radius: var(--radius-xl) var(--radius-xl) 0 0;
  background: var(--bg-card);
  border-bottom: 1px solid var(--border-light);
}

.column-header.header-pending {
  border-top: 4px solid var(--warning-color);
}

.column-header.header-in_progress {
  border-top: 4px solid var(--primary-color);
}

.column-header.header-completed {
  border-top: 4px solid var(--success-color);
}

.column-title-wrapper {
  display: flex;
  align-items: center;
  gap: 10px;
}

.column-icon {
  width: 32px;
  height: 32px;
  border-radius: var(--radius-md);
  background: var(--bg-tertiary);
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--text-secondary);
}

.column-title {
  font-weight: 600;
  font-size: 15px;
  color: var(--text-primary);
}

.column-badge :deep(.el-badge__content) {
  background: var(--primary-color);
  border: none;
  font-weight: 500;
}

.column-content {
  padding: 16px;
  flex: 1;
  overflow-y: auto;
}

.task-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.task-card {
  background: var(--bg-card);
  border-radius: var(--radius-lg);
  padding: 16px;
  cursor: pointer;
  transition: all var(--transition-fast);
  border: 1px solid var(--border-light);
  box-shadow: var(--shadow-sm);
}

.task-card:hover {
  box-shadow: var(--shadow-md);
  transform: translateY(-2px);
}

.task-card.dragging {
  opacity: 0.5;
}

.task-card.overdue {
  border-left: 4px solid var(--danger-color);
}

.task-card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.task-card-badges {
  display: flex;
  align-items: center;
  gap: 8px;
}

.task-card-title {
  margin: 0 0 12px;
  font-size: 15px;
  font-weight: 500;
  color: var(--text-primary);
  line-height: 1.5;
}

.task-card-title.completed {
  text-decoration: line-through;
  color: var(--text-muted);
}

.task-card-desc {
  margin: 0 0 12px;
  font-size: 13px;
  color: var(--text-secondary);
  overflow: hidden;
  text-overflow: ellipsis;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  line-height: 1.5;
}

.task-card-tags {
  display: flex;
  gap: 6px;
  margin-bottom: 12px;
  flex-wrap: wrap;
}

.task-tag {
  border-radius: var(--radius-sm);
}

.task-card-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.due-time {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  color: var(--text-muted);
}

.due-time.overdue {
  color: var(--danger-color);
  font-weight: 500;
}

.members {
  display: flex;
}

.member-avatar {
  margin-left: -8px;
  border: 2px solid var(--bg-card);
}

.member-avatar:first-child {
  margin-left: 0;
}

.empty-column {
  padding: 40px 0;
}

.empty-column :deep(.el-empty__description) {
  color: var(--text-muted);
}

/* 动画 */
.task-card-enter-active,
.task-card-leave-active {
  transition: all 0.3s ease;
}

.task-card-enter-from,
.task-card-leave-to {
  opacity: 0;
  transform: translateY(20px);
}

/* 对话框 */
.kanban-dialog :deep(.el-dialog__header) {
  padding: 20px 24px;
  border-bottom: 1px solid var(--border-light);
}

.kanban-dialog :deep(.el-dialog__body) {
  padding: 24px;
}

.kanban-dialog :deep(.el-dialog__footer) {
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
</style>
