import {
  Building2,
  CalendarClock,
  ClipboardList,
  FileText,
  FolderOpen,
  Home,
  LogOut,
  Menu,
  MessageSquare,
  Shield,
  Users,
  WalletCards,
  X,
} from 'lucide-react'
import { useState } from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { Button } from '../components/ui/Button.jsx'
import { useAuth } from '../features/security/hooks/useAuth.js'
import styles from './AppLayout.module.css'

const navigationGroups = [
  {
    label: 'Principal',
    items: [
      {
        label: 'Inicio',
        to: '/',
        icon: Home,
      },
    ],
  },
  {
    label: 'Recursos humanos',
    items: [
      {
        label: 'Empleados',
        to: '/human-resources/employees',
        icon: Users,
      },
      {
        label: 'Departamentos',
        to: '/human-resources/departments',
        icon: Building2,
      },
    ],
  },
  {
    label: 'Vacaciones',
    items: [
      {
        label: 'Solicitudes',
        to: '/leave-management/requests',
        icon: CalendarClock,
      },
      {
        label: 'Saldos',
        to: '/leave-management/balances',
        icon: ClipboardList,
      },
    ],
  },
  {
    label: 'Gestión',
    items: [
      {
        label: 'Documentos',
        to: '/documents',
        icon: FolderOpen,
      },
      {
        label: 'Formularios',
        to: '/forms',
        icon: FileText,
      },
      {
        label: 'Planilla',
        to: '/payroll',
        icon: WalletCards,
      },
      {
        label: 'Mensajes',
        to: '/chat',
        icon: MessageSquare,
      },
    ],
  },
  {
    label: 'Sistema',
    items: [
      {
        label: 'Usuarios',
        to: '/security/users',
        icon: Shield,
      },
    ],
  },
]

export function AppLayout() {
  const [isSidebarOpen, setIsSidebarOpen] = useState(false)
  const { user, logout } = useAuth()
  const displayName = getDisplayName(user)

  return (
    <div className={styles.shell}>
      <aside
        className={
          isSidebarOpen
            ? `${styles.sidebar} ${styles.sidebarOpen}`
            : styles.sidebar
        }
        aria-label="Navegación principal"
      >
        <div className={styles.brand}>
          <div className={styles.brandMark} aria-hidden="true">
            LM
          </div>
          <div>
            <strong>LithoManagerPro</strong>
            <span>Administración</span>
          </div>
          <Button
            variant="ghost"
            className={styles.closeButton}
            aria-label="Cerrar navegación"
            onClick={() => setIsSidebarOpen(false)}
          >
            <X size={20} aria-hidden="true" />
          </Button>
        </div>

        <nav className={styles.navigation}>
          {navigationGroups.map((group) => (
            <div className={styles.navigationGroup} key={group.label}>
              <p className={styles.navigationLabel}>{group.label}</p>
              {group.items.map((item) => {
                const Icon = item.icon

                return (
                  <NavLink
                    className={({ isActive }) =>
                      isActive
                        ? `${styles.navItem} ${styles.navItemActive}`
                        : styles.navItem
                    }
                    key={item.to}
                    to={item.to}
                    onClick={() => setIsSidebarOpen(false)}
                  >
                    <Icon size={20} aria-hidden="true" />
                    <span>{item.label}</span>
                  </NavLink>
                )
              })}
            </div>
          ))}
        </nav>
      </aside>

      {isSidebarOpen ? (
        <button
          className={styles.overlay}
          type="button"
          aria-label="Cerrar navegación"
          onClick={() => setIsSidebarOpen(false)}
        />
      ) : null}

      <div className={styles.mainArea}>
        <header className={styles.topbar}>
          <Button
            variant="ghost"
            className={styles.menuButton}
            aria-label="Abrir navegación"
            onClick={() => setIsSidebarOpen(true)}
          >
            <Menu size={20} aria-hidden="true" />
          </Button>

          <div className={styles.userSummary}>
            <div className={styles.avatar} aria-hidden="true">
              {getInitials(displayName)}
            </div>
            <div>
              <strong>{displayName}</strong>
              <span>{user?.roleDisplayName ?? 'Usuario'}</span>
            </div>
          </div>

          <Button variant="secondary" onClick={logout}>
            <LogOut size={18} aria-hidden="true" />
            Cerrar sesión
          </Button>
        </header>

        <main className={styles.content}>
          <Outlet />
        </main>
      </div>
    </div>
  )
}

function getDisplayName(user) {
  const fullName = [
    user?.firstName,
    user?.lastName,
  ]
    .filter(Boolean)
    .join(' ')

  return fullName || user?.emailAddress || 'Usuario'
}

function getInitials(displayName) {
  return displayName
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join('')
}
