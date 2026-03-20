<template>
  <div class="statistics-page">
    <!-- 页面标题 -->
    <div class="page-header">
      <div class="header-content">
        <h1 class="page-title">数据统计</h1>
        <p class="page-subtitle">查看任务完成情况和团队绩效</p>
      </div>
      <div class="header-actions">
        <el-button type="primary" @click="refreshData">
          <el-icon><Refresh /></el-icon>
          刷新数据
        </el-button>
      </div>
    </div>

    <!-- 统计卡片 -->
    <div class="stats-grid">
      <div class="stat-card">
        <div class="stat-icon total">
          <el-icon :size="28"><Document /></el-icon>
        </div>
        <div class="stat-info">
          <div class="stat-value">{{ stats.totalTasks }}</div>
          <div class="stat-label">总任务数</div>
        </div>
        <div class="stat-trend">
          <el-tag size="small" type="info">全部</el-tag>
        </div>
      </div>

      <div class="stat-card">
        <div class="stat-icon completed">
          <el-icon :size="28"><CircleCheck /></el-icon>
        </div>
        <div class="stat-info">
          <div class="stat-value">{{ stats.completedTasks }}</div>
          <div class="stat-label">已完成</div>
        </div>
        <div class="stat-trend">
          <el-tag size="small" type="success">+{{ stats.completionRate }}%</el-tag>
        </div>
      </div>

      <div class="stat-card">
        <div class="stat-icon pending">
          <el-icon :size="28"><Clock /></el-icon>
        </div>
        <div class="stat-info">
          <div class="stat-value">{{ stats.pendingTasks }}</div>
          <div class="stat-label">进行中</div>
        </div>
        <div class="stat-trend">
          <el-tag size="small" type="warning">待处理</el-tag>
        </div>
      </div>

      <div class="stat-card">
        <div class="stat-icon overdue">
          <el-icon :size="28"><Warning /></el-icon>
        </div>
        <div class="stat-info">
          <div class="stat-value">{{ stats.overdueTasks }}</div>
          <div class="stat-label">已逾期</div>
        </div>
        <div class="stat-trend">
          <el-tag size="small" type="danger">需关注</el-tag>
        </div>
      </div>
    </div>

    <!-- 图表区域 -->
    <div class="charts-grid">
      <!-- 完成率仪表盘 -->
      <div class="chart-card">
        <div class="chart-header">
          <div class="chart-title">
            <el-icon><PieChart /></el-icon>
            <span>任务完成率</span>
          </div>
        </div>
        <div class="chart-content">
          <div class="completion-rate">
            <el-progress
              type="dashboard"
              :percentage="stats.completionRate"
              :color="getProgressColor(stats.completionRate)"
              :stroke-width="12"
              :width="180"
            >
              <template #default="{ percentage }">
                <div class="percentage-display">
                  <span class="percentage-value">{{ percentage }}%</span>
                  <span class="percentage-label">完成率</span>
                </div>
              </template>
            </el-progress>
          </div>
          <div class="rate-legend">
            <div class="legend-item">
              <div class="legend-dot completed"></div>
              <span>已完成 {{ stats.completedTasks }}</span>
            </div>
            <div class="legend-item">
              <div class="legend-dot pending"></div>
              <span>进行中 {{ stats.pendingTasks }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- 优先级分布 -->
      <div class="chart-card">
        <div class="chart-header">
          <div class="chart-title">
            <el-icon><Histogram /></el-icon>
            <span>优先级分布</span>
          </div>
        </div>
        <div class="chart-content">
          <div class="priority-chart">
            <div
              v-for="item in priorityData"
              :key="item.priority"
              class="priority-item"
            >
              <div class="priority-info">
                <span class="priority-label" :style="{ color: item.color }">
                  {{ item.label }}
                </span>
                <span class="priority-count">{{ item.count }} 个任务</span>
              </div>
              <div class="priority-bar-wrapper">
                <div
                  class="priority-bar"
                  :style="{
                    width: getPercentage(item.count) + '%',
                    background: item.gradient
                  }"
                ></div>
              </div>
              <span class="priority-percent">{{ getPercentage(item.count) }}%</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 最近任务 -->
    <div class="recent-tasks-section">
      <div class="section-header">
        <div class="section-title">
          <el-icon><List /></el-icon>
          <span>最近任务</span>
        </div>
        <el-button text type="primary" @click="goToTasks">
          查看全部
          <el-icon class="el-icon--right"><ArrowRight /></el-icon>
        </el-button>
      </div>

      <div class="tasks-table-wrapper">
        <el-table
          :data="recentTasks"
          class="modern-table"
          v-loading="taskStore.loading"
        >
          <el-table-column label="任务标题" min-width="280">
            <template #default="{ row }">
              <div class="task-title-cell">
                <div
                  class="task-status-dot"
                  :class="{ completed: row.isCompleted }"
                ></div>
                <span :class="{ completed: row.isCompleted }">{{ row.summary }}</span>
              </div>
            </template>
          </el-table-column>

          <el-table-column label="优先级" width="120">
            <template #default="{ row }">
              <div class="priority-tag" :class="`priority-${row.priority}`">
                {{ getPriorityLabel(row.priority) }}
              </div>
            </template>
          </el-table-column>

          <el-table-column label="截止时间" width="160">
            <template #default="{ row }">
              <div class="due-date" :class="{ overdue: isOverdue(row) }">
                <el-icon v-if="isOverdue(row)" class="overdue-icon"><Warning /></el-icon>
                <span v-if="row.dueTime">{{ formatDate(row.dueTime) }}</span>
                <span v-else class="text-muted">未设置</span>
              </div>
            </template>
          </el-table-column>

          <el-table-column label="状态" width="100">
            <template #default="{ row }">
              <el-tag
                :type="row.isCompleted ? 'success' : 'warning'"
                size="small"
                effect="light"
                class="status-tag"
              >
                <el-icon v-if="row.isCompleted"><CircleCheck /></el-icon>
                <el-icon v-else><Clock /></el-icon>
                {{ row.isCompleted ? '已完成' : '进行中' }}
              </el-tag>
            </template>
          </el-table-column>

          <el-table-column label="操作" width="100" fixed="right">
            <template #default="{ row }">
              <el-button
                link
                type="primary"
                size="small"
                @click="goToTask(row.id)"
              >
                查看
              </el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import {
  Document,
  CircleCheck,
  Clock,
  Warning,
  Refresh,
  PieChart,
  Histogram,
  List,
  ArrowRight
} from '@element-plus/icons-vue'
import { useTaskStore } from '../stores/task'
import type { Task } from '../types'
import dayjs from 'dayjs'

