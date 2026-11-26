import { ArrowLeft } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Alert } from '../../../components/ui/Alert.jsx'
import styles from '../../../app/Page.module.css'
import { getEmployees } from '../../humanResources/services/employeeService.js'
import { useAuth } from '../../security/hooks/useAuth.js'
import { DisabilitiesSection } from '../components/DisabilitiesSection.jsx'
import { OvertimeSection } from '../components/OvertimeSection.jsx'
import { WorkSchedulesSection } from '../components/WorkSchedulesSection.jsx'
import { payrollMutationRoles } from '../payrollFormat.js'
import {
  getDisabilityTypes,
  getOvertimeRules,
  getWorkShiftTypes,
} from '../services/payrollOperationsService.js'
import payrollStyles from './Payroll.module.css'

const tabs = [
  { id: 'overtime', label: 'Horas extra' },
  { id: 'disabilities', label: 'Incapacidades' },
  { id: 'schedules', label: 'Horarios' },
]

/**
 * Data the payroll calculation consumes: approved overtime, approved
 * disabilities and each employee's work shift.
 */
export function PayrollOperationsPage() {
  const { accessToken, user } = useAuth()
  const canManage = payrollMutationRoles.has(user?.roleCode)
  const [activeTab, setActiveTab] = useState('overtime')
  const [catalogs, setCatalogs] = useState({
    overtimeRules: [],
    disabilityTypes: [],
    workShiftTypes: [],
  })
  const [employees, setEmployees] = useState([])
  const [loadError, setLoadError] = useState('')

  useEffect(() => {
    const controller = new AbortController()
    const { signal } = controller

    Promise.all([
      getOvertimeRules({ accessToken, signal }),
      getDisabilityTypes({ accessToken, signal }),
      getWorkShiftTypes({ accessToken, signal }),
      // Only administrators register operations, and only they can list
      // employees (HumanResourcesEmployees policy).
      canManage
        ? getEmployees({ accessToken, isActive: true, signal })
        : Promise.resolve([]),
    ])
      .then(([overtimeRules, disabilityTypes, workShiftTypes, activeEmployees]) => {
        setCatalogs({ overtimeRules, disabilityTypes, workShiftTypes })
        setEmployees(activeEmployees)
      })
      .catch((error) => {
        if (error.name !== 'AbortError') {
          setLoadError(error.message)
        }
      })

    return () => controller.abort()
  }, [accessToken, canManage])

  const sectionProps = {
    accessToken,
    canManage,
    catalogs,
    employees,
  }

  return (
    <section className={styles.page}>
      <div className={styles.pageHeader}>
        <div>
          <Link className={payrollStyles.backLink} to="/payroll">
            <ArrowLeft size={16} aria-hidden="true" />
            Planillas
          </Link>
          <h1>Operaciones de planilla</h1>
          <p>
            Horas extra, incapacidades y horarios. Lo aprobado aquí es lo que
            usa el cálculo de la planilla.
          </p>
        </div>
      </div>

      {loadError ? <Alert variant="error">{loadError}</Alert> : null}

      <div className={payrollStyles.tabs} role="tablist">
        {tabs.map((tab) => (
          <button
            key={tab.id}
            type="button"
            role="tab"
            aria-selected={activeTab === tab.id}
            className={
              activeTab === tab.id
                ? payrollStyles.tabActive
                : payrollStyles.tab
            }
            onClick={() => setActiveTab(tab.id)}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {activeTab === 'overtime' ? <OvertimeSection {...sectionProps} /> : null}
      {activeTab === 'disabilities' ? <DisabilitiesSection {...sectionProps} /> : null}
      {activeTab === 'schedules' ? <WorkSchedulesSection {...sectionProps} /> : null}
    </section>
  )
}
