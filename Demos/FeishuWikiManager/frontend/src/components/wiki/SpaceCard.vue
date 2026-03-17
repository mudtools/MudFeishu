<script setup lang="ts">
import type { Space } from '@/types'

defineProps<{
  space: Space
}>()

defineEmits<{
  click: []
}>()

function getSpaceTypeLabel(type: string) {
  const labels: Record<string, string> = {
    team: '团队空间',
    my_library: '个人空间',
    personal: '个人空间'
  }
  return labels[type] || type
}

function getVisibilityLabel(visibility: string) {
  const labels: Record<string, string> = {
    public: '公开',
    private: '私有'
  }
  return labels[visibility] || visibility
}
</script>

<template>
  <el-card class="space-card" shadow="hover" @click="$emit('click')">
    <div class="space-icon">
      <el-icon :size="32" color="#3370ff"><Folder /></el-icon>
    </div>
    
    <div class="space-content">
      <h3 class="space-name">{{ space.name }}</h3>
      <p class="space-desc">{{ space.description || '暂无描述' }}</p>
      
      <div class="space-meta">
        <el-tag size="small" type="info">{{ getSpaceTypeLabel(space.spaceType) }}</el-tag>
        <el-tag size="small">{{ getVisibilityLabel(space.visibility) }}</el-tag>
      </div>
    </div>
  </el-card>
</template>

<style scoped>
.space-card {
  cursor: pointer;
  transition: all var(--transition-normal);
  margin-bottom: 20px;
  border: 1px solid var(--card-border);
  background: var(--card-bg);
  border-radius: var(--border-radius-lg);
}

.space-card:hover {
  transform: translateY(-4px);
  box-shadow: var(--shadow-xl);
  border-color: var(--primary-color);
}

.space-icon {
  width: 56px;
  height: 56px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--primary-bg);
  border-radius: var(--border-radius);
  margin-bottom: 16px;
}

.space-icon .el-icon {
  color: var(--primary-color);
}

.space-content {
  min-width: 0;
}

.space-name {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0 0 8px 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.space-desc {
  font-size: 13px;
  color: var(--text-secondary);
  margin: 0 0 16px 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.space-meta {
  display: flex;
  gap: 8px;
}
</style>
