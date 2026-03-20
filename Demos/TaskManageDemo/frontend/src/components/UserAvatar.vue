<template>
  <el-tooltip :content="name" placement="top" :disabled="!showTooltip">
    <el-avatar
      :size="size"
      :src="avatarUrl"
      :class="['user-avatar', { 'clickable': clickable }]"
      @click="handleClick"
    >
      {{ initials }}
    </el-avatar>
  </el-tooltip>
</template>

<script setup lang="ts">
import { computed } from 'vue'

interface Props {
  name: string
  avatarUrl?: string
  size?: number
  showTooltip?: boolean
  clickable?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  size: 32,
  showTooltip: true,
  clickable: false
})

const emit = defineEmits<{
  click: []
}>()

const initials = computed(() => {
  return props.name?.charAt(0)?.toUpperCase() || '?'
})

const handleClick = () => {
  if (props.clickable) {
    emit('click')
  }
}
</script>

<style scoped>
.user-avatar {
  cursor: default;
}

.user-avatar.clickable {
  cursor: pointer;
  transition: transform 0.2s;
}

.user-avatar.clickable:hover {
  transform: scale(1.1);
}
</style>
