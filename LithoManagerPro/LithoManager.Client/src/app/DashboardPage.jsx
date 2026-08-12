import { Badge } from '../components/ui/Badge.jsx'
import styles from './Page.module.css'

export function DashboardPage() {
  return (
    <section className={styles.page}>
      <div className={styles.pageHeader}>
        <div>
          <h1>Inicio</h1>
          <p>Resumen inicial de operaciones y accesos principales.</p>
        </div>
        <Badge variant="info">Base frontend</Badge>
      </div>

      <div className={styles.section}>
        <h2>Primer alcance</h2>
        <p>
          Esta base prepara navegación, autenticación, estilos compartidos y
          estructura para construir las pantallas reales de departamentos y
          empleados.
        </p>
      </div>
    </section>
  )
}
