<template>
  <div class="pagination-wrapper" v-if="totalPages > 1">
    <el-pagination v-model:current-page="currentPage" v-model:page-size="pageSize" :page-sizes="pageSizes" :total="total" :layout="layout" @size-change="handleSizeChange" @current-change="handleCurrentChange" />
  </div>
</template>

<script setup lang="ts">
import { computed } from "vue"

interface Props {
  total: number
  page: number
  limit: number
  pageSizes?: number[]
  layout?: string
}

const props = withDefaults(defineProps<Props>(), {
  pageSizes: () => [10, 20, 50, 100],
  layout: "total, sizes, prev, pager, next, jumper",
})

const emit = defineEmits<{
  "update:page": [value: number]
  "update:limit": [value: number]
  change: []
}>()

const currentPage = computed({
  get: () => props.page,
  set: (val) => emit("update:page", val),
})

const pageSize = computed({
  get: () => props.limit,
  set: (val) => emit("update:limit", val),
})

const totalPages = computed(() => Math.ceil(props.total / props.limit))

const handleSizeChange = (val: number) => {
  emit("update:limit", val)
  emit("change")
}

const handleCurrentChange = (val: number) => {
  emit("update:page", val)
  emit("change")
}
</script>

<style scoped>
.pagination-wrapper {
  display: flex;
  justify-content: flex-end;
  margin-top: 16px;
  padding: 16px 0;
}
</style>
