<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import type { Node, FavoriteNode } from '@/types'

const props = defineProps<{
  nodes: Node[]
  favorites?: FavoriteNode[]
  spaceId?: string
}>()

const emit = defineEmits<{
  nodeExpand: [node: Node]
  toggleFavorite: [node: Node]
}>()

const router = useRouter()
const expandedKeys = ref<Set<string>>(new Set())

function getObjTypeIcon(objType: string) {
  const icons: Record<string, string> = {
    docx: 'Document',
    sheet: 'Grid',
    bitable: 'Table',
    slides: 'PictureFilled',
    mindnote: 'Share',
    file: 'Document'
  }
  return icons[objType] || 'Document'
}

function isFavorite(nodeToken: string) {
  return props.favorites?.some(f => f.nodeToken === nodeToken) ?? false
}

function handleNodeClick(node: Node) {
  if (props.spaceId) {
    router.push(`/spaces/${props.spaceId}/nodes/${node.nodeToken}`)
  }
}

function handleExpand(node: Node) {
  if (expandedKeys.value.has(node.nodeToken)) {
    expandedKeys.value.delete(node.nodeToken)
  } else {
    expandedKeys.value.add(node.nodeToken)
    emit('nodeExpand', node)
  }
}

function handleToggleFavorite(node: Node, event: Event) {
  event.stopPropagation()
  emit('toggleFavorite', node)
}
</script>

<template>
  <div class="node-tree">
    <div v-if="nodes.length === 0" class="empty-tree">
      <el-empty description="暂无文档" :image-size="80" />
    </div>
    
    <div v-else class="tree-content">
      <div 
        v-for="node in nodes" 
        :key="node.nodeToken" 
        class="tree-node"
      >
        <div class="node-content" @click="handleNodeClick(node)">
          <span 
            v-if="node.hasChildren" 
            class="expand-icon"
            @click.stop="handleExpand(node)"
          >
            <el-icon :class="{ expanded: expandedKeys.has(node.nodeToken) }">
              <ArrowRight />
            </el-icon>
          </span>
          <span v-else class="expand-placeholder" />
          
          <el-icon class="obj-type-icon" :class="`${node.objType}-icon`">
            <component :is="getObjTypeIcon(node.objType)" />
          </el-icon>
          
          <span class="node-title">{{ node.title }}</span>
          
          <el-button
            text
            size="small"
            class="favorite-btn"
            @click="handleToggleFavorite(node, $event)"
          >
            <el-icon :color="isFavorite(node.nodeToken) ? '#e6a23c' : '#909399'">
              <Star v-if="isFavorite(node.nodeToken)" />
              <StarFilled v-else />
            </el-icon>
          </el-button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.node-tree {
  min-height: 200px;
}

.empty-tree {
  padding: 40px 0;
}

.tree-content {
  padding: 8px 0;
}

.tree-node {
  margin-bottom: 2px;
}

.node-content {
  display: flex;
  align-items: center;
  padding: 8px 12px;
  border-radius: 4px;
  cursor: pointer;
  transition: background-color 0.2s;
}

.node-content:hover {
  background-color: #f0f2f5;
}

.expand-icon {
  width: 20px;
  height: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 4px;
}

.expand-icon .el-icon {
  transition: transform 0.2s;
}

.expand-icon .el-icon.expanded {
  transform: rotate(90deg);
}

.expand-placeholder {
  width: 24px;
}

.obj-type-icon {
  margin-right: 8px;
  font-size: 16px;
}

.docx-icon { color: #3370ff; }
.sheet-icon { color: #67c23a; }
.bitable-icon { color: #e6a23c; }
.slides-icon { color: #f56c6c; }
.mindnote-icon { color: #9c27b0; }
.file-icon { color: #606266; }

.node-title {
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-size: 14px;
  color: var(--text-primary);
}

.favorite-btn {
  opacity: 0;
  transition: opacity 0.2s;
}

.node-content:hover .favorite-btn {
  opacity: 1;
}
</style>
