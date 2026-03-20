<template>
  <el-card class="search-card">
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
          <el-option label="全部" value="" />
          <el-option label="进行中" value="pending" />
          <el-option label="已完成" value="completed" />
        </el-select>
      </el-form-item>
      <el-form-item label="优先级">
        <el-select v-model="localForm.priority" placeholder="选择优先级" clearable>
          <el-option label="全部" :value="null" />
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
  </el-card>
</template>

<script setup lang="ts">
import { reactive, watch, ref } from 'vue'

interface SearchForm {
  keyword: string
  status: string
  priority: number | null
  dueTimeFrom?: string
  dueTimeTo?: string
}

interface Props {
  modelValue: SearchForm
  showDateRange?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  showDateRange: false
})

const emit = defineEmits<{
  'update:modelValue': [value: SearchForm]
  search: []
  reset: []
}>()

const localForm = reactive<SearchForm>({
  keyword: props.modelValue.keyword,
  status: props.modelValue.status,
  priority: props.modelValue.priority
})

const dateRange = ref<[string, string] | null>(null)

watch(() => props.modelValue, (val) => {
  localForm.keyword = val.keyword
  localForm.status = val.status
  localForm.priority = val.priority
}, { deep: true })

watch(localForm, (val) => {
  emit('update:modelValue', { ...val })
}, { deep: true })

const handleDateChange = (val: [string, string] | null) => {
  if (val) {
    localForm.dueTimeFrom = val[0]
    localForm.dueTimeTo = val[1]
  } else {
    delete localForm.dueTimeFrom
    delete localForm.dueTimeTo
  }
}

const handleSearch = () => {
  emit('search')
}

const handleReset = () => {
  localForm.keyword = ''
  localForm.status = ''
  localForm.priority = null
  dateRange.value = null
  delete localForm.dueTimeFrom
  delete localForm.dueTimeTo
  emit('reset')
}
</script>

<style scoped>
.search-card {
  margin-bottom: 16px;
}

.search-form {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}
</style>
