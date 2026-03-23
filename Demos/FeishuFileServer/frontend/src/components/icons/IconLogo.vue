<template>
  <svg viewBox="0 0 48 48" fill="none" xmlns="http://www.w3.org/2000/svg" :width="size" :height="size">
    <!-- 云朵背景 -->
    <path 
      d="M38 20C38 14.4772 33.5228 10 28 10C23.8994 10 20.3742 12.4672 18.8324 16.0011C18.5568 16.0004 18.2787 16 18 16C11.3726 16 6 21.3726 6 28C6 34.6274 11.3726 40 18 40H34C40.6274 40 46 34.6274 46 28C46 22.7223 42.4449 18.2568 37.6004 16.7963"
      fill="currentColor"
      fill-opacity="0.2"
    />
    <!-- 云朵轮廓 -->
    <path 
      d="M38 20C38 14.4772 33.5228 10 28 10C23.8994 10 20.3742 12.4672 18.8324 16.0011C18.5568 16.0004 18.2787 16 18 16C11.3726 16 6 21.3726 6 28C6 34.6274 11.3726 40 18 40H34C40.6274 40 46 34.6274 46 28C46 22.7223 42.4449 18.2568 37.6004 16.7963"
      stroke="currentColor" 
      stroke-width="2" 
      stroke-linecap="round" 
      stroke-linejoin="round"
    />
    <!-- 文件图标 -->
    <path 
      d="M18 22H30M18 28H26"
      stroke="currentColor" 
      stroke-width="2.5" 
      stroke-linecap="round"
    />
    <!-- 箭头 -->
    <path 
      d="M32 28L36 24L40 28M36 24V34"
      stroke="currentColor" 
      stroke-width="2" 
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
