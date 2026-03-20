<template>
  <slot v-if="!hasError" />
  <div v-else class="error-boundary">
    <el-result icon="error" title="页面出错了" :sub-title="errorMessage">
      <template #extra>
        <el-button type="primary" @click="handleRetry">重试</el-button>
        <el-button @click="handleGoHome">返回首页</el-button>
      </template>
    </el-result>
  </div>
</template>

<script setup lang="ts">
import { ref, onErrorCaptured, type ComponentPublicInstance } from "vue"
import { useRouter } from "vue-router"
import { ElMessage } from "element-plus"

interface Props {
  fallbackMessage?: string
}

const props = withDefaults(defineProps<Props>(), {
  fallbackMessage: "页面加载失败，请稍后重试",
})

const emit = defineEmits<{
  error: [error: Error, instance: ComponentPublicInstance | null, info: string]
}>()

const router = useRouter()
const hasError = ref(false)
const errorMessage = ref("")

onErrorCaptured(
  (error: Error, instance: ComponentPublicInstance | null, info: string) => {
    hasError.value = true
    errorMessage.value = error.message || props.fallbackMessage

    console.error("Error captured by ErrorBoundary:", error)
    console.error("Component:", instance)
    console.error("Error info:", info)

    emit("error", error, instance, info)

    return false
  }
)

const handleRetry = () => {
  hasError.value = false
  errorMessage.value = ""
  ElMessage.success("正在重试...")
}

const handleGoHome = () => {
  hasError.value = false
  errorMessage.value = ""
  router.push("/")
}
</script>

<style scoped>
.error-boundary {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 400px;
  padding: 40px;
}
</style>
