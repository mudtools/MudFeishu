<template>
  <div class="task-detail-page">
    <!-- 返回按钮 -->
    <div class="back-nav">
      <el-button text @click="goBack">
        <el-icon><ArrowLeft /></el-icon>
        返回任务列表
      </el-button>
    </div>

    <div v-loading="loading" class="detail-content">
      <template v-if="task">
        <!-- 任务头部 -->
        <div class="task-header-card">
          <div class="task-header-top">
            <div class="task-badges">
              <el-tag v-if="task.isCompleted" type="success" effect="dark" size="small">
                <el-icon><CircleCheck /></el-icon>
                已完成
              </el-tag>
              <TaskPriorityTag :priority="task.priority" />
              <el-tag v-if="isOverdue" type="danger" effect="dark" size="small">
                <el-icon><Warning /></el-icon>
                已逾期
              </el-tag>
            </div>
            <div class="task-actions">
              <el-button
                v-if="!task.isCompleted"
                type="success"
                @click="handleComplete"
              >
                <el-icon><CircleCheck /></el-icon>
                完成任务
              </el-button>
              <el-button type="primary" @click="handleEdit">
                <el-icon><Edit /></el-icon>
                编辑
              </el-button>
              <el-button type="danger" @click="handleDelete">
                <el-icon><Delete /></el-icon>
                删除
              </el-button>
            </div>
          </div>

          <h1 class="task-title" :class="{ completed: task.isCompleted }">
            {{ task.summary }}
          </h1>

          <div class="task-meta">
            <div class="meta-item">
              <el-icon><User /></el-icon>
              <span>创建者: {{ task.creatorName || '未知' }}</span>
            </div>
            <div class="meta-item">
              <el-icon><Calendar /></el-icon>
              <span>创建于: {{ formatDate(task.createdAt) }}</span>
            </div>
            <div v-if="task.taskListName" class="meta-item">
              <el-icon><Folder /></el-icon>
              <span>清单: {{ task.taskListName }}</span>
            </div>
          </div>
        </div>

        <div class="detail-grid">
          <!-- 左侧内容 -->
          <div class="detail-main">
            <!-- 描述 -->
            <el-card class="section-card" shadow="never">
              <template #header>
                <div class="section-header">
                  <el-icon><Document /></el-icon>
                  <span>任务描述</span>
                </div>
              </template>
              <div class="description-content">
                {{ task.description || '暂无描述' }}
              </div>
            </el-card>

            <!-- 检查项 -->
            <el-card v-if="task.checkItems && task.checkItems.length > 0" class="section-card" shadow="never">
              <template #header>
                <div class="section-header">
                  <el-icon><List /></el-icon>
                  <span>检查项</span>
                  <el-progress
                    :percentage="checkItemProgress"
                    :format="(p: number) => `${p}%`"
                    class="check-progress"
                  />
                </div>
              </template>
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
            </el-card>

            <!-- 评论 -->
            <el-card class="section-card" shadow="never">
              <template #header>
                <div class="section-header">
                  <el-icon><ChatDotRound /></el-icon>
                  <span>评论</span>
                </div>
              </template>
              <div class="comments-section">
                <div class="comment-input">
                  <el-input
                    v-model="newComment"
                    type="textarea"
                    :rows="3"
                    placeholder="添加评论..."
                  />
                  <el-button type="primary" class="submit-comment" @click="submitComment">
                    发表评论
                  </el-button>
                </div>
                <div class="comments-list">
                  <div v-for="comment in comments" :key="comment.id" class="comment-item">
                    <UserAvatar :name="comment.author" :size="36" />
                    <div class="comment-content">
                      <div class="comment-header">
                        <span class="comment-author">{{ comment.author }}</span>
                        <span class="comment-time">{{ formatDate(comment.createdAt) }}</span>
                      </div>
                      <p class="comment-text">{{ comment.content }}</p>
                    </div>
                  </div>
                </div>
              </div>
            </el-card>
          </div>

          <!-- 右侧信息 -->
          <div class="detail-sidebar">
            <!-- 时间信息 -->
            <el-card class="info-card" shadow="never">
              <template #header>
                <div class="info-header">
                  <el-icon><Clock /></el-icon>
                  <span>时间信息</span>
                </div>
              </template>
              <div class="info-list">
                <div class="info-item">
                  <span class="info-label">开始时间</span>
                  <span class="info-value">{{ task.startTime ? formatDate(task.startTime) : '未设置' }}</span>
                </div>
                <div class="info-item">
                  <span class="info-label">截止时间</span>
                  <span class="info-value" :class="{ 'text-danger': isOverdue }">
                    {{ task.dueTime ? formatDate(task.dueTime) : '未设置' }}
                  </span>
                </div>
                <div class="info-item">
                  <span class="info-label">完成时间</span>
                  <span class="info-value">{{ task.completedTime ? formatDate(task.completedTime) : '未完成' }}</span>
                </div>
              </div>
            </el-card>

            <!-- 成员 -->
            <el-card v-if="task.members && task.members.length > 0" class="info-card" shadow="never">
              <template #header>
                <div class="info-header">
                  <el-icon><UserFilled /></el-icon>
                  <span>任务成员</span>
                </div>
              </template>
              <div class="members-list">
                <div
                  v-for="member in task.members"
                  :key="member.id"
                  class="member-item"
                >
                  <UserAvatar
                    :name="member.name || '用户'"
                    :avatar-url="member.avatarUrl"
                    :size="36"
                  />
                  <div class="member-info">
                    <span class="member-name">{{ member.name }}</span>
                    <el-tag size="small" :type="member.role === 'assignee' ? 'primary' : 'info'">
                      {{ member.role === 'assignee' ? '负责人' : '关注者' }}
                    </el-tag>
                  </div>
                </div>
              </div>
            </el-card>

            <!-- 标签 -->
            <el-card v-if="task.tags && task.tags.length > 0" class="info-card" shadow="never">
              <template #header>
                <div class="info-header">
                  <el-icon><CollectionTag /></el-icon>
                  <span>标签</span>
                </div>
              </template>
              <div class="tags-list">
                <el-tag
                  v-for="tag in task.tags"
                  :key="tag"
                  effect="plain"
                  class="task-tag"
                >
                  {{ tag }}
                </el-tag>
              </div>
            </el-card>
          </div>
        </div>
      </template>

      <el-empty v-else-if="!loading" description="任务不存在" />
    </div>

    <!-- 编辑对话框 -->
    <el-dialog
      v-model="editDialogVisible"
      title="编辑任务"
      width="600px"
      destroy-on-close
      class="edit-dialog"
    >
      <el-form
        ref="formRef"
        :model="editForm"
        :rules="formRules"
        label-position="top"
        class="edit-form"
      >
        <el-form-item label="任务标题" prop="summary">
          <el-input v-model="editForm.summary" size="large" />
        </el-form-item>
        <el-form-item label="任务描述" prop="description">
          <el-input v-model="editForm.description" type="textarea" :rows="4" />
        </el-form-item>
        <div class="form-row">
          <el-form-item label="优先级" prop="priority" class="form-col">
            <el-select v-model="editForm.priority" style="width: 100%">
              <el-option label="低" :value="1" />
              <el-option label="中" :value="2" />
              <el-option label="高" :value="3" />
              <el-option label="紧急" :value="4" />
            </el-select>
          </el-form-item>
          <el-form-item label="截止时间" prop="dueTime" class="form-col">
            <el-date-picker
              v-model="editForm.dueTime"
              type="datetime"
              format="YYYY-MM-DD HH:mm"
              value-format="YYYY-MM-DDTHH:mm:ss"
              style="width: 100%"
            />
          </el-form-item>
        </div>
      </el-form>
      <template #footer>
        <div class="dialog-footer">
          <el-button @click="editDialogVisible = false">取消</el-button>
          <el-button type="primary" :loading="submitting" @click="handleSave">
            保存
          </el-button>
        </div>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import {
  ArrowLeft,
  CircleCheck,
  Edit,
  Delete,
  Warning,
  User,
  Calendar,
  Folder,
  Document,
  List,
  ChatDotRound,
  Clock,
  UserFilled,
  CollectionTag
} from '@element-plus/icons-vue'
import { useTaskStore } from '../stores/task'
import type { UpdateTaskRequest } from '../types'
import { TaskPriorityTag, UserAvatar } from '../components'
import dayjs from 'dayjs'

