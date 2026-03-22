<template>
  <el-dialog
    v-model="visible"
    title="移动到"
    width="500px"
    @close="handleClose"
  >
    <div class="folder-tree-container">
      <el-tree
        ref="treeRef"
        :data="folderTreeData"
        :props="treeProps"
        node-key="folderToken"
        :expand-on-click-node="false"
        :default-expand-all="true"
        highlight-current
        @node-click="handleNodeClick"
      >
        <template #default="{ node, data }">
          <div class="tree-node" :class="{ 'is-root': !data.folderToken }">
            <el-icon class="folder-icon">
              <Folder v-if="data.folderToken" />
              <HomeFilled v-else />
            </el-icon>
            <span>{{ node.label }}</span>
          </div>
        </template>
      </el-tree>
    </div>
    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button type="primary" @click="handleSubmit" :loading="loading">确定</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { ElMessage, ElTree } from 'element-plus'
import { Folder, HomeFilled } from '@element-plus/icons-vue'
import { folderApi, batchApi } from '@/api'
import { useFileStore } from '@/stores/fileStore'
import type { FolderResponse } from '@/api/types'

const props = defineProps<{
  itemToken: string
  itemType: 'file' | 'folder' | 'batch'
}>()

const emit = defineEmits<{
  close: []
  success: []
}>()

const visible = ref(true)
const loading = ref(false)
const folders = ref<FolderResponse[]>([])
const selectedFolder = ref<string | null>(null)
const fileStore = useFileStore()

const treeProps = {
  children: 'children',
  label: 'folderName'
}

// 添加根目录选项
const folderTreeData = computed(() => {
  return [
    {
      folderToken: '',
      folderName: '根目录',
      children: folders.value
    }
  ]
})

const loadFolders = async () => {
  try {
    const response = await folderApi.getList()
    folders.value = buildTree(response.folders)
  } catch (error) {
    ElMessage.error('加载文件夹失败')
  }
}

const buildTree = (folderList: FolderResponse[]): any[] => {
  const map = new Map<string, any>()
  const roots: any[] = []

  folderList.forEach(folder => {
    map.set(folder.folderToken, { ...folder, children: [] })
  })

  folderList.forEach(folder => {
    const node = map.get(folder.folderToken)
    if (folder.parentFolderToken) {
      const parent = map.get(folder.parentFolderToken)
      if (parent) {
        parent.children.push(node)
      } else {
        roots.push(node)
      }
    } else {
      roots.push(node)
    }
  })

  return roots
}

const handleNodeClick = (data: FolderResponse) => {
  selectedFolder.value = data.folderToken || null
}

const handleSubmit = async () => {
  loading.value = true
  try {
    const targetFolderToken = selectedFolder.value || ''

    if (props.itemType === 'batch') {
      // 批量移动：使用选中的所有文件
      const selectedFiles = fileStore.selectedFiles
      if (selectedFiles.length === 0) {
        ElMessage.warning('没有选中任何文件')
        return
      }
      await batchApi.move({
        fileTokens: selectedFiles,
        folderTokens: [],
        targetFolderToken: targetFolderToken
      })
      fileStore.clearSelection()
    } else if (props.itemType === 'folder') {
      // 移动文件夹
      await folderApi.update(props.itemToken, {
        parentFolderToken: targetFolderToken || undefined
      })
    } else {
      // 移动单个文件
      await batchApi.move({
        fileTokens: [props.itemToken],
        folderTokens: [],
        targetFolderToken: targetFolderToken
      })
    }
    
    ElMessage.success('移动成功')
    emit('success')
    handleClose()
  } catch (error) {
    ElMessage.error('移动失败')
  } finally {
    loading.value = false
  }
}

const handleClose = () => {
  emit('close')
}

onMounted(() => {
  loadFolders()
})
</script>

<style scoped lang="scss">
.folder-tree-container {
  height: 300px;
  overflow-y: auto;
  border: 1px solid var(--el-border-color-light);
  border-radius: 4px;
  padding: 8px;
}

.tree-node {
  display: flex;
  align-items: center;
  gap: 8px;

  .folder-icon {
    color: #ffc107;
  }
}
</style>
