<template>
  <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg" :width="size" :height="size">
    <path 
      d="M3 9.5L12 3L21 9.5V19C21 20.1046 20.1046 21 19 21H5C3.89543 21 3 20.1046 3 19V9.5Z"
      fill="currentColor"
      fill-opacity="0.15"
      stroke="currentColor" 
      stroke-width="1.8" 
      stroke-linecap="round" 
      stroke-linejoin="round"
    />
    <path 
      d="M9 21V14H15V21"
      stroke="currentColor" 
      stroke-width="1.8" 
      stroke-linecap="round" 
      stroke-linejoin="round"
    />
  </svg>
</template>

<script setup lang="ts">
defineProps<{
  size?: number | string
}>()
</script>
