<template>
  <div class="kanban-page">
    <div class="kanban-header">
      <h2>任务看板</h2>
      <el-button type="primary" @click="showCreateDialog">新建任务</el-button>
    </div>

    <div class="kanban-board">
      <div
        v-for="column in columns"
        :key="column.status"
        class="kanban-column"
        @dragover.prevent
        @drop="handleDrop($event, column.status)"
      >
        <div class="column-header">
          <span class="column-title">{{ column.title }}</span>
          <el-badge :value="getTasksByStatus(column.status).length" type="primary" />
        </div>

        <div class="column-content">
          <div
            v-for="task in getTasksByStatus(column.status)"
            :key="task.id"
            class="task-card"
            draggable="true"
            @dragstart="handleDragStart($event, task)"
            @click="goToTask(task.id)"
          >
            <div class="task-card-header">
              <el-tag :type="getPriorityType(task.priority)" size="small">
                {{ getPriorityLabel(task.priority) }}
              </el-tag>
              <span v-if="isOverdue(task)" class="overdue-badge">逾期</span>
            </div>
            <h4 class="task-card-title">{{ task.summary }}</h4>
            <p v-if="task.description" class="task-card-desc">{{ task.description }}</p>
            <div class="task-card-footer">
              <div v-if="task.dueTime" class="due-time" :class="{ overdue: isOverdue(task) }">
                <el-icon><Clock /></el-icon>
                {{ formatDate(task.dueTime) }}
              </div>
              <div v-if="task.members && task.members.length > 0" class="members">
                <el-avatar
                  v-for="member in task.members.filter((m: any) => m.role === 'assignee').slice(0, 2)"
                  :key="member.id"
                  :size="24"
                  :src="member.user?.avatarUrl"
                >
                  {{ member.user?.name?.charAt(0) }}
                </el-avatar>
              </div>
            </div>
          </div>

          <el-empty v-if="getTasksByStatus(column.status).length === 0" description="暂无任务" />
        </div>
      </div>
    </div>

    <el-dialog v-model="createDialogVisible" title="新建任务" width="500px">
      <el-form ref="formRef" :model="taskForm" :rules="formRules" label-width="80px">
        <el-form-item label="任务标题" prop="summary">
          <el-input v-model="taskForm.summary" placeholder="请输入任务标题" />
        </el-form-item>
        <el-form-item label="优先级" prop="priority">
          <el-select v-model="taskForm.priority" placeholder="选择优先级">
            <el-option label="低" :value="1" />
            <el-option label="中" :value="2" />
            <el-option label="高" :value="3" />
            <el-option label="紧急" :value="4" />
          </el-select>
        </el-form-item>
        <el-form-item label="截止时间" prop="dueTime">
          <el-date-picker
            v-model="taskForm.dueTime"
            type="datetime"
            placeholder="选择截止时间"
            format="YYYY-MM-DD HH:mm"
            value-format="YYYY-MM-DDTHH:mm:ss"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="createDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="handleCreate">创建</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { Clock } from '@element-plus/icons-vue'
import { useTaskStore } from '../stores/task'
import type { Task, CreateTaskRequest } from '../types'
import dayjs from 'dayjs'

const router = useRouter()
const taskStore = useTaskStore()

const columns = [
  { status: 'pending', title: '待处理' },
  { status: 'in_progress', title: '进行中' },
  { status: 'completed', title: '已完成' },
]

const createDialogVisible = ref(false)
const submitting = ref(false)
const formRef = ref<FormInstance>()
const draggedTask = ref<Task | null>(null)

const taskForm = reactive<CreateTaskRequest>({
  summary: '',
  priority: 2,
})

const formRules: FormRules = {
  summary: [{ required: true, message: '请输入任务标题', trigger: 'blur' }],
}

const getPriorityType = (priority: number) => {
  const types: Record<number, string> = { 1: 'info', 2: 'warning', 3: 'danger', 4: 'danger' }
  return types[priority] || 'info'
}

const getPriorityLabel = (priority: number) => {
  const labels: Record<number, string> = { 1: '低', 2: '中', 3: '高', 4: '紧急' }
  return labels[priority] || '未设置'
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

const handleDrop = async (_event: DragEvent, status: string) => {
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

const showCreateDialog = () => {
  Object.assign(taskForm, { summary: '', priority: 2, dueTime: '' })
  createDialogVisible.value = true
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
  padding: 20px;
  height: calc(100vh - 100px);
  display: flex;
  flex-direction: column;
}

.kanban-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.kanban-header h2 {
  margin: 0;
}

.kanban-board {
  display: flex;
  gap: 20px;
  flex: 1;
  overflow-x: auto;
}

.kanban-column {
  flex: 1;
  min-width: 300px;
  max-width: 400px;
  background: #f5f7fa;
  border-radius: 8px;
  display: flex;
  flex-direction: column;
}

.column-header {
  padding: 16px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-bottom: 1px solid #e4e7ed;
}

.column-title {
  font-weight: 600;
  font-size: 16px;
}

.column-content {
  padding: 12px;
  flex: 1;
  overflow-y: auto;
}

.task-card {
  background: #fff;
  border-radius: 6px;
  padding: 12px;
  margin-bottom: 12px;
  cursor: pointer;
  transition: box-shadow 0.2s;
  border: 1px solid #e4e7ed;
}

.task-card:hover {
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
}

.task-card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
}

.overdue-badge {
  color: #f56c6c;
  font-size: 12px;
}

.task-card-title {
  margin: 0 0 8px;
  font-size: 14px;
  font-weight: 500;
}

.task-card-desc {
  margin: 0 0 8px;
  font-size: 12px;
  color: #909399;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
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
  color: #909399;
}

.due-time.overdue {
  color: #f56c6c;
}

.members {
  display: flex;
}

.members .el-avatar {
  margin-left: -8px;
  border: 2px solid #fff;
}

.members .el-avatar:first-child {
  margin-left: 0;
}
</style>
