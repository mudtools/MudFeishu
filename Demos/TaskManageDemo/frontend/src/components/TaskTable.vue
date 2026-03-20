<template>
  <div class="task-table">
    <!-- 列表视图 -->
    <div v-if="viewMode === 'list'" class="task-list">
      <div
        v-for="task in tasks"
        :key="task.id"
        class="task-item"
        :class="getItemClass(task)"
        @click="handleRowClick(task)"
      >
        <!-- 复选框 -->
        <div class="task-checkbox">
          <el-checkbox
            :model-value="task.isCompleted"
            @change="handleToggleComplete(task, $event)"
            @click.stop
          />
        </div>

        <!-- 任务内容 -->
        <div class="task-content">
          <div class="task-header">
            <TaskPriorityTag :priority="task.priority" />
            <span class="task-title" :class="{ 'text-muted': task.isCompleted }">
              {{ task.summary }}
            </span>
          </div>
          <div class="task-meta">
            <span
              v-if="task.dueTime"
              class="meta-item"
              :class="{ 'text-danger': isTaskOverdue(task) }"
            >
              <el-icon><Calendar /></el-icon>
              {{ formatDate(task.dueTime) }}
            </span>
            <span v-if="task.taskListName" class="meta-item">
              <el-icon><Folder /></el-icon>
              {{ task.taskListName }}
            </span>
          </div>
        </div>

        <!-- 负责人 -->
        <div class="task-assignees">
          <template v-if="task.members && task.members.length > 0">
            <UserAvatar
              v-for="member in getAssignees(task.members).slice(0, 3)"
              :key="member.id"
              :name="member.user?.name || ''"
              :avatar-url="member.user?.avatarUrl"
              :size="28"
              class="member-avatar"
            />
          </template>
          <span v-else class="text-muted">未分配</span>
        </div>

        <!-- 操作按钮 -->
        <div class="task-actions">
          <el-tooltip content="编辑">
            <el-button text circle size="small" @click.stop="handleEdit(task)">
              <el-icon><Edit /></el-icon>
            </el-button>
          </el-tooltip>
          <el-tooltip content="删除">
            <el-button
              text
              circle
              size="small"
              type="danger"
              @click.stop="handleDelete(task)"
            >
              <el-icon><Delete /></el-icon>
            </el-button>
          </el-tooltip>
        </div>
      </div>
    </div>

    <!-- 网格视图 -->
    <div v-else class="task-grid">
      <el-card
        v-for="task in tasks"
        :key="task.id"
        class="task-card-item"
        :class="getItemClass(task)"
        shadow="hover"
        @click="handleRowClick(task)"
      >
        <div class="task-card-header">
          <TaskPriorityTag :priority="task.priority" />
          <el-dropdown trigger="click" @command="(cmd: string) => handleCommand(cmd, task)">
            <el-button text circle size="small" @click.stop>
              <el-icon><More /></el-icon>
            </el-button>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item command="edit">
                  <el-icon><Edit /></el-icon> 编辑
                </el-dropdown-item>
                <el-dropdown-item v-if="!task.isCompleted" command="complete">
                  <el-icon><CircleCheck /></el-icon> 完成
                </el-dropdown-item>
                <el-dropdown-item command="delete" divided>
                  <el-icon><Delete /></el-icon> 删除
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>

        <div class="task-card-title" :class="{ 'text-muted': task.isCompleted }">
          {{ task.summary }}
        </div>

        <div v-if="task.description" class="task-card-description">
          {{ truncateText(task.description, 100) }}
        </div>

        <div class="task-card-footer">
          <span v-if="task.dueTime" class="footer-item" :class="{ 'text-danger': isTaskOverdue(task) }">
            <el-icon><Calendar /></el-icon>
            {{ formatDate(task.dueTime) }}
          </span>
          <div class="footer-assignees">
            <UserAvatar
              v-for="member in getAssignees(task.members || []).slice(0, 2)"
              :key="member.id"
              :name="member.user?.name || ''"
              :avatar-url="member.user?.avatarUrl"
              :size="20"
            />
          </div>
        </div>
      </el-card>
    </div>
  </div>
</template>

<script setup lang="ts">
// import { computed } from 'vue'
import { Calendar, Folder, Edit, Delete, More, CircleCheck } from '@element-plus/icons-vue'
import dayjs from 'dayjs'
import type { Task, TaskMember } from '../types'
import TaskPriorityTag from './TaskPriorityTag.vue'
import UserAvatar from './UserAvatar.vue'

