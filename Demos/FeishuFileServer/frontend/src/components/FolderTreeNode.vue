<template>
  <div class="folder-tree-node">
    <div 
      class="folder-item"
      :class="{ 'is-active': currentFolderToken === folder.folderToken }"
      @click="handleClick"
      @contextmenu.prevent="handleContextMenu($event)"
    >
      <el-icon 
        class="expand-icon" 
        :class="{ 'is-expanded': expanded }"
        @click.stop="toggleExpand"
        v-if="hasChildren"
      >
        <CaretRight v-if="!expanded" />
        <CaretBottom v-else />
      </el-icon>
      <span class="expand-placeholder" v-else></span>
      <el-icon class="folder-icon"><Folder /></el-icon>
      <span class="folder-name">{{ folder.folderName }}</span>
      <div class="folder-actions" @click.stop>
        <el-button 
          type="primary" 
          size="small" 
          text
          @click="handleCreate"
          title="新建子文件夹"
        >
          <el-icon><Plus /></el-icon>
        </el-button>
        <el-button 
          type="warning" 
          size="small" 
          text
          @click="handleRename"
          title="重命名"
        >
          <el-icon><Edit /></el-icon>
        </el-button>
        <el-button 
          type="danger" 
          size="small" 
          text
          @click="handleDelete"
          title="删除"
        >
          <el-icon><Delete /></el-icon>
        </el-button>
      </div>
    </div>
    <div v-if="expanded && hasChildren" class="folder-children">
      <FolderTreeNode
        v-for="child in folder.children" 
        :key="child.folderToken"
        :folder="child"
        :current-folder-token="currentFolderToken"
        @node-click="$emit('node-click', $event)"
        @context-menu="$emit('context-menu', $event, $event)"
        @create-folder="$emit('create-folder', $event)"
        @rename-folder="$emit('rename-folder', $event)"
        @delete-folder="$emit('delete-folder', $event)"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { Folder, Edit, Delete, Plus, CaretRight, CaretBottom } from '@element-plus/icons-vue'
import type { FolderTreeNode } from '@/stores/folderStore'

const props = defineProps<{
  folder: FolderTreeNode
  currentFolderToken?: string | null
}>()

const emit = defineEmits<{
  'node-click': [folder: FolderTreeNode]
  'context-menu': [event: MouseEvent, folder: FolderTreeNode]
  'create-folder': [folder: FolderTreeNode]
  'rename-folder': [folder: FolderTreeNode]
  'delete-folder': [folder: FolderTreeNode]
}>()

const expanded = ref(false)
const hasChildren = computed(() => props.folder.children && props.folder.children.length > 0)

const toggleExpand = () => {
  if (hasChildren.value) {
    expanded.value = !expanded.value
  }
}

const handleClick = () => {
  emit('node-click', props.folder)
}

const handleContextMenu = (event: MouseEvent) => {
  emit('context-menu', event, props.folder)
}

const handleCreate = () => {
  emit('create-folder', props.folder)
}

const handleRename = () => {
  emit('rename-folder', props.folder)
}

const handleDelete = () => {
  emit('delete-folder', props.folder)
}
</script>

<style scoped lang="scss">
.folder-tree-node {
  display: flex;
  flex-direction: column;
}

.folder-children {
  display: flex;
  flex-direction: column;
  padding-left: 16px;
}

.expand-icon {
  font-size: 12px;
  color: var(--text-tertiary);
  cursor: pointer;
  transition: all var(--transition-fast);
  width: 16px;
  height: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;

  &:hover {
    color: var(--text-primary);
  }

  &.is-expanded {
    color: var(--primary-color);
  }
}

.expand-placeholder {
  width: 16px;
  height: 16px;
  flex-shrink: 0;
}

.folder-item {
  display: flex;
  align-items: center;
  gap: 2px;
  padding: 4px 8px;
  cursor: pointer;
  border-radius: var(--radius-md);
  transition: all var(--transition-fast);
  position: relative;
  color: var(--text-primary);

  &:hover {
    background: var(--bg-secondary);
    transform: translateX(2px);

    .folder-actions {
      opacity: 1;
    }
  }

  &.is-active {
    background: var(--primary-light);
    color: var(--primary-color);

    .folder-icon {
      color: var(--primary-color);
    }
    
    .folder-name {
      color: var(--primary-color);
      font-weight: 600;
    }
  }

  .folder-icon {
    color: #ffc107;
    font-size: 16px;
    transition: all var(--transition-fast);
    flex-shrink: 0;
  }

  .folder-name {
    flex: 1;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    font-size: 13px;
    min-width: 0;
    color: var(--text-primary);
  }

  .folder-actions {
    opacity: 0;
    transition: opacity var(--transition-fast);
    display: flex;
    align-items: center;
    gap: 0;
    flex-shrink: 0;
    margin-left: auto;

    :deep(.el-button) {
      padding: 2px;
      margin: 0;
      min-width: 20px;
      height: 20px;
      font-size: 12px;
      border-radius: 4px;
      
      .el-icon {
        font-size: 12px;
        margin: 0;
      }
      
      &:hover {
        background: var(--bg-tertiary);
      }
    }
  }
}
</style>
