import { Link } from 'react-router-dom'
import styles from './Page.module.css'

export function NotFoundPage() {
  return (
    <section className={styles.statePage}>
      <h1>Recurso no encontrado</h1>
      <p>La página que buscas no existe o fue movida.</p>
      <Link className={styles.textLink} to="/">
        Volver al inicio
      </Link>
    </section>
  )
}
