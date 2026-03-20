<template>
  <el-container class="app-layout">
    <el-aside width="220px" class="app-aside">
      <div class="logo">
        <h1>任务管理系统</h1>
      </div>
      <el-menu :default-active="activeMenu" class="app-menu" router>
        <el-menu-item index="/tasks">
          <el-icon>
            <Document />
          </el-icon>
          <span>任务列表</span>
        </el-menu-item>
        <el-menu-item index="/kanban">
          <el-icon>
            <Grid />
          </el-icon>
          <span>任务看板</span>
        </el-menu-item>
        <el-menu-item index="/tasklists">
          <el-icon>
            <Folder />
          </el-icon>
          <span>任务清单</span>
        </el-menu-item>
        <el-menu-item index="/templates">
          <el-icon>
            <DocumentCopy />
          </el-icon>
          <span>任务模板</span>
        </el-menu-item>
        <el-menu-item index="/statistics">
          <el-icon>
            <DataAnalysis />
          </el-icon>
          <span>统计报表</span>
        </el-menu-item>
      </el-menu>
    </el-aside>
    <el-container>
      <el-header class="app-header">
        <div class="header-right">
          <el-dropdown>
            <span class="user-info">
              <el-avatar :size="32">U</el-avatar>
              <span class="user-name">用户</span>
            </span>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item>个人设置</el-dropdown-item>
                <el-dropdown-item divided>退出登录</el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </el-header>
      <el-main class="app-main">
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>

<script setup lang="ts">
import { computed } from "vue"
import { useRoute } from "vue-router"
import {
  Document,
  Grid,
  Folder,
  DocumentCopy,
  DataAnalysis,
} from "@element-plus/icons-vue"

const route = useRoute()
const activeMenu = computed(() => route.path)
</script>

<style scoped>
.app-layout {
  height: 100vh;
}

.app-aside {
  background: #001529;
  overflow: hidden;
}

.logo {
  height: 60px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #002140;
}

.logo h1 {
  color: #fff;
  font-size: 18px;
  margin: 0;
  white-space: nowrap;
}

.app-menu {
  border-right: none;
  background: #001529;
}

.app-menu .el-menu-item {
  color: rgba(255, 255, 255, 0.65);
}

.app-menu .el-menu-item:hover {
  background: #1890ff;
  color: #fff;
}

.app-menu .el-menu-item.is-active {
  background: #1890ff;
  color: #fff;
}

.app-header {
  background: #fff;
  box-shadow: 0 1px 4px rgba(0, 21, 41, 0.08);
  display: flex;
  align-items: center;
  justify-content: flex-end;
  padding: 0 20px;
}

.header-right {
  display: flex;
  align-items: center;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
}

.user-name {
  font-size: 14px;
}

.app-main {
  background: #f0f2f5;
  overflow: auto;
}
</style>
