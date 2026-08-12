import styles from './Page.module.css'

export function PlaceholderPage({ title, description }) {
  return (
    <section className={styles.page}>
      <div className={styles.pageHeader}>
        <div>
          <h1>{title}</h1>
          <p>{description}</p>
        </div>
      </div>

      <div className={styles.section}>
        <p>Esta sección se construirá cuando avancemos este módulo.</p>
      </div>
    </section>
  )
}
