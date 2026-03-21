<template>
  <el-form :inline="true" :model="localForm" class="search-form">
    <el-form-item label="关键词">
      <el-input
        v-model="localForm.keyword"
        placeholder="搜索任务标题或描述"
        clearable
        @keyup.enter="handleSearch"
      />
    </el-form-item>
    <el-form-item label="状态">
      <el-select v-model="localForm.status" placeholder="选择状态" clearable>
        <el-option label="全部" value="all" />
        <el-option label="进行中" value="pending" />
        <el-option label="已完成" value="completed" />
      </el-select>
    </el-form-item>
    <el-form-item label="优先级">
      <el-select v-model="localForm.priority" placeholder="选择优先级" clearable>
        <el-option label="全部" :value="0" />
        <el-option label="低" :value="1" />
        <el-option label="中" :value="2" />
        <el-option label="高" :value="3" />
        <el-option label="紧急" :value="4" />
      </el-select>
    </el-form-item>
    <el-form-item v-if="showDateRange" label="截止时间">
      <el-date-picker
        v-model="dateRange"
        type="daterange"
        range-separator="至"
        start-placeholder="开始日期"
        end-placeholder="结束日期"
        value-format="YYYY-MM-DD"
        @change="handleDateChange"
      />
    </el-form-item>
    <el-form-item>
      <el-button type="primary" @click="handleSearch">搜索</el-button>
      <el-button @click="handleReset">重置</el-button>
    </el-form-item>
  </el-form>
</template>

<script setup lang="ts">
import { reactive, watch, ref } from 'vue'

interface Props {
  keyword?: string
  status?: string
  priority?: number
  showDateRange?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  keyword: '',
  status: 'all',
  priority: undefined,
  showDateRange: false
})

const emit = defineEmits<{
  'update:keyword': [value: string]
  'update:status': [value: string]
  'update:priority': [value: number | undefined]
  search: []
  reset: []
}>()

const localForm = reactive({
  keyword: props.keyword,
  status: props.status,
  priority: props.priority
})

const dateRange = ref<[string, string] | null>(null)

watch(() => props.keyword, (val) => { localForm.keyword = val })
watch(() => props.status, (val) => { localForm.status = val })
watch(() => props.priority, (val) => { localForm.priority = val })

watch(() => localForm.keyword, (val) => emit('update:keyword', val))
watch(() => localForm.status, (val) => emit('update:status', val))
watch(() => localForm.priority, (val) => emit('update:priority', val))

const handleDateChange = (_val: [string, string] | null) => {
  // Handle date range if needed
}

const handleSearch = () => {
  emit('search')
}

const handleReset = () => {
  localForm.keyword = ''
  localForm.status = 'all'
  localForm.priority = undefined
  dateRange.value = null
  emit('reset')
}
</script>

<style scoped>
.search-form {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}
</style>
