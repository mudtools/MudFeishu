<template>
  <div class="templates-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <span>任务模板</span>
          <el-button type="primary" @click="showCreateDialog">新建模板</el-button>
        </div>
      </template>

      <el-table v-loading="loading" :data="templates" stripe>
        <el-table-column prop="name" label="模板名称" min-width="150" />
        <el-table-column prop="description" label="描述" min-width="200">
          <template #default="{ row }">
            {{ row.description || '暂无描述' }}
          </template>
        </el-table-column>
        <el-table-column prop="defaultPriority" label="默认优先级" width="120">
          <template #default="{ row }">
            <el-tag :type="getPriorityType(row.defaultPriority)">
              {{ getPriorityLabel(row.defaultPriority) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="defaultDueDays" label="默认截止天数" width="120">
          <template #default="{ row }">
            {{ row.defaultDueDays ? `${row.defaultDueDays} 天` : '未设置' }}
          </template>
        </el-table-column>
        <el-table-column prop="isPublic" label="公开" width="80">
          <template #default="{ row }">
            <el-tag :type="row.isPublic ? 'success' : 'info'">
              {{ row.isPublic ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="250" fixed="right">
          <template #default="{ row }">
            <el-button type="success" size="small" text @click="useTemplate(row)">
              使用模板
            </el-button>
            <el-button type="primary" size="small" text @click="handleEdit(row)">
              编辑
            </el-button>
            <el-button type="danger" size="small" text @click="handleDelete(row)">
              删除
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="editingTemplate ? '编辑模板' : '新建模板'" width="600px">
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="100px">
        <el-form-item label="模板名称" prop="name">
          <el-input v-model="form.name" placeholder="请输入模板名称" />
        </el-form-item>
        <el-form-item label="描述" prop="description">
          <el-input v-model="form.description" type="textarea" :rows="2" placeholder="请输入描述" />
        </el-form-item>
        <el-form-item label="默认标题" prop="defaultSummary">
          <el-input v-model="form.defaultSummary" placeholder="使用模板创建任务时的默认标题" />
        </el-form-item>
        <el-form-item label="默认描述" prop="defaultDescription">
          <el-input v-model="form.defaultDescription" type="textarea" :rows="2" placeholder="默认描述" />
        </el-form-item>
        <el-form-item label="默认优先级" prop="defaultPriority">
          <el-select v-model="form.defaultPriority" placeholder="选择优先级">
            <el-option label="低" :value="1" />
            <el-option label="中" :value="2" />
            <el-option label="高" :value="3" />
            <el-option label="紧急" :value="4" />
          </el-select>
        </el-form-item>
        <el-form-item label="默认截止天数" prop="defaultDueDays">
          <el-input-number v-model="form.defaultDueDays" :min="1" :max="365" />
          <span class="form-tip">从创建时间开始计算</span>
        </el-form-item>
        <el-form-item label="公开模板" prop="isPublic">
          <el-switch v-model="form.isPublic" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="handleSubmit">
          {{ editingTemplate ? '保存' : '创建' }}
        </el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="useDialogVisible" title="使用模板创建任务" width="500px">
      <el-form :model="useForm" label-width="80px">
        <el-form-item label="任务标题">
          <el-input v-model="useForm.summary" placeholder="留空则使用模板默认标题" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="useDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="usingTemplate" @click="handleUseTemplate">
          创建任务
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from "vue"
import { useRouter } from "vue-router"
import { ElMessage, ElMessageBox } from "element-plus"
import type { FormInstance, FormRules } from "element-plus"
import { templateApi } from "../api"
import type { TaskTemplate, CreateTaskTemplateRequest } from "../types"

const router = useRouter()
const loading = ref(false)
const templates = ref<TaskTemplate[]>([])
const dialogVisible = ref(false)
const editingTemplate = ref<TaskTemplate | null>(null)
const submitting = ref(false)
const formRef = ref<FormInstance>()

const useDialogVisible = ref(false)
const usingTemplate = ref(false)
const selectedTemplate = ref<TaskTemplate | null>(null)
const useForm = reactive({ summary: "" })

const form = reactive<CreateTaskTemplateRequest>({
  name: "",
  description: "",
  defaultSummary: "",
  defaultDescription: "",
  defaultPriority: 2,
  defaultDueDays: undefined,
  isPublic: true,
})

const formRules: FormRules = {
  name: [{ required: true, message: "请输入模板名称", trigger: "blur" }],
}

const getPriorityType = (priority: number) => {
  const types: Record<number, string> = {
    1: "info",
    2: "warning",
    3: "danger",
    4: "danger",
  }
  return types[priority] || "info"
}

const getPriorityLabel = (priority: number) => {
  const labels: Record<number, string> = {
    1: "低",
    2: "中",
    3: "高",
    4: "紧急",
  }
  return labels[priority] || "未设置"
}

const fetchTemplates = async () => {
  loading.value = true
  try {
    templates.value = await templateApi.getTemplates()
  } catch {
    ElMessage.error("获取模板列表失败")
  } finally {
    loading.value = false
  }
}

const showCreateDialog = () => {
  editingTemplate.value = null
  Object.assign(form, {
    name: "",
    description: "",
    defaultSummary: "",
    defaultDescription: "",
    defaultPriority: 2,
    defaultDueDays: undefined,
    isPublic: true,
  })
  dialogVisible.value = true
}

const handleEdit = (template: TaskTemplate) => {
  editingTemplate.value = template
  Object.assign(form, {
    name: template.name,
    description: template.description || "",
    defaultSummary: template.defaultSummary || "",
    defaultDescription: template.defaultDescription || "",
    defaultPriority: template.defaultPriority,
    defaultDueDays: template.defaultDueDays,
    isPublic: template.isPublic,
  })
  dialogVisible.value = true
}

const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    submitting.value = true
    try {
      if (editingTemplate.value) {
        await templateApi.updateTemplate(editingTemplate.value.id, form)
        ElMessage.success("模板更新成功")
      } else {
        await templateApi.createTemplate(form)
        ElMessage.success("模板创建成功")
      }
      dialogVisible.value = false
      fetchTemplates()
    } catch {
      ElMessage.error(editingTemplate.value ? "模板更新失败" : "模板创建失败")
    } finally {
      submitting.value = false
    }
  })
}

const handleDelete = async (template: TaskTemplate) => {
  try {
    await ElMessageBox.confirm("确定要删除此模板吗？", "确认删除", {
      type: "warning",
    })
    await templateApi.deleteTemplate(template.id)
    ElMessage.success("模板已删除")
    fetchTemplates()
  } catch {}
}

const useTemplate = (template: TaskTemplate) => {
  selectedTemplate.value = template
  useForm.summary = template.defaultSummary || ""
  useDialogVisible.value = true
}

const handleUseTemplate = async () => {
  if (!selectedTemplate.value) return
  usingTemplate.value = true
  try {
    const task = await templateApi.createTaskFromTemplate(
      selectedTemplate.value.id,
      useForm.summary || undefined
    )
    ElMessage.success("任务创建成功")
    useDialogVisible.value = false
    router.push(`/tasks/${task.id}`)
  } catch {
    ElMessage.error("任务创建失败")
  } finally {
    usingTemplate.value = false
  }
}

onMounted(() => fetchTemplates())
</script>

<style scoped>
.templates-page {
  padding: 20px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.form-tip {
  margin-left: 10px;
  color: #909399;
  font-size: 12px;
}
</style>
