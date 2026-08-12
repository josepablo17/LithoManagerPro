import { Button } from '../../../components/ui/Button.jsx'
import styles from '../../../app/Page.module.css'

export function DepartmentsPage() {
  return (
    <section className={styles.page}>
      <div className={styles.pageHeader}>
        <div>
          <h1>Departamentos</h1>
          <p>Administra la estructura organizacional de la empresa.</p>
        </div>
        <Button>Agregar departamento</Button>
      </div>

      <div className={styles.toolbar}>
        <input
          className={styles.searchInput}
          type="search"
          placeholder="Buscar departamento"
          aria-label="Buscar departamento"
        />
        <select className={styles.select} aria-label="Filtrar por estado">
          <option>Todos los estados</option>
          <option>Activos</option>
          <option>Inactivos</option>
        </select>
      </div>

      <div className={styles.tableShell}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Código</th>
              <th>Nombre</th>
              <th>Estado</th>
              <th aria-label="Acciones" />
            </tr>
          </thead>
          <tbody>
            <tr>
              <td colSpan="4" className={styles.emptyCell}>
                Aún no hemos conectado el listado de departamentos.
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  )
}