const router = useRouter()
const taskStore = useTaskStore()

const stats = computed(() => {
  const tasks = taskStore.tasks
  const total = tasks.length
  const completed = tasks.filter((t) => t.isCompleted).length
  const pending = total - completed
  const overdue = tasks.filter((t) => !t.isCompleted && t.dueTime && dayjs(t.dueTime).isBefore(dayjs())).length

  return {
    totalTasks: total,
    completedTasks: completed,
    pendingTasks: pending,
    overdueTasks: overdue,
    completionRate: total > 0 ? Math.round((completed / total) * 100) : 0,
  }
})

const priorityData = computed(() => {
  const tasks = taskStore.tasks
  const priorities = [
    { priority: 4, label: '紧急', color: '#ef4444', gradient: 'linear-gradient(90deg, #ef4444, #f87171)' },
    { priority: 3, label: '高', color: '#f97316', gradient: 'linear-gradient(90deg, #f97316, #fb923c)' },
    { priority: 2, label: '中', color: '#eab308', gradient: 'linear-gradient(90deg, #eab308, #facc15)' },
    { priority: 1, label: '低', color: '#6b7280', gradient: 'linear-gradient(90deg, #6b7280, #9ca3af)' },
  ]

  return priorities.map((p) => ({
    ...p,
    count: tasks.filter((t) => t.priority === p.priority).length,
  }))
})

