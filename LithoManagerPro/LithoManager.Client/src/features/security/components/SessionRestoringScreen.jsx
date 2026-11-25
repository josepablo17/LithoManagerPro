import styles from '../../../app/Page.module.css'

export function SessionRestoringScreen() {
  return (
    <section
      className={styles.statePage}
      role="status"
      aria-live="polite"
    >
      <p>Restaurando tu sesión…</p>
    </section>
  )
}
