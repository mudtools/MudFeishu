<template>
  <div class="not-found-page">
    <div class="not-found-content">
      <h1>404</h1>
      <h2>页面不存在</h2>
      <p>抱歉，您访问的页面不存在或已被移除</p>
      <el-button type="primary" @click="goHome">返回首页</el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'

const router = useRouter()

const goHome = () => {
  router.push('/')
}
</script>

<style scoped>
.not-found-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #f5f7fa;
}

.not-found-content {
  text-align: center;
}

.not-found-content h1 {
  font-size: 120px;
  margin: 0;
  color: #409eff;
  font-weight: 600;
}

.not-found-content h2 {
  font-size: 24px;
  margin: 16px 0;
  color: #303133;
}

.not-found-content p {
  color: #909399;
  margin-bottom: 24px;
}
</style>
