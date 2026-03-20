<template>
  <div class="task-detail-page">
    <el-page-header @back="goBack">
      <template #content>
        <span class="text-large font-600 mr-3">任务详情</span>
      </template>
    </el-page-header>

    <el-card v-loading="loading" class="detail-card">
      <template v-if="task">
        <div class="task-header">
          <div class="task-title-row">
            <el-tag v-if="task.isCompleted" type="success">已完成</el-tag>
            <el-tag :type="getPriorityType(task.priority)" size="small">
              {{ getPriorityLabel(task.priority) }}
            </el-tag>
            <h2 :class="{ completed: task.isCompleted }">{{ task.summary }}</h2>
          </div>
          <div class="task-actions">
            <el-button
              v-if="!task.isCompleted"
              type="success"
              @click="handleComplete"
            >
              完成任务
            </el-button>
            <el-button type="primary" @click="handleEdit">编辑</el-button>
            <el-button type="danger" @click="handleDelete">删除</el-button>
          </div>
        </div>

        <el-divider />

        <el-descriptions :column="2" border>
          <el-descriptions-item label="任务ID">{{ task.taskGuid }}</el-descriptions-item>
          <el-descriptions-item label="优先级">
            <el-tag :type="getPriorityType(task.priority)">
              {{ getPriorityLabel(task.priority) }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="截止时间">
            <span v-if="task.dueTime" :class="{ overdue: isOverdue }">
              {{ formatDate(task.dueTime) }}
              <el-tag v-if="isOverdue" type="danger" size="small">已逾期</el-tag>
            </span>
            <span v-else class="text-muted">未设置</span>
          </el-descriptions-item>
          <el-descriptions-item label="开始时间">
            <span v-if="task.startTime">{{ formatDate(task.startTime) }}</span>
            <span v-else class="text-muted">未设置</span>
          </el-descriptions-item>
          <el-descriptions-item label="创建时间">
            {{ formatDate(task.createdAt) }}
          </el-descriptions-item>
          <el-descriptions-item label="更新时间">
            {{ formatDate(task.updatedAt) }}
          </el-descriptions-item>
          <el-descriptions-item label="完成时间" :span="2">
            <span v-if="task.completedTime">{{ formatDate(task.completedTime) }}</span>
            <span v-else class="text-muted">未完成</span>
          </el-descriptions-item>
        </el-descriptions>

        <div class="section">
          <h3>任务描述</h3>
          <div class="description">
            {{ task.description || '暂无描述' }}
          </div>
        </div>

        <div v-if="task.members && task.members.length > 0" class="section">
          <h3>任务成员</h3>
          <div class="members-list">
            <div
              v-for="member in task.members"
              :key="member.id"
              class="member-item"
            >
              <el-avatar :size="32" :src="member.user?.avatarUrl">
                {{ member.user?.name?.charAt(0) }}
              </el-avatar>
              <div class="member-info">
                <span class="member-name">{{ member.user?.name }}</span>
                <el-tag size="small" :type="member.role === 'assignee' ? 'primary' : 'info'">
                  {{ member.role === 'assignee' ? '负责人' : '关注者' }}
                </el-tag>
              </div>
            </div>
          </div>
        </div>

        <div v-if="task.checkItems && task.checkItems.length > 0" class="section">
          <h3>检查项</h3>
          <div class="check-items">
            <div
              v-for="item in task.checkItems"
              :key="item.id"
              class="check-item"
            >
              <el-checkbox :model-value="item.isCompleted" disabled />
              <span :class="{ completed: item.isCompleted }">{{ item.content }}</span>
            </div>
          </div>
        </div>
      </template>

      <el-empty v-else-if="!loading" description="任务不存在" />
    </el-card>

    <el-dialog v-model="editDialogVisible" title="编辑任务" width="600px">
      <el-form ref="formRef" :model="editForm" :rules="formRules" label-width="80px">
        <el-form-item label="任务标题" prop="summary">
          <el-input v-model="editForm.summary" />
        </el-form-item>
        <el-form-item label="任务描述" prop="description">
          <el-input v-model="editForm.description" type="textarea" :rows="3" />
        </el-form-item>
        <el-form-item label="优先级" prop="priority">
          <el-select v-model="editForm.priority">
            <el-option label="低" :value="1" />
            <el-option label="中" :value="2" />
            <el-option label="高" :value="3" />
            <el-option label="紧急" :value="4" />
          </el-select>
        </el-form-item>
        <el-form-item label="截止时间" prop="dueTime">
          <el-date-picker
            v-model="editForm.dueTime"
            type="datetime"
            format="YYYY-MM-DD HH:mm"
            value-format="YYYY-MM-DDTHH:mm:ss"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="editDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="handleSave">
          保存
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { useTaskStore } from '../stores/task'
import type { UpdateTaskRequest } from '../types'
import dayjs from 'dayjs'

const route = useRoute()
const router = useRouter()
const taskStore = useTaskStore()

const loading = computed(() => taskStore.loading)
const task = computed(() => taskStore.currentTask)

const editDialogVisible = ref(false)
const submitting = ref(false)
const formRef = ref<FormInstance>()

const editForm = reactive<UpdateTaskRequest>({
  summary: '',
  description: '',
  priority: 2,
  dueTime: '',
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

const formatDate = (date: string) => dayjs(date).format('YYYY-MM-DD HH:mm')

const isOverdue = computed(() => {
  if (!task.value || task.value.isCompleted || !task.value.dueTime) return false
  return dayjs(task.value.dueTime).isBefore(dayjs())
})

const goBack = () => router.push('/tasks')

const handleComplete = async () => {
  try {
    await ElMessageBox.confirm('确定要将此任务标记为已完成吗？', '确认', { type: 'success' })
    await taskStore.completeTask(task.value!.id)
    ElMessage.success('任务已完成')
  } catch {}
}

const handleEdit = () => {
  if (!task.value) return
  Object.assign(editForm, {
    summary: task.value.summary,
    description: task.value.description || '',
    priority: task.value.priority,
    dueTime: task.value.dueTime || '',
  })
  editDialogVisible.value = true
}

const handleSave = async () => {
  if (!formRef.value || !task.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    submitting.value = true
    try {
      await taskStore.updateTask(task.value!.id, editForm)
      ElMessage.success('任务更新成功')
      editDialogVisible.value = false
    } catch {
      ElMessage.error('任务更新失败')
    } finally {
      submitting.value = false
    }
  })
}

const handleDelete = async () => {
  try {
    await ElMessageBox.confirm('确定要删除此任务吗？此操作不可恢复。', '确认删除', { type: 'warning' })
    await taskStore.deleteTask(task.value!.id)
    ElMessage.success('任务已删除')
    router.push('/tasks')
  } catch {}
}

onMounted(() => {
  const id = Number(route.params.id)
  if (id) taskStore.fetchTask(id)
})
</script>

<style scoped>
.task-detail-page {
  padding: 20px;
}

.detail-card {
  margin-top: 20px;
}

.task-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
}

.task-title-row {
  display: flex;
  align-items: center;
  gap: 12px;
}

.task-title-row h2 {
  margin: 0;
  font-size: 20px;
}

.task-title-row h2.completed {
  text-decoration: line-through;
  color: #999;
}

.section {
  margin-top: 24px;
}

.section h3 {
  margin-bottom: 12px;
  font-size: 16px;
  color: #303133;
}

.description {
  padding: 12px;
  background: #f5f7fa;
  border-radius: 4px;
  white-space: pre-wrap;
}

.members-list {
  display: flex;
  flex-wrap: wrap;
  gap: 16px;
}

.member-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 8px 12px;
  background: #f5f7fa;
  border-radius: 4px;
}

.member-info {
  display: flex;
  align-items: center;
  gap: 8px;
}

.member-name {
  font-weight: 500;
}

.check-items {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.check-item {
  display: flex;
  align-items: center;
  gap: 8px;
}

.check-item .completed {
  text-decoration: line-through;
  color: #999;
}

.text-muted {
  color: #999;
}

.overdue {
  color: #f56c6c;
}
</style>