const recentTasks = computed(() => {
  return [...taskStore.tasks]
    .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
    .slice(0, 10)
})

const getProgressColor = (percentage: number) => {
  if (percentage >= 80) return '#10b981'
  if (percentage >= 50) return '#f59e0b'
  return '#ef4444'
}

const getPercentage = (count: number) => {
  const total = taskStore.tasks.length
  return total > 0 ? Math.round((count / total) * 100) : 0
}

const getPriorityLabel = (priority: number) => {
  const labels: Record<number, string> = { 1: '低', 2: '中', 3: '高', 4: '紧急' }
  return labels[priority] || '未设置'
}

const isOverdue = (task: Task) => {
  if (task.isCompleted || !task.dueTime) return false
  return dayjs(task.dueTime).isBefore(dayjs())
}

const formatDate = (date: string) => dayjs(date).format('MM-DD HH:mm')

const refreshData = () => {
  taskStore.fetchTasks({ includeCompleted: true, pageSize: 100 })
}

const goToTasks = () => {
  router.push('/tasks')
}

const goToTask = (id: number) => {
  router.push(`/tasks/${id}`)
}
</script>

<style scoped>
.statistics-page {
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

/* 统计卡片网格 */
.stats-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 20px;
  margin-bottom: 24px;
}

.stat-card {
  background: var(--bg-card);
  border-radius: var(--radius-xl);
  padding: 20px;
  display: flex;
  align-items: center;
  gap: 16px;
  border: 1px solid var(--border-light);
  box-shadow: var(--shadow-sm);
  transition: all var(--transition-fast);
}

.stat-card:hover {
  box-shadow: var(--shadow-md);
  transform: translateY(-2px);
}

.stat-icon {
  width: 56px;
  height: 56px;
  border-radius: var(--radius-lg);
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fff;
  flex-shrink: 0;
}

.stat-icon.total {
  background: linear-gradient(135deg, var(--primary-color) 0%, var(--primary-light) 100%);
}

