<template>
  <div v-if="loading" class="loading-wrapper">
    <el-skeleton :rows="rows" animated />
  </div>
  <slot v-else />
</template>

<script setup lang="ts">
interface Props {
  loading: boolean
  rows?: number
}

withDefaults(defineProps<Props>(), {
  rows: 5
})
</script>

<style scoped>
.loading-wrapper {
  padding: 20px;
}
</style>
