import styles from './TextInput.module.css'

export function TextInput({
  id,
  label,
  error,
  helperText,
  required = false,
  ...props
}) {
  const descriptionId = error || helperText
    ? `${id}-description`
    : undefined

  return (
    <div className={styles.field}>
      <label className={styles.label} htmlFor={id}>
        {label}
        {required ? <span aria-hidden="true"> *</span> : null}
      </label>
      <input
        id={id}
        className={error ? styles.inputError : styles.input}
        aria-invalid={Boolean(error)}
        aria-describedby={descriptionId}
        required={required}
        {...props}
      />
      {error || helperText ? (
        <p
          id={descriptionId}
          className={error ? styles.errorText : styles.helperText}
        >
          {error ?? helperText}
        </p>
      ) : null}
    </div>
  )
}
