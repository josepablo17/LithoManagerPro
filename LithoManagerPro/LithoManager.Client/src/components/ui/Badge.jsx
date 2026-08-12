import styles from './Badge.module.css'

export function Badge({ children, variant = 'neutral' }) {
  return (
    <span className={styles[variant]}>
      {children}
    </span>
  )
}
