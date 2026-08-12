import { Link } from 'react-router-dom'
import styles from './Page.module.css'

export function ForbiddenPage() {
  return (
    <section className={styles.statePage}>
      <h1>Acceso denegado</h1>
      <p>No tienes permisos para ver esta sección.</p>
      <Link className={styles.textLink} to="/">
        Volver al inicio
      </Link>
    </section>
  )
}
