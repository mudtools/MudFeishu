<template>
  <div class="table-container">
    <!-- 加载状态 -->
    <LoadingWrapper :loading="loading">
      <!-- 空状态 -->
      <EmptyState
        v-if="showEmpty && (!data || data.length === 0)"
        :title="emptyTitle"
        :description="emptyDescription"
      >
        <template #action>
          <slot name="empty-action" />
        </template>
      </EmptyState>

      <!-- 内容插槽 -->
      <div v-else class="table-content">
        <slot />
      </div>
    </LoadingWrapper>

    <!-- 分页 -->
    <div v-if="showPagination && total > 0" class="table-pagination">
      <Pagination
        :page="currentPage"
        :limit="pageSize"
        :total="total"
        :page-sizes="pageSizes"
        @update:page="handlePageChange"
        @update:limit="handleSizeChange"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
// import { computed } from 'vue'
import LoadingWrapper from './LoadingWrapper.vue'
import EmptyState from './EmptyState.vue'
import Pagination from './Pagination.vue'

interface Props {
  /** 加载状态 */
  loading?: boolean
  /** 数据列表 */
  data?: unknown[]
  /** 是否显示空状态 */
  showEmpty?: boolean
  /** 空状态标题 */
  emptyTitle?: string
  /** 空状态描述 */
  emptyDescription?: string
  /** 是否显示分页 */
  showPagination?: boolean
  /** 当前页码 */
  currentPage?: number
  /** 每页大小 */
  pageSize?: number
  /** 总条数 */
  total?: number
  /** 每页大小选项 */
  pageSizes?: number[]
}

const props = withDefaults(defineProps<Props>(), {
  loading: false,
  data: () => [],
  showEmpty: true,
  emptyTitle: '暂无数据',
  emptyDescription: '',
  showPagination: true,
  currentPage: 1,
  pageSize: 20,
  total: 0,
  pageSizes: () => [10, 20, 50, 100],
})

const emit = defineEmits<{
  'page-change': [page: number]
  'size-change': [size: number]
  'pagination-change': [page: number, size: number]
}>()

const handlePageChange = (page: number) => {
  emit('page-change', page)
  emit('pagination-change', page, props.pageSize)
}

const handleSizeChange = (size: number) => {
  emit('size-change', size)
  emit('pagination-change', props.currentPage, size)
}
</script>

<style scoped>
.table-container {
  width: 100%;
}

.table-content {
  min-height: 200px;
}

.table-pagination {
  display: flex;
  justify-content: flex-end;
  padding: 16px 0;
  border-top: 1px solid var(--el-border-color-lighter);
}
</style>
