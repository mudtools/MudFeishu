/**
 * 确认对话框组合式函数
 * 封装删除确认等操作
 */
import { ElMessageBox, ElMessage } from 'element-plus'
import type { Action } from 'element-plus'

export interface UseConfirmOptions {
  /** 标题 */
  title?: string
  /** 确认消息 */
  message?: string
  /** 确认按钮文本 */
  confirmText?: string
  /** 取消按钮文本 */
  cancelText?: string
  /** 消息类型 */
  type?: 'success' | 'warning' | 'info' | 'error'
  /** 成功消息 */
  successMessage?: string
}

export interface UseConfirmResult {
  /** 确认操作 */
  confirm: (
    onConfirm: () => Promise<void> | void,
    options?: UseConfirmOptions
  ) => Promise<boolean>
  /** 删除确认 */
  confirmDelete: (
    onConfirm: () => Promise<void> | void,
    itemName?: string
  ) => Promise<boolean>
}

/**
 * 确认对话框组合式函数
 * @param defaultOptions 默认配置
 * @returns 确认方法
 */
export function useConfirm(defaultOptions: UseConfirmOptions = {}): UseConfirmResult {
  /**
   * 确认操作
   * @param onConfirm 确认回调
   * @param options 配置选项
   * @returns 是否确认
   */
  const confirm = async (
    onConfirm: () => Promise<void> | void,
    options: UseConfirmOptions = {}
  ): Promise<boolean> => {
    const {
      title = defaultOptions.title || '确认操作',
      message = defaultOptions.message || '确定要执行此操作吗？',
      confirmText = '确定',
      cancelText = '取消',
      type = 'warning',
      successMessage,
    } = { ...defaultOptions, ...options }

    try {
      await ElMessageBox.confirm(message, title, {
        confirmButtonText: confirmText,
        cancelButtonText: cancelText,
        type,
      })

      await onConfirm()

      if (successMessage) {
        ElMessage.success(successMessage)
      }

      return true
    } catch (action: unknown) {
      // 用户取消
      if (action === 'cancel' || (action as Action) === 'cancel') {
        return false
      }
      // 其他错误重新抛出
      throw action
    }
  }

  /**
   * 删除确认
   * @param onConfirm 确认回调
   * @param itemName 项目名称
   * @returns 是否确认
   */
  const confirmDelete = async (
    onConfirm: () => Promise<void> | void,
    itemName = '此项目'
  ): Promise<boolean> => {
    return confirm(onConfirm, {
      title: '确认删除',
      message: `确定要删除${itemName}吗？此操作不可恢复。`,
      confirmText: '删除',
      cancelText: '取消',
      type: 'warning',
      successMessage: '删除成功',
    })
  }

  return {
    confirm,
    confirmDelete,
  }
}
