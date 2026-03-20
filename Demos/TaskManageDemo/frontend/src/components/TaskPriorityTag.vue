<template>
  <el-tag :type="tagType" :size="size" :effect="effect">
    {{ priorityLabel }}
  </el-tag>
</template>

<script setup lang="ts">
import { computed } from 'vue'

interface Props {
  priority: number
  size?: 'large' | 'default' | 'small'
  effect?: 'dark' | 'light' | 'plain'
}

const props = withDefaults(defineProps<Props>(), {
  size: 'default',
  effect: 'light'
})

const priorityConfig: Record<number, { label: string; type: '' | 'success' | 'warning' | 'danger' | 'info' }> = {
  0: { label: '无', type: 'info' },
  1: { label: '低', type: 'info' },
  2: { label: '中', type: '' },
  3: { label: '高', type: 'warning' },
  4: { label: '紧急', type: 'danger' }
}

const tagType = computed(() => priorityConfig[props.priority]?.type ?? 'info')
const priorityLabel = computed(() => priorityConfig[props.priority]?.label ?? '未知')
</script>
