<template>
  <el-breadcrumb separator="/" class="breadcrumb-nav">
    <el-breadcrumb-item :to="{ path: '/' }">
      <el-icon><HomeFilled /></el-icon>
    </el-breadcrumb-item>
    <el-breadcrumb-item v-for="(item, index) in breadcrumbs" :key="index" :to="item.path">
      {{ item.title }}
    </el-breadcrumb-item>
  </el-breadcrumb>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { HomeFilled } from '@element-plus/icons-vue'

const route = useRoute()

const breadcrumbs = computed(() => {
  const matched = route.matched.filter(item => item.meta && item.meta.title)
  return matched.map(item => ({
    title: item.meta.title as string,
    path: item.path
  }))
})
</script>

<style scoped>
.breadcrumb-nav {
  font-size: 14px;
}

.breadcrumb-nav :deep(.el-breadcrumb__item) {
  display: flex;
  align-items: center;
}

.breadcrumb-nav :deep(.el-breadcrumb__inner) {
  display: flex;
  align-items: center;
  gap: 4px;
  color: var(--text-secondary);
  font-weight: 500;
}

.breadcrumb-nav :deep(.el-breadcrumb__inner.is-link:hover) {
  color: var(--primary-color);
}

.breadcrumb-nav :deep(.el-breadcrumb__separator) {
  color: var(--text-muted);
}
</style>
