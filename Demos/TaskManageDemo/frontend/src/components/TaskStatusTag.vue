<template>
  <el-tag :type="tagType" :size="size" :effect="effect">
    {{ statusLabel }}
  </el-tag>
</template>

<script setup lang="ts">
import { computed } from 'vue'

interface Props {
  isCompleted: boolean
  isOverdue?: boolean
  size?: 'large' | 'default' | 'small'
  effect?: 'dark' | 'light' | 'plain'
}

const props = withDefaults(defineProps<Props>(), {
  isOverdue: false,
  size: 'default',
  effect: 'light'
})

const tagType = computed(() => {
  if (props.isCompleted) return 'success'
  if (props.isOverdue) return 'danger'
  return 'warning'
})

const statusLabel = computed(() => {
  if (props.isCompleted) return '已完成'
  if (props.isOverdue) return '已逾期'
  return '进行中'
})
</script>
