<template>
  <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg" :width="size" :height="size">
    <path 
      d="M4 12C4 7.58172 7.58172 4 12 4C14.5 4 16.5 5 18 6.5L20 4.5V10H14.5L16.5 8C15.5 7 13.9 6 12 6C8.68629 6 6 8.68629 6 12C6 15.3137 8.68629 18 12 18C15.3137 18 18 15.3137 18 12H20C20 16.4183 16.4183 20 12 20C7.58172 20 4 16.4183 4 12Z"
      fill="currentColor"
      stroke="currentColor"
      stroke-width="1.5"
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
