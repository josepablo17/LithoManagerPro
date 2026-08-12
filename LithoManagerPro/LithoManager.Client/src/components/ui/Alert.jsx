import styles from './Alert.module.css'

export function Alert({ children, variant = 'info' }) {
  return (
    <div className={styles[variant]} role="alert">
      {children}
    </div>
  )
}
