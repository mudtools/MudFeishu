<script setup lang="ts">
import { ref, computed } from 'vue'
import { ElMessage } from 'element-plus'

const props = defineProps<{
  visible: boolean
  spaceId: string
  parentToken?: string
}>()

const emit = defineEmits<{
  'update:visible': [value: boolean]
  'created': []
}>()

const dialogVisible = computed({
  get: () => props.visible,
  set: (value) => emit('update:visible', value)
})

const newDocTitle = ref('')
const newDocType = ref('docx')
const creating = ref(false)

const docTypes = [
  { value: 'docx', label: '文档', icon: 'Document' },
  { value: 'sheet', label: '表格', icon: 'Grid' },
  { value: 'bitable', label: '多维表格', icon: 'Table' },
  { value: 'slides', label: '幻灯片', icon: 'PictureFilled' },
  { value: 'mindnote', label: '思维导图', icon: 'Share' }
]

async function handleCreate() {
  if (!newDocTitle.value.trim()) {
    ElMessage.warning('请输入文档标题')
    return
  }

  try {
    creating.value = true
    const { wikiApi } = await import('@/api')
    const response = await wikiApi.createNode(props.spaceId, {
      spaceId: props.spaceId,
      parentNodeToken: props.parentToken,
      title: newDocTitle.value,
      objType: newDocType.value
    })

    if (response.data.success) {
      ElMessage.success('创建成功')
      dialogVisible.value = false
      resetForm()
      emit('created')
    }
  } catch (error: any) {
    ElMessage.error(error.message || '创建失败')
  } finally {
    creating.value = false
  }
}

function resetForm() {
  newDocTitle.value = ''
  newDocType.value = 'docx'
}

function handleClose() {
  dialogVisible.value = false
  resetForm()
}
</script>

<template>
  <el-dialog 
    :model-value="visible" 
    title="新建文档" 
    width="480px"
    @update:model-value="emit('update:visible', $event)"
    @close="handleClose"
  >
    <el-form label-width="80px">
      <el-form-item label="文档类型">
        <el-select v-model="newDocType" style="width: 100%">
          <el-option 
            v-for="type in docTypes" 
            :key="type.value" 
            :label="type.label" 
            :value="type.value"
          />
        </el-select>
      </el-form-item>
      <el-form-item label="标题" required>
        <el-input 
          v-model="newDocTitle" 
          placeholder="请输入文档标题" 
          @keyup.enter="handleCreate"
        />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button type="primary" :loading="creating" @click="handleCreate">
        创建
      </el-button>
    </template>
  </el-dialog>
</template>