.stat-icon.completed {
  background: linear-gradient(135deg, var(--success-color) 0%, #34d399 100%);
}

.stat-icon.pending {
  background: linear-gradient(135deg, var(--warning-color) 0%, #fbbf24 100%);
}

.stat-icon.overdue {
  background: linear-gradient(135deg, var(--danger-color) 0%, #f87171 100%);
}

.stat-info {
  flex: 1;
}

.stat-value {
  font-size: 32px;
  font-weight: 700;
  color: var(--text-primary);
  line-height: 1;
  margin-bottom: 4px;
}

.stat-label {
  font-size: 14px;
  color: var(--text-secondary);
}

.stat-trend {
  flex-shrink: 0;
}

/* 图表区域 */
.charts-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 24px;
  margin-bottom: 24px;
}

.chart-card {
  background: var(--bg-card);
  border-radius: var(--radius-xl);
  border: 1px solid var(--border-light);
  box-shadow: var(--shadow-sm);
  overflow: hidden;
}

.chart-header {
  padding: 20px 24px;
  border-bottom: 1px solid var(--border-light);
}

.chart-title {
  display: flex;
  align-items: center;
  gap: 10px;
  font-weight: 600;
  font-size: 16px;
  color: var(--text-primary);
}

.chart-content {
  padding: 24px;
}

/* 完成率仪表盘 */
.completion-rate {
  display: flex;
  justify-content: center;
  margin-bottom: 24px;
}

.percentage-display {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
}

.percentage-value {
  font-size: 36px;
  font-weight: 700;
  color: var(--text-primary);
  line-height: 1;
}

.percentage-label {
  font-size: 14px;
  color: var(--text-secondary);
}

.rate-legend {
  display: flex;
  justify-content: center;
  gap: 32px;
}

.legend-item {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  color: var(--text-secondary);
}

.legend-dot {
  width: 12px;
  height: 12px;
  border-radius: 50%;
}

.legend-dot.completed {
  background: var(--success-color);
}

.legend-dot.pending {
  background: var(--warning-color);
}

/* 优先级分布 */
.priority-chart {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.priority-item {
  display: flex;
  align-items: center;
  gap: 16px;
}

.priority-info {
  width: 80px;
  flex-shrink: 0;
}

.priority-label {
  display: block;
  font-weight: 600;
  font-size: 14px;
  margin-bottom: 2px;
}

.priority-count {
  font-size: 12px;
  color: var(--text-muted);
}

.priority-bar-wrapper {
  flex: 1;
  height: 10px;
  background: var(--bg-tertiary);
  border-radius: var(--radius-full);
  overflow: hidden;
}

.priority-bar {
  height: 100%;
  border-radius: var(--radius-full);
  transition: width 0.5s ease;
}

.priority-percent {
  width: 50px;
  text-align: right;
  font-size: 14px;
  font-weight: 500;
  color: var(--text-secondary);
}

/* 最近任务 */
.recent-tasks-section {
  background: var(--bg-card);
  border-radius: var(--radius-xl);
  border: 1px solid var(--border-light);
  box-shadow: var(--shadow-sm);
  overflow: hidden;
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px 24px;
  border-bottom: 1px solid var(--border-light);
}

.section-title {
  display: flex;
  align-items: center;
  gap: 10px;
  font-weight: 600;
  font-size: 16px;
  color: var(--text-primary);
}

.tasks-table-wrapper {
  padding: 0;
}

/* 表格样式 */
.modern-table :deep(.el-table__header) {
  background: var(--bg-secondary);
}

.modern-table :deep(.el-table__header th) {
  background: transparent;
  font-weight: 600;
  color: var(--text-secondary);
  border-bottom: 1px solid var(--border-light);
}

.modern-table :deep(.el-table__row) {
  transition: background-color var(--transition-fast);
}

.modern-table :deep(.el-table__row:hover) {
  background: var(--bg-secondary);
}

.task-title-cell {
  display: flex;
  align-items: center;
  gap: 12px;
}

.task-status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--warning-color);
  flex-shrink: 0;
}

.task-status-dot.completed {
  background: var(--success-color);
}

.task-title-cell .completed {
  text-decoration: line-through;
  color: var(--text-muted);
}

.priority-tag {
  display: inline-flex;
  align-items: center;
  padding: 4px 12px;
  border-radius: var(--radius-full);
  font-size: 12px;
  font-weight: 500;
}

.priority-1 {
  background: rgba(107, 114, 128, 0.15);
  color: #6b7280;
}

.priority-2 {
  background: rgba(234, 179, 8, 0.15);
  color: #ca8a04;
}

.priority-3 {
  background: rgba(249, 115, 22, 0.15);
  color: #ea580c;
}

.priority-4 {
  background: rgba(239, 68, 68, 0.15);
  color: #dc2626;
}

.due-date {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  color: var(--text-secondary);
}

.due-date.overdue {
  color: var(--danger-color);
  font-weight: 500;
}

.overdue-icon {
  color: var(--danger-color);
}

.status-tag {
  display: inline-flex;
  align-items: center;
  gap: 4px;
}

.text-muted {
  color: var(--text-muted);
}

/* 响应式 */
@media (max-width: 1200px) {
  .stats-grid {
    grid-template-columns: repeat(2, 1fr);
  }

  .charts-grid {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 768px) {
  .stats-grid {
    grid-template-columns: 1fr;
  }

  .page-header {
    flex-direction: column;
    gap: 16px;
  }
}
</style>
