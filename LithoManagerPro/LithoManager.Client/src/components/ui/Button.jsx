import styles from './Button.module.css'

export function Button({
  children,
  type = 'button',
  variant = 'primary',
  size = 'medium',
  isLoading = false,
  loadingText = 'Guardando...',
  disabled = false,
  className = '',
  ...props
}) {
  const classes = [
    styles.button,
    styles[variant],
    styles[size],
    className,
  ]
    .filter(Boolean)
    .join(' ')

  return (
    <button
      type={type}
      className={classes}
      disabled={disabled || isLoading}
      aria-busy={isLoading}
      {...props}
    >
      {isLoading ? loadingText : children}
    </button>
  )
}
