/**
 * 路由守卫
 */
import type { Router } from 'vue-router'

/**
 * 设置路由守卫
 */
export function setupRouterGuards(router: Router) {
  // 全局前置守卫
  router.beforeEach(async (to, _from, next) => {
    // 设置页面标题
    const title = to.meta.title as string | undefined
    document.title = title ? `${title} - TaskManage` : 'TaskManage'

    // 直接检查 localStorage 中的 token，避免 Pinia store 初始化问题
    const token = localStorage.getItem('token')
    const isAuthenticated = !!token

    const isPublic = to.meta.public === true
    const requiresAuth = to.meta.requiresAuth !== false // 默认需要认证

    // 公开页面直接放行
    if (isPublic) {
      // 已登录用户访问登录页，重定向到首页
      if (to.name === 'Login' && isAuthenticated) {
        next({ name: 'TaskList' })
        return
      }
      next()
      return
    }

    // 需要认证的页面
    if (requiresAuth && !isAuthenticated) {
      // 未登录，重定向到登录页
      next({
        name: 'Login',
        query: { redirect: to.fullPath },
      })
      return
    }

    // 绑定飞书页面特殊处理：已绑定飞书的用户直接跳转到首页
    if (to.name === 'BindFeishu') {
      // 如果用户信息中没有 isFeishuBound 字段，需要从其他地方判断
      // 这里暂时不做限制，让用户可以访问绑定页面
    }

    next()
  })

  // 全局后置守卫
  router.afterEach((to) => {
    // 可以在这里添加页面访问统计等逻辑
    console.log('导航完成:', to.path)
  })

  // 错误处理
  router.onError((error) => {
    console.error('路由错误:', error)
  })
}

/**
 * 路由元信息类型扩展
 */
declare module 'vue-router' {
  interface RouteMeta {
    /** 页面标题 */
    title?: string
    /** 是否公开页面（不需要登录） */
    public?: boolean
    /** 是否需要认证 */
    requiresAuth?: boolean
    /** 所需权限列表 */
    permissions?: string[]
    /** 所需角色列表 */
    roles?: string[]
    /** 是否缓存页面 */
    keepAlive?: boolean
    /** 页面图标 */
    icon?: string
  }
}
