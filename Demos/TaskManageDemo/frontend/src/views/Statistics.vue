<template>
  <div class="statistics-page">
    <el-row :gutter="20">
      <el-col :span="6">
        <el-card class="stat-card">
          <div class="stat-content">
            <div class="stat-icon total">
              <el-icon :size="32"><Document /></el-icon>
            </div>
            <div class="stat-info">
              <div class="stat-value">{{ stats.totalTasks }}</div>
              <div class="stat-label">总任务数</div>
            </div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card">
          <div class="stat-content">
            <div class="stat-icon completed">
              <el-icon :size="32"><CircleCheck /></el-icon>
            </div>
            <div class="stat-info">
              <div class="stat-value">{{ stats.completedTasks }}</div>
              <div class="stat-label">已完成</div>
            </div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card">
          <div class="stat-content">
            <div class="stat-icon pending">
              <el-icon :size="32"><Clock /></el-icon>
            </div>
            <div class="stat-info">
              <div class="stat-value">{{ stats.pendingTasks }}</div>
              <div class="stat-label">进行中</div>
            </div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card">
          <div class="stat-content">
            <div class="stat-icon overdue">
              <el-icon :size="32"><Warning /></el-icon>
            </div>
            <div class="stat-info">
              <div class="stat-value">{{ stats.overdueTasks }}</div>
              <div class="stat-label">已逾期</div>
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <el-row :gutter="20" class="chart-row">
      <el-col :span="12">
        <el-card>
          <template #header>
            <span>完成率</span>
          </template>
          <div class="completion-rate">
            <el-progress
              type="dashboard"
              :percentage="stats.completionRate"
              :color="getProgressColor(stats.completionRate)"
            >
              <template #default="{ percentage }">
                <span class="percentage-value">{{ percentage }}%</span>
              </template>
            </el-progress>
          </div>
        </el-card>
      </el-col>
      <el-col :span="12">
        <el-card>
          <template #header>
            <span>任务优先级分布</span>
          </template>
          <div class="priority-chart">
            <div
              v-for="item in priorityData"
              :key="item.priority"
              class="priority-item"
            >
              <span class="priority-label">{{ item.label }}</span>
              <el-progress
                :percentage="getPercentage(item.count)"
                :color="item.color"
                :stroke-width="20"
              />
              <span class="priority-count">{{ item.count }}</span>
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <el-card class="recent-tasks-card">
      <template #header>
        <span>最近任务</span>
      </template>
      <el-table :data="recentTasks" stripe>
        <el-table-column prop="summary" label="任务标题" min-width="200">
          <template #default="{ row }">
            <span :class="{ completed: row.isCompleted }">{{ row.summary }}</span>
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
            <span v-if="row.dueTime">{{ formatDate(row.dueTime) }}</span>
            <span v-else class="text-muted">未设置</span>
          </template>
        </el-table-column>
        <el-table-column prop="isCompleted" label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="row.isCompleted ? 'success' : 'warning'">
              {{ row.isCompleted ? '已完成' : '进行中' }}
            </el-tag>
          </template>
        </el-table-column>
      </el-table>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { Document, CircleCheck, Clock, Warning } from '@element-plus/icons-vue'
import { useTaskStore } from '../stores/task'
import dayjs from 'dayjs'

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
    { priority: 1, label: '低', color: '#909399' },
    { priority: 2, label: '中', color: '#e6a23c' },
    { priority: 3, label: '高', color: '#f56c6c' },
    { priority: 4, label: '紧急', color: '#f56c6c' },
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
  if (percentage >= 80) return '#67c23a'
  if (percentage >= 50) return '#e6a23c'
  return '#f56c6c'
}

const getPercentage = (count: number) => {
  const total = taskStore.tasks.length
  return total > 0 ? Math.round((count / total) * 100) : 0
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

onMounted(() => {
  taskStore.fetchTasks({ includeCompleted: true, pageSize: 100 })
})
</script>

<style scoped>
.statistics-page {
  padding: 20px;
}

.stat-card {
  margin-bottom: 20px;
}

.stat-content {
  display: flex;
  align-items: center;
  gap: 16px;
}

.stat-icon {
  width: 64px;
  height: 64px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fff;
}

.stat-icon.total {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.stat-icon.completed {
  background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%);
}

.stat-icon.pending {
  background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
}

.stat-icon.overdue {
  background: linear-gradient(135deg, #eb3349 0%, #f45c43 100%);
}

.stat-info {
  flex: 1;
}

.stat-value {
  font-size: 28px;
  font-weight: 600;
  color: #303133;
}

.stat-label {
  font-size: 14px;
  color: #909399;
  margin-top: 4px;
}

.chart-row {
  margin-top: 20px;
}

.completion-rate {
  display: flex;
  justify-content: center;
  padding: 20px;
}

.percentage-value {
  font-size: 28px;
  font-weight: 600;
}

.priority-chart {
  padding: 10px;
}

.priority-item {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 16px;
}

.priority-label {
  width: 40px;
  font-size: 14px;
}

.priority-item .el-progress {
  flex: 1;
}

.priority-count {
  width: 40px;
  text-align: right;
  font-size: 14px;
  color: #606266;
}

.recent-tasks-card {
  margin-top: 20px;
}

.completed {
  text-decoration: line-through;
  color: #999;
}

.text-muted {
  color: #999;
}
</style>