const route = useRoute()
const router = useRouter()
const taskStore = useTaskStore()

const loading = computed(() => taskStore.loading)
const task = computed(() => taskStore.currentTask)

const editDialogVisible = ref(false)
const submitting = ref(false)
const formRef = ref<FormInstance>()
const newComment = ref('')

const editForm = reactive<UpdateTaskRequest>({
  summary: '',
  description: '',
  priority: 2,
  dueTime: '',
})

const formRules: FormRules = {
  summary: [{ required: true, message: '请输入任务标题', trigger: 'blur' }],
}

// 模拟评论数据
const comments = ref([
  { id: 1, author: '张三', content: '这个任务需要尽快完成', createdAt: '2024-01-15T10:30:00' },
  { id: 2, author: '李四', content: '我已经开始处理了', createdAt: '2024-01-15T14:20:00' },
])

const formatDate = (date: string) => dayjs(date).format('YYYY-MM-DD HH:mm')

const isOverdue = computed(() => {
  if (!task.value || task.value.isCompleted || !task.value.dueTime) return false
  return dayjs(task.value.dueTime).isBefore(dayjs())
})

const checkItemProgress = computed(() => {
  if (!task.value?.checkItems || task.value.checkItems.length === 0) return 0
  const completed = task.value.checkItems.filter(item => item.isCompleted).length
  return Math.round((completed / task.value.checkItems.length) * 100)
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

const submitComment = () => {
  if (!newComment.value.trim()) return
  comments.value.unshift({
    id: Date.now(),
    author: '当前用户',
    content: newComment.value,
    createdAt: new Date().toISOString()
  })
  newComment.value = ''
  ElMessage.success('评论已发表')
}

onMounted(() => {
  const id = Number(route.params.id)
  if (id) taskStore.fetchTask(id)
})
</script>

<style scoped>
.task-detail-page {
  padding: 0;
}

.back-nav {
  margin-bottom: 20px;
}

.task-header-card {
  background: var(--bg-card);
  border-radius: var(--radius-xl);
  padding: 24px;
  margin-bottom: 24px;
  border: 1px solid var(--border-light);
  box-shadow: var(--shadow-sm);
}

.task-header-top {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}

.task-badges {
  display: flex;
  gap: 8px;
}

.task-actions {
  display: flex;
  gap: 8px;
}

.task-title {
  font-size: 24px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0 0 16px;
  line-height: 1.4;
}

.task-title.completed {
  text-decoration: line-through;
  color: var(--text-muted);
}

.task-meta {
  display: flex;
  gap: 24px;
  flex-wrap: wrap;
}

.meta-item {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  color: var(--text-secondary);
}

.detail-grid {
  display: grid;
  grid-template-columns: 1fr 320px;
  gap: 24px;
}

.detail-main {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.section-card {
  border-radius: var(--radius-lg);
}

.section-card :deep(.el-card__header) {
  padding: 16px 20px;
  border-bottom: 1px solid var(--border-light);
}

.section-header {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
  color: var(--text-primary);
}

.check-progress {
  margin-left: auto;
  width: 100px;
}

.description-content {
  padding: 16px;
  background: var(--bg-secondary);
  border-radius: var(--radius-md);
  white-space: pre-wrap;
  line-height: 1.6;
  color: var(--text-secondary);
}

.check-items {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.check-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 12px;
  background: var(--bg-secondary);
  border-radius: var(--radius-md);
}

.check-item .completed {
  text-decoration: line-through;
  color: var(--text-muted);
}

.comments-section {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.comment-input {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.submit-comment {
  align-self: flex-end;
}

.comments-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.comment-item {
  display: flex;
  gap: 12px;
}

.comment-content {
  flex: 1;
  background: var(--bg-secondary);
  padding: 12px 16px;
  border-radius: var(--radius-md);
}

.comment-header {
  display: flex;
  justify-content: space-between;
  margin-bottom: 8px;
}

.comment-author {
  font-weight: 600;
  color: var(--text-primary);
}

.comment-time {
  font-size: 12px;
  color: var(--text-muted);
}

.comment-text {
  margin: 0;
  color: var(--text-secondary);
  line-height: 1.5;
}

.detail-sidebar {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.info-card {
  border-radius: var(--radius-lg);
}

.info-card :deep(.el-card__header) {
  padding: 16px 20px;
  border-bottom: 1px solid var(--border-light);
}

.info-header {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
  color: var(--text-primary);
}

.info-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.info-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.info-label {
  font-size: 13px;
  color: var(--text-secondary);
}

.info-value {
  font-size: 13px;
  color: var(--text-primary);
  font-weight: 500;
}

.members-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.member-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px 12px;
  background: var(--bg-secondary);
  border-radius: var(--radius-md);
}

.member-info {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.member-name {
  font-weight: 500;
  color: var(--text-primary);
}

.tags-list {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.task-tag {
  border-radius: var(--radius-sm);
}

/* 对话框 */
.edit-dialog :deep(.el-dialog__header) {
  padding: 20px 24px;
  border-bottom: 1px solid var(--border-light);
}

.edit-dialog :deep(.el-dialog__body) {
  padding: 24px;
}

.edit-dialog :deep(.el-dialog__footer) {
  padding: 16px 24px;
  border-top: 1px solid var(--border-light);
}

.edit-form :deep(.el-form-item__label) {
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

@media (max-width: 1024px) {
  .detail-grid {
    grid-template-columns: 1fr;
  }

  .detail-sidebar {
    order: -1;
  }
}
</style>
