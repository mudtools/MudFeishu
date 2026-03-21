import { defineStore } from "pinia";
import { ref } from "vue";
import { taskApi } from "../api";
import type { Task, CreateTaskRequest, UpdateTaskRequest } from "../types";
import { useTaskSearchStore } from "./taskSearch";

/**
 * 任务CRUD状态管理
 * 负责任务的创建、读取、更新、删除操作
 */
export const useTaskCrudStore = defineStore("taskCrud", () => {
  // ==================== State ====================
  const currentTask = ref<Task | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const operationInProgress = ref<string | null>(null);

  // ==================== Actions ====================

  /**
   * 获取任务详情
   */
  async function fetchTask(id: number) {
    loading.value = true;
    error.value = null;
    operationInProgress.value = "fetch";
    try {
      currentTask.value = await taskApi.getTask(id);
      return currentTask.value;
    } catch (e) {
      error.value = e instanceof Error ? e.message : "获取任务详情失败";
      currentTask.value = null;
      throw e;
    } finally {
      loading.value = false;
      operationInProgress.value = null;
    }
  }

  /**
   * 创建任务
   */
  async function createTask(request: CreateTaskRequest) {
    loading.value = true;
    error.value = null;
    operationInProgress.value = "create";
    try {
      const task = await taskApi.createTask(request);

      // 同步更新搜索列表
      const searchStore = useTaskSearchStore();
      searchStore.addTaskToList(task);

      return task;
    } catch (e) {
      error.value = e instanceof Error ? e.message : "创建任务失败";
      throw e;
    } finally {
      loading.value = false;
      operationInProgress.value = null;
    }
  }

  /**
   * 更新任务
   */
  async function updateTask(id: number, request: UpdateTaskRequest) {
    loading.value = true;
    error.value = null;
    operationInProgress.value = "update";
    try {
      const task = await taskApi.updateTask(id, request);

      // 更新当前任务
      if (currentTask.value?.id === id) {
        currentTask.value = task;
      }

      // 同步更新搜索列表中的任务
      const searchStore = useTaskSearchStore();
      searchStore.updateTaskInList(task);

      return task;
    } catch (e) {
      error.value = e instanceof Error ? e.message : "更新任务失败";
      throw e;
    } finally {
      loading.value = false;
      operationInProgress.value = null;
    }
  }

  /**
   * 删除任务
   */
  async function deleteTask(id: number) {
    loading.value = true;
    error.value = null;
    operationInProgress.value = "delete";
    try {
      await taskApi.deleteTask(id);

      // 清除当前任务（如果是当前查看的任务）
      if (currentTask.value?.id === id) {
        currentTask.value = null;
      }

      // 同步更新搜索列表
      const searchStore = useTaskSearchStore();
      searchStore.removeTaskFromList(id);
    } catch (e) {
      error.value = e instanceof Error ? e.message : "删除任务失败";
      throw e;
    } finally {
      loading.value = false;
      operationInProgress.value = null;
    }
  }

  /**
   * 完成任务
   */
  async function completeTask(id: number) {
    loading.value = true;
    error.value = null;
    operationInProgress.value = "complete";
    try {
      const task = await taskApi.completeTask(id);

      // 更新当前任务
      if (currentTask.value?.id === id) {
        currentTask.value = task;
      }

      // 同步更新搜索列表
      const searchStore = useTaskSearchStore();
      searchStore.updateTaskInList(task);

      return task;
    } catch (e) {
      error.value = e instanceof Error ? e.message : "完成任务失败";
      throw e;
    } finally {
      loading.value = false;
      operationInProgress.value = null;
    }
  }

  /**
   * 设置当前任务
   */
  function setCurrentTask(task: Task | null) {
    currentTask.value = task;
  }

  /**
   * 清除当前任务
   */
  function clearCurrentTask() {
    currentTask.value = null;
    error.value = null;
  }

  /**
   * 清除错误
   */
  function clearError() {
    error.value = null;
  }

  return {
    // State
    currentTask,
    loading,
    error,
    operationInProgress,
    // Actions
    fetchTask,
    createTask,
    updateTask,
    deleteTask,
    completeTask,
    setCurrentTask,
    clearCurrentTask,
    clearError,
  };
});
