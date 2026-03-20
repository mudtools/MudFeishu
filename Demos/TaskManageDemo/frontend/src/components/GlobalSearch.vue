<template>
  <div class="global-search">
    <el-input v-model="searchQuery" placeholder="搜索任务、清单、模板..." prefix-icon="Search" clearable @focus="isFocused = true" @blur="handleBlur" @keyup.enter="handleSearch">
      <template #prefix>
        <el-icon>
          <Search />
        </el-icon>
      </template>
      <template #suffix>
        <kbd class="keyboard-shortcut">⌘K</kbd>
      </template>
    </el-input>

    <transition name="fade">
      <div v-if="isFocused && (recentSearches.length > 0 || searchResults.length > 0)" class="search-dropdown">
        <div v-if="recentSearches.length > 0 && !searchQuery" class="search-section">
          <div class="section-title">最近搜索</div>
          <div v-for="(item, index) in recentSearches" :key="index" class="search-item" @click="selectSearch(item)">
            <el-icon>
              <Clock />
            </el-icon>
            <span>{{ item }}</span>
          </div>
        </div>

        <div v-if="searchResults.length > 0" class="search-section">
          <div class="section-title">搜索结果</div>
          <div v-for="(item, index) in searchResults" :key="index" class="search-item" @click="selectResult(item)">
            <el-icon>
              <Document />
            </el-icon>
            <div class="result-info">
              <span class="result-title">{{ item.title }}</span>
              <span class="result-type">{{ item.type }}</span>
            </div>
          </div>
        </div>
      </div>
    </transition>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from "vue"
import { Search, Clock, Document } from "@element-plus/icons-vue"
import { useRouter } from "vue-router"

const router = useRouter()

const searchQuery = ref("")
const isFocused = ref(false)
const recentSearches = ref(["项目进度", "周报", "设计评审"])
const searchResults = ref<Array<{ title: string; type: string; id: number }>>(
  []
)

// 模拟搜索结果
watch(searchQuery, (val) => {
  if (val) {
    searchResults.value = [
      { title: `${val} - 任务1`, type: "任务", id: 1 },
      { title: `${val} - 清单A`, type: "清单", id: 2 },
      { title: `${val} - 模板B`, type: "模板", id: 3 },
    ]
  } else {
    searchResults.value = []
  }
})

const handleBlur = () => {
  setTimeout(() => {
    isFocused.value = false
  }, 200)
}

const handleSearch = () => {
  if (searchQuery.value) {
    router.push({ path: "/tasks", query: { search: searchQuery.value } })
    isFocused.value = false
  }
}

const selectSearch = (item: string) => {
  searchQuery.value = item
  handleSearch()
}

const selectResult = (item: { title: string; type: string; id: number }) => {
  if (item.type === "任务") {
    router.push(`/tasks/${item.id}`)
  }
  isFocused.value = false
}

// 键盘快捷键
const handleKeydown = (e: KeyboardEvent) => {
  if ((e.metaKey || e.ctrlKey) && e.key === "k") {
    e.preventDefault()
    const input = document.querySelector(
      ".global-search input"
    ) as HTMLInputElement
    input?.focus()
  }
}

// 添加键盘监听
document.addEventListener("keydown", handleKeydown)
</script>

<style scoped>
.global-search {
  position: relative;
  width: 100%;
}

.global-search :deep(.el-input__wrapper) {
  background: var(--bg-tertiary);
  border: none;
  border-radius: var(--radius-full);
  padding: 0 16px;
  height: 44px;
}

.global-search :deep(.el-input__inner) {
  background: transparent;
  font-size: 14px;
}

.keyboard-shortcut {
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: var(--radius-sm);
  padding: 2px 6px;
  font-size: 11px;
  color: var(--text-muted);
  font-family: var(--font-mono);
}

.search-dropdown {
  position: absolute;
  top: calc(100% + 8px);
  left: 0;
  right: 0;
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-xl);
  z-index: 1000;
  padding: 8px;
  max-height: 400px;
  overflow-y: auto;
}

.search-section {
  margin-bottom: 8px;
}

.search-section:last-child {
  margin-bottom: 0;
}

.section-title {
  font-size: 11px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  color: var(--text-muted);
  padding: 8px 12px;
}

.search-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px 12px;
  border-radius: var(--radius-md);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.search-item:hover {
  background: var(--primary-bg);
}

.search-item .el-icon {
  color: var(--text-muted);
  font-size: 16px;
}

.result-info {
  display: flex;
  flex-direction: column;
  flex: 1;
}

.result-title {
  font-size: 14px;
  color: var(--text-primary);
  font-weight: 500;
}

.result-type {
  font-size: 12px;
  color: var(--text-muted);
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}
</style>
