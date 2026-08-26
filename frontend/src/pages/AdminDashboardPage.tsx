import { Link } from 'react-router-dom'
import { useCompanySettings } from '../company/CompanySettingsContext'

function getAdminAreas(companyName: string) { return [
  { title: 'Competencias de referencia', text: 'Competencias deportivas reales que sirven como fuente de fechas, partidos y resultados.', to: '/competitions' },
  { title: 'Equipos y Planteles', text: 'Gestionar equipos, jugadores y planteles disponibles para las competencias.', to: '/admin/teams' },
  { title: 'Fixture / Partidos', text: 'Fechas y partidos oficiales de las competencias de referencia, reutilizados por las Competencias EL NENE.', to: '/admin/fixture' },
  { title: `Competencias ${companyName}`, text: `Competencias propias de ${companyName} creadas sobre una competencia de referencia.`, to: '/admin/official-leagues' },
  { title: 'Resultados', text: 'Cargar y corregir resultados reales.', to: '/admin/results' },
  { title: 'Rankings', text: 'Consultar rankings generados.', to: '/rankings' },
  { title: 'Configuración', text: 'Empresa, identidad, puntuación y reglas de juego.', to: '/admin/settings' },
] }

export default function AdminDashboardPage() {
  const { company } = useCompanySettings()
  const companyName = company.shortName || 'PlayPredict'
  const adminAreas = getAdminAreas(companyName)
  return (
    <div>
      <div className="admin-header admin-dashboard__header">
        <div>
          <span className="admin-eyebrow">PLAYPREDICT</span>
          <h1>Administración</h1>
          <p className="admin-help">Gestioná las competencias de referencia y las competencias propias de {companyName} desde un único lugar.</p>
        </div>
      </div>
      <div className="admin-dashboard__grid">
        {adminAreas.map((area) => (
          <article key={area.title} className="admin-dashboard__card">
            <div><h2>{area.title}</h2><p>{area.text}</p></div>
            <Link className="btn btn-primary" to={area.to}>Gestionar</Link>
          </article>
        ))}
      </div>
    </div>
  )
}
