import { createRouter, createWebHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/LoginView.vue'),
    meta: { public: true }
  },
  {
    path: '/',
    name: 'Home',
    component: () => import('@/views/FileManager.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: 'folder/:folderToken?',
        name: 'Folder',
        component: () => import('@/views/FileManager.vue')
      }
    ]
  },
  {
    path: '/recycle-bin',
    name: 'RecycleBin',
    component: () => import('@/views/RecycleBin.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/shares',
    name: 'ShareList',
    component: () => import('@/views/ShareList.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/share/:shareCode',
    name: 'ShareAccess',
    component: () => import('@/views/ShareAccess.vue'),
    meta: { public: true }
  },
  {
    path: '/file/:fileToken',
    name: 'FileDetail',
    component: () => import('@/views/FileDetail.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'NotFound',
    component: () => import('@/views/NotFound.vue')
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach(async (to, _from, next) => {
  const authStore = useAuthStore()

  // 如果有 token 但没有用户信息，尝试获取用户信息
  if (authStore.token && !authStore.user) {
    try {
      await authStore.init()
    } catch (error) {
      // 获取用户信息失败，清除认证状态
      authStore.clearAuth()
    }
  }

  // 公开页面直接放行
  if (to.meta.public) {
    // 已登录用户访问登录页，重定向到首页
    if (to.path === '/login' && authStore.isLoggedIn) {
      next('/')
      return
    }
    next()
    return
  }

  // 需要认证的页面
  if (to.meta.requiresAuth || !to.meta.public) {
    if (!authStore.isLoggedIn) {
      next({ path: '/login', query: { redirect: to.fullPath } })
      return
    }
  }

  next()
})

export default router
