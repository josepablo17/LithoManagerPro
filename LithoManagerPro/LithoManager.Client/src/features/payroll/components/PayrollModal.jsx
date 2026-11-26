import { X } from 'lucide-react'
import { useId } from 'react'
import { Button } from '../../../components/ui/Button.jsx'
import styles from '../pages/Payroll.module.css'

/** Dialog shell shared by the payroll forms and confirmations. */
export function PayrollModal({
  title,
  subtitle,
  isBusy = false,
  onClose,
  children,
}) {
  const titleId = useId()

  return (
    <div className={styles.modalOverlay}>
      <section
        className={styles.modal}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
      >
        <div className={styles.modalHeader}>
          <div>
            <h2 id={titleId}>{title}</h2>
            {subtitle ? <p>{subtitle}</p> : null}
          </div>
          <Button
            variant="ghost"
            size="small"
            aria-label="Cerrar"
            disabled={isBusy}
            onClick={onClose}
          >
            <X size={18} aria-hidden="true" />
          </Button>
        </div>
        <div className={styles.modalBody}>{children}</div>
      </section>
    </div>
  )
}

export function FormField({ id, label, hint, error, children }) {
  return (
    <div className={styles.field}>
      <label className={styles.label} htmlFor={id}>
        {label}
      </label>
      {children}
      {error ? (
        <span className={styles.errorText}>{error}</span>
      ) : hint ? (
        <span className={styles.hint}>{hint}</span>
      ) : null}
    </div>
  )
}

export function FormActions({
  isSaving,
  submitText,
  savingText = 'Guardando...',
  submitVariant = 'primary',
  onCancel,
}) {
  return (
    <div className={styles.formActions}>
      <Button variant="secondary" disabled={isSaving} onClick={onCancel}>
        Cancelar
      </Button>
      <Button
        type="submit"
        variant={submitVariant}
        isLoading={isSaving}
        loadingText={savingText}
      >
        {submitText}
      </Button>
    </div>
  )
}
