import { Button } from '../../../components/ui/Button.jsx'
import styles from '../../../app/Page.module.css'

export function EmployeesPage() {
  return (
    <section className={styles.page}>
      <div className={styles.pageHeader}>
        <div>
          <h1>Empleados</h1>
          <p>Gestiona la información laboral y su vínculo opcional con usuarios.</p>
        </div>
        <Button>Agregar empleado</Button>
      </div>

      <div className={styles.toolbar}>
        <input
          className={styles.searchInput}
          type="search"
          placeholder="Buscar empleado"
          aria-label="Buscar empleado"
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
              <th>Empleado</th>
              <th>Departamento</th>
              <th>Puesto</th>
              <th>Estado</th>
              <th aria-label="Acciones" />
            </tr>
          </thead>
          <tbody>
            <tr>
              <td colSpan="5" className={styles.emptyCell}>
                Aún no hemos conectado el listado de empleados.
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  )
}
