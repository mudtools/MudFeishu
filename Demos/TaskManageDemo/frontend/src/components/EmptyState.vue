<template>
  <div class="empty-state">
    <el-empty :description="description" :image-size="imageSize">
      <template v-if="showAction" #default>
        <el-button type="primary" @click="$emit('action')">
          {{ actionText }}
        </el-button>
      </template>
    </el-empty>
  </div>
</template>

<script setup lang="ts">
interface Props {
  description?: string
  imageSize?: number
  showAction?: boolean
  actionText?: string
}

withDefaults(defineProps<Props>(), {
  description: '暂无数据',
  imageSize: 120,
  showAction: false,
  actionText: '立即创建'
})

defineEmits<{
  action: []
}>()
</script>

<style scoped>
.empty-state {
  padding: 40px 0;
  display: flex;
  justify-content: center;
  align-items: center;
}
</style>