interface Props {
  /** 任务列表 */
  tasks: Task[]
  /** 视图模式 */
  viewMode?: 'list' | 'grid'
}

const props = withDefaults(defineProps<Props>(), {
  viewMode: 'list',
})

const emit = defineEmits<{
  'row-click': [task: Task]
  'edit': [task: Task]
  'delete': [task: Task]
  'toggle-complete': [task: Task, completed: boolean]
}>()

/**
 * 格式化日期
 */
const formatDate = (date: string) => dayjs(date).format('YYYY-MM-DD HH:mm')

/**
 * 判断任务是否逾期
 */
const isTaskOverdue = (task: Task) => {
  if (task.isCompleted || !task.dueTime) return false
  return dayjs(task.dueTime).isBefore(dayjs())
}

/**
 * 获取负责人列表
 */
const getAssignees = (members: TaskMember[]) => {
  return members.filter((m) => m.role === 'assignee')
}

/**
 * 截断文本
 */
const truncateText = (text: string, maxLength: number) => {
  if (text.length <= maxLength) return text
  return text.slice(0, maxLength) + '...'
}

/**
 * 获取任务项样式类
 */
const getItemClass = (task: Task) => ({
  completed: task.isCompleted,
  overdue: isTaskOverdue(task),
})

/**
 * 行点击
 */
const handleRowClick = (task: Task) => {
  emit('row-click', task)
}

/**
 * 编辑任务
 */
const handleEdit = (task: Task) => {
  emit('edit', task)
}

/**
 * 删除任务
 */
const handleDelete = (task: Task) => {
  emit('delete', task)
}

/**
 * 切换完成状态
 */
const handleToggleComplete = (task: Task, completed: boolean) => {
  emit('toggle-complete', task, completed)
}

/**
 * 处理下拉命令
 */
const handleCommand = (command: string, task: Task) => {
  switch (command) {
    case 'edit':
      handleEdit(task)
      break
    case 'complete':
      handleToggleComplete(task, true)
      break
    case 'delete':
      handleDelete(task)
      break
  }
}
</script>

<style scoped>
.task-table {
  width: 100%;
}

/* 列表视图样式 */
.task-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.task-item {
  display: flex;
  align-items: center;
  padding: 16px;
  background: var(--el-bg-color);
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
  border: 1px solid var(--el-border-color-lighter);
}

.task-item:hover {
  background: var(--el-fill-color-light);
  border-color: var(--el-color-primary-light-5);
}

.task-item.completed {
  opacity: 0.6;
}

.task-item.overdue {
  border-left: 3px solid var(--el-color-danger);
}

.task-checkbox {
  margin-right: 12px;
}

.task-content {
  flex: 1;
  min-width: 0;
}

.task-header {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 4px;
}

.task-title {
  font-size: 14px;
  font-weight: 500;
  color: var(--el-text-color-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.task-title.text-muted {
  text-decoration: line-through;
}

.task-meta {
  display: flex;
  align-items: center;
  gap: 16px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.meta-item {
  display: flex;
  align-items: center;
  gap: 4px;
}

.meta-item.text-danger {
  color: var(--el-color-danger);
}

.task-assignees {
  display: flex;
  align-items: center;
  margin-right: 16px;
}

.member-avatar {
  margin-left: -8px;
  border: 2px solid var(--el-bg-color);
}

.member-avatar:first-child {
  margin-left: 0;
}

.task-actions {
  display: flex;
  gap: 4px;
  opacity: 0;
  transition: opacity 0.2s;
}

.task-item:hover .task-actions {
  opacity: 1;
}

/* 网格视图样式 */
.task-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 16px;
}

.task-card-item {
  cursor: pointer;
  transition: all 0.2s;
}

.task-card-item.completed {
  opacity: 0.6;
}

.task-card-item.overdue {
  border-left: 3px solid var(--el-color-danger);
}

.task-card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
}

.task-card-title {
  font-size: 14px;
  font-weight: 500;
  color: var(--el-text-color-primary);
  margin-bottom: 8px;
  line-height: 1.4;
}

.task-card-title.text-muted {
  text-decoration: line-through;
}

.task-card-description {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  margin-bottom: 12px;
  line-height: 1.5;
}

.task-card-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding-top: 12px;
  border-top: 1px solid var(--el-border-color-lighter);
}

.footer-item {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.footer-item.text-danger {
  color: var(--el-color-danger);
}

.footer-assignees {
  display: flex;
  align-items: center;
}

.text-muted {
  color: var(--el-text-color-secondary);
}
</style>
