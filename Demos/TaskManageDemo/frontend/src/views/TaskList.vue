<template>
  <div class="task-list-page">
    <el-card class="search-card">
      <el-form :inline="true" :model="searchForm" class="search-form">
        <el-form-item label="关键词">
          <el-input
            v-model="searchForm.keyword"
            placeholder="搜索任务标题或描述"
            clearable
            @keyup.enter="handleSearch"
          />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="searchForm.status" placeholder="选择状态" clearable>
            <el-option label="全部" value="all" />
            <el-option label="进行中" value="pending" />
            <el-option label="已完成" value="completed" />
          </el-select>
        </el-form-item>
        <el-form-item label="优先级">
          <el-select v-model="searchForm.priority" placeholder="选择优先级" clearable>
            <el-option label="全部" :value="0" />
            <el-option label="低" :value="1" />
            <el-option label="中" :value="2" />
            <el-option label="高" :value="3" />
            <el-option label="紧急" :value="4" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSearch">搜索</el-button>
          <el-button @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card class="task-card">
      <template #header>
        <div class="card-header">
          <span>任务列表 (共 {{ taskStore.total }} 个)</span>
          <el-button type="primary" @click="showCreateDialog">新建任务</el-button>
        </div>
      </template>

      <el-table
        v-loading="taskStore.loading"
        :data="taskStore.tasks"
        stripe
        style="width: 100%"
        @row-click="handleRowClick"
      >
        <el-table-column prop="summary" label="任务标题" min-width="200">
          <template #default="{ row }">
            <div class="task-title">
              <el-tag v-if="row.isCompleted" type="success" size="small">已完成</el-tag>
              <span :class="{ completed: row.isCompleted }">{{ row.summary }}</span>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="priority" label="优先级" width="100">
          <template #default="{ row }">
            <el-tag :type="getPriorityType(row.priority)" size="small">
              {{ getPriorityLabel(row.priority) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="dueTime" label="截止时间" width="150">
          <template #default="{ row }">
            <span v-if="row.dueTime" :class="{ overdue: isOverdue(row) }">
              {{ formatDate(row.dueTime) }}
            </span>
            <span v-else class="text-muted">未设置</span>
          </template>
        </el-table-column>
        <el-table-column prop="members" label="负责人" width="120">
          <template #default="{ row }">
            <template v-if="row.members && row.members.length > 0">
              <el-avatar
                v-for="member in row.members.filter((m: any) => m.role === 'assignee').slice(0, 3)"
                :key="member.id"
                :size="24"
                :src="member.user?.avatarUrl"
                class="member-avatar"
              >
                {{ member.user?.name?.charAt(0) }}
              </el-avatar>
            </template>
            <span v-else class="text-muted">未分配</span>
          </template>
        </el-table-column>
        <el-table-column prop="createdAt" label="创建时间" width="150">
          <template #default="{ row }">
            {{ formatDate(row.createdAt) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="150" fixed="right">
          <template #default="{ row }">
            <el-button
              v-if="!row.isCompleted"
              type="success"
              size="small"
              text
              @click.stop="handleComplete(row)"
            >
              完成
            </el-button>
            <el-button type="primary" size="small" text @click.stop="handleEdit(row)">
              编辑
            </el-button>
            <el-button type="danger" size="small" text @click.stop="handleDelete(row)">
              删除
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-container">
        <el-pagination
          v-model:current-page="currentPage"
          v-model:page-size="pageSize"
          :total="taskStore.total"
          :page-sizes="[10, 20, 50, 100]"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="handleSizeChange"
          @current-change="handlePageChange"
        />
      </div>
    </el-card>

    <el-dialog
      v-model="createDialogVisible"
      :title="editingTask ? '编辑任务' : '新建任务'"
      width="600px"
      destroy-on-close
    >
      <el-form
        ref="taskFormRef"
        :model="taskForm"
        :rules="taskRules"
        label-width="80px"
      >
        <el-form-item label="任务标题" prop="summary">
          <el-input v-model="taskForm.summary" placeholder="请输入任务标题" />
        </el-form-item>
        <el-form-item label="任务描述" prop="description">
          <el-input
            v-model="taskForm.description"
            type="textarea"
            :rows="3"
            placeholder="请输入任务描述"
          />
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
        <el-button type="primary" :loading="submitting" @click="handleSubmit">
          {{ editingTask ? '保存' : '创建' }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { useTaskStore } from '../stores/task'
import type { Task, TaskSearchParams, CreateTaskRequest, UpdateTaskRequest } from '../types'
import dayjs from 'dayjs'

const router = useRouter()
const taskStore = useTaskStore()

const searchForm = reactive<TaskSearchParams>({
  keyword: '',
  status: 'all',
  priority: 0,
  page: 1,
  pageSize: 20,
})

const currentPage = ref(1)
const pageSize = ref(20)

const createDialogVisible = ref(false)
const editingTask = ref<Task | null>(null)
const submitting = ref(false)
const taskFormRef = ref<FormInstance>()

const taskForm = reactive<CreateTaskRequest & { id?: number }>({
  summary: '',
  description: '',
  priority: 2,
  dueTime: '',
})

const taskRules: FormRules = {
  summary: [{ required: true, message: '请输入任务标题', trigger: 'blur' }],
}

const getPriorityType = (priority: number) => {
  const types: Record<number, string> = {
    1: 'info',
    2: 'warning',
    3: 'danger',
    4: 'danger',
  }
  return types[priority] || 'info'
}

const getPriorityLabel = (priority: number) => {
  const labels: Record<number, string> = {
    1: '低',
    2: '中',
    3: '高',
    4: '紧急',
  }
  return labels[priority] || '未设置'
}

const formatDate = (date: string) => {
  return dayjs(date).format('YYYY-MM-DD HH:mm')
}

const isOverdue = (task: Task) => {
  if (task.isCompleted || !task.dueTime) return false
  return dayjs(task.dueTime).isBefore(dayjs())
}

const handleSearch = () => {
  currentPage.value = 1
  taskStore.fetchTasks({ ...searchForm, page: 1 })
}

const handleReset = () => {
  searchForm.keyword = ''
  searchForm.status = 'all'
  searchForm.priority = 0
  handleSearch()
}

const handleSizeChange = (size: number) => {
  pageSize.value = size
  taskStore.fetchTasks({ ...searchForm, page: 1, pageSize: size })
}

const handlePageChange = (page: number) => {
  currentPage.value = page
  taskStore.fetchTasks({ ...searchForm, page })
}

const handleRowClick = (row: Task) => {
  router.push(`/tasks/${row.id}`)
}

const showCreateDialog = () => {
  editingTask.value = null
  Object.assign(taskForm, {
    summary: '',
    description: '',
    priority: 2,
    dueTime: '',
  })
  createDialogVisible.value = true
}

const handleEdit = (task: Task) => {
  editingTask.value = task
  Object.assign(taskForm, {
    id: task.id,
    summary: task.summary,
    description: task.description || '',
    priority: task.priority,
    dueTime: task.dueTime || '',
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
        ElMessage.success('任务更新成功')
      } else {
        const request: CreateTaskRequest = {
          summary: taskForm.summary,
          description: taskForm.description,
          priority: taskForm.priority,
          dueTime: taskForm.dueTime || undefined,
        }
        await taskStore.createTask(request)
        ElMessage.success('任务创建成功')
      }
      createDialogVisible.value = false
    } catch (error) {
      ElMessage.error(editingTask.value ? '任务更新失败' : '任务创建失败')
    } finally {
      submitting.value = false
    }
  })
}

const handleComplete = async (task: Task) => {
  try {
    await ElMessageBox.confirm('确定要将此任务标记为已完成吗？', '确认', {
      type: 'success',
    })
    await taskStore.completeTask(task.id)
    ElMessage.success('任务已完成')
  } catch {
    // User cancelled
  }
}

const handleDelete = async (task: Task) => {
  try {
    await ElMessageBox.confirm('确定要删除此任务吗？此操作不可恢复。', '确认删除', {
      type: 'warning',
    })
    await taskStore.deleteTask(task.id)
    ElMessage.success('任务已删除')
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
  padding: 20px;
}

.search-card {
  margin-bottom: 20px;
}

.search-form {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.task-title {
  display: flex;
  align-items: center;
  gap: 8px;
}

.task-title .completed {
  text-decoration: line-through;
  color: #999;
}

.member-avatar {
  margin-left: -8px;
  border: 2px solid #fff;
}

.member-avatar:first-child {
  margin-left: 0;
}

.text-muted {
  color: #999;
}

.overdue {
  color: #f56c6c;
}

.pagination-container {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}
</style>
