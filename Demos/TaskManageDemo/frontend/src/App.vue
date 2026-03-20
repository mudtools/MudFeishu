<template>
  <el-config-provider :locale="zhCn">
    <ErrorBoundary>
      <router-view />
    </ErrorBoundary>
  </el-config-provider>
</template>

<script setup lang="ts">
import zhCn from "element-plus/es/locale/lang/zh-cn"
import { ErrorBoundary } from "./components"
</script>

<style>
#app {
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto,
    "Helvetica Neue", Arial, "Noto Sans", sans-serif;
}
</style>
